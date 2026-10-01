using SwiftBets.Wallet.Application.Ports;
using SwiftBets.Wallet.Domain;
using SwiftBets.Wallet.Domain.ResponsibleGambling;

namespace SwiftBets.Wallet.Application.Ledger;

/// <summary>
/// Credits (winnings, refunds), deposits and operator top-ups into a punter account, and debits (resettlement clawbacks)
/// out of it, against the house or funding account of the account's currency.
/// </summary>
public sealed class TransferHandler(PostingRunner runner, IGamblingRules rules, TimeProvider time)
{
    public Task<WalletOutcome> CreditAsync(string idempotencyKey, Guid accountId, long amount, string currency, string reference) =>
        MoveAsync(PostingKind.Credit, idempotencyKey, accountId, amount, currency, reference);

    /// <summary>An operator top-up is also how a first punter account is opened (its id is the user id).</summary>
    public Task<WalletOutcome> TopUpAsync(string idempotencyKey, Guid accountId, long amount, string currency, string reference) =>
        MoveAsync(PostingKind.TopUp, idempotencyKey, accountId, amount, currency, reference, openIfMissing: true);

    /// <summary>A provider-confirmed deposit; the account must already be open (payments opens it first).</summary>
    public Task<WalletOutcome> DepositAsync(string idempotencyKey, Guid accountId, long amount, string currency, string reference) =>
        MoveAsync(PostingKind.Deposit, idempotencyKey, accountId, amount, currency, reference);

    public Task<WalletOutcome> DebitAsync(string idempotencyKey, Guid accountId, long amount, string currency, string reference) =>
        runner.RunAsync(idempotencyKey, PostingKind.Debit, accountId, amount, async transaction =>
        {
            var account = await transaction.LockAccountAsync(accountId);
            if (account is null)
            {
                return (WalletFailure.AccountNotFound, null, [], null, false);
            }

            if (WellKnownAccounts.HouseFor(currency) is not { } house)
            {
                return (WalletFailure.CurrencyMismatch, null, [], null, false);
            }

            if (account.Pay(amount, currency) is { } failure)
            {
                return (failure, null, [], null, false);
            }

            // A clawback undoes winnings, so the loss headroom they earned goes too.
            if (account.Kind == AccountKind.Punter)
            {
                await transaction.AddSpendAsync(accountId, time.GetUtcNow(), 0, -amount, 0);
            }

            return (null, new Posting(Guid.CreateVersion7(), PostingKind.Debit, idempotencyKey, reference,
                [new(accountId, Bucket.Available, -amount), new(house, Bucket.Available, amount)]), [account], null, false);
        });

    private Task<WalletOutcome> MoveAsync(PostingKind kind, string idempotencyKey, Guid accountId, long amount, string currency, string reference, bool openIfMissing = false) =>
        runner.RunAsync(idempotencyKey, kind, accountId, amount, async transaction =>
        {
            var source = kind == PostingKind.Credit ? WellKnownAccounts.HouseFor(currency) : WellKnownAccounts.FundingFor(currency);
            if (source is null)
            {
                return (WalletFailure.CurrencyMismatch, null, [], null, false);
            }

            var account = await transaction.LockAccountAsync(accountId)
                ?? (openIfMissing ? await transaction.OpenPunterAccountAsync(accountId, currency) : null);
            if (account is null)
            {
                return (WalletFailure.AccountNotFound, null, [], null, false);
            }

            if (account.Receive(amount, currency) is { } failure)
            {
                return (failure, null, [], null, false);
            }

            if (account.Kind == AccountKind.Punter)
            {
                await RecordSpendAsync(transaction, kind, account, amount);
            }

            return (null, new Posting(Guid.CreateVersion7(), kind, idempotencyKey, reference,
                [new(source.Value, Bucket.Available, -amount), new(accountId, Bucket.Available, amount)]), [account], null, false);
        });

    /// <summary>A deposit or top-up is judged against deposit limits and blocks; a credit is winnings or a refund.</summary>
    private async Task RecordSpendAsync(IWalletTransaction transaction, PostingKind kind, Account account, long amount)
    {
        var now = time.GetUtcNow();
        if (kind is not (PostingKind.TopUp or PostingKind.Deposit))
        {
            await transaction.AddSpendAsync(account.AccountId, now, 0, amount, 0);
            return;
        }

        if (SpendPolicy.CheckDeposit(rules.For(account.UserId).In(account.Currency), await transaction.GetSpendAsync(account.AccountId, now), amount, now) is { } refusal)
        {
            throw new WalletRefusedException(refusal.Failure, refusal.Detail);
        }

        await transaction.AddSpendAsync(account.AccountId, now, 0, 0, amount);
    }
}
