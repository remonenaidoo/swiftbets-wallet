using SwiftBets.Wallet.Application.Ports;
using SwiftBets.Wallet.Domain;
using SwiftBets.Wallet.Domain.ResponsibleGambling;

namespace SwiftBets.Wallet.Application.Ledger;

/// <summary>Credits (winnings, refunds, top-ups) and debits (resettlement clawbacks) between a punter and the house or funding account.</summary>
public sealed class TransferHandler(PostingRunner runner, IGamblingRules rules, TimeProvider time)
{
    public Task<WalletOutcome> CreditAsync(string idempotencyKey, Guid accountId, long amount, string currency, string reference) =>
        MoveAsync(PostingKind.Credit, WellKnownAccounts.House, idempotencyKey, accountId, amount, currency, reference);

    /// <summary>A top-up is also how a punter account is opened.</summary>
    public Task<WalletOutcome> TopUpAsync(string idempotencyKey, Guid accountId, long amount, string currency, string reference) =>
        MoveAsync(PostingKind.TopUp, WellKnownAccounts.Funding, idempotencyKey, accountId, amount, currency, reference, openIfMissing: true);

    public Task<WalletOutcome> DebitAsync(string idempotencyKey, Guid accountId, long amount, string currency, string reference) =>
        runner.RunAsync(idempotencyKey, PostingKind.Debit, accountId, amount, async transaction =>
        {
            var account = await transaction.LockAccountAsync(accountId);
            if (account is null)
            {
                return (WalletFailure.AccountNotFound, null, [], null, false);
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
                [new(accountId, Bucket.Available, -amount), new(WellKnownAccounts.House, Bucket.Available, amount)]), [account], null, false);
        });

    private Task<WalletOutcome> MoveAsync(PostingKind kind, Guid source, string idempotencyKey, Guid accountId, long amount, string currency, string reference, bool openIfMissing = false) =>
        runner.RunAsync(idempotencyKey, kind, accountId, amount, async transaction =>
        {
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
                await RecordSpendAsync(transaction, kind, accountId, amount);
            }

            return (null, new Posting(Guid.CreateVersion7(), kind, idempotencyKey, reference,
                [new(source, Bucket.Available, -amount), new(accountId, Bucket.Available, amount)]), [account], null, false);
        });

    /// <summary>A top-up is a deposit, judged against deposit limits and blocks; a credit is winnings or a refund.</summary>
    private async Task RecordSpendAsync(IWalletTransaction transaction, PostingKind kind, Guid accountId, long amount)
    {
        var now = time.GetUtcNow();
        if (kind != PostingKind.TopUp)
        {
            await transaction.AddSpendAsync(accountId, now, 0, amount, 0);
            return;
        }

        if (SpendPolicy.CheckDeposit(rules.For(accountId), await transaction.GetSpendAsync(accountId, now), amount, now) is { } refusal)
        {
            throw new WalletRefusedException(refusal.Failure, refusal.Detail);
        }

        await transaction.AddSpendAsync(accountId, now, 0, 0, amount);
    }
}
