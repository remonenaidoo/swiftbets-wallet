using SwiftBets.Wallet.Application.Ports;
using SwiftBets.Wallet.Domain;
using SwiftBets.Wallet.Domain.ResponsibleGambling;

namespace SwiftBets.Wallet.Application.Ledger;

/// <summary>
/// Holds money: a stake, judged against stake and loss limits and betting blocks under the account lock (ADR 0006), or a
/// withdrawal, refused only by a withdrawal block and kept out of the spend totals.
/// </summary>
public sealed class ReserveFundsHandler(PostingRunner runner, IGamblingRules rules, TimeProvider time)
{
    public Task<WalletOutcome> HandleAsync(string idempotencyKey, Guid accountId, long amount, string currency, string reference) =>
        HoldAsync(ReservationPurpose.Stake, idempotencyKey, accountId, amount, currency, reference);

    public Task<WalletOutcome> HoldWithdrawalAsync(string idempotencyKey, Guid accountId, long amount, string currency, string reference) =>
        HoldAsync(ReservationPurpose.Withdrawal, idempotencyKey, accountId, amount, currency, reference);

    private Task<WalletOutcome> HoldAsync(ReservationPurpose purpose, string idempotencyKey, Guid accountId, long amount, string currency, string reference)
    {
        var kind = purpose == ReservationPurpose.Stake ? PostingKind.Reserve : PostingKind.WithdrawalHold;
        return runner.RunAsync(idempotencyKey, kind, accountId, amount, async transaction =>
        {
            var account = await transaction.LockAccountAsync(accountId);
            if (account is null)
            {
                return (WalletFailure.AccountNotFound, null, [], null, false);
            }

            if (purpose == ReservationPurpose.Withdrawal && account.Kind != AccountKind.Punter)
            {
                return (WalletFailure.InvalidState, null, [], null, false);
            }

            if (account.Reserve(amount, currency) is { } failure)
            {
                return (failure, null, [], null, false);
            }

            if (account.Kind == AccountKind.Punter)
            {
                await JudgeAsync(transaction, purpose, account, amount);
            }

            var reservation = new Reservation(Guid.CreateVersion7(), accountId, amount, currency, reference, ReservationState.Held, purpose);
            var posting = new Posting(Guid.CreateVersion7(), kind, idempotencyKey, reference,
            [
                new LedgerEntry(accountId, Bucket.Available, -amount),
                new LedgerEntry(accountId, Bucket.Reserved, amount),
            ]);
            return (null, posting, [account], reservation, true);
        });
    }

    private async Task JudgeAsync(IWalletTransaction transaction, ReservationPurpose purpose, Account account, long amount)
    {
        var now = time.GetUtcNow();
        var accountRules = rules.For(account.UserId).In(account.Currency);
        if (purpose == ReservationPurpose.Withdrawal)
        {
            if (SpendPolicy.CheckWithdrawal(accountRules, now) is { } blocked)
            {
                throw new WalletRefusedException(blocked.Failure, blocked.Detail);
            }

            return;
        }

        if (SpendPolicy.CheckStake(accountRules, await transaction.GetSpendAsync(account.AccountId, now), amount, now) is { } refusal)
        {
            throw new WalletRefusedException(refusal.Failure, refusal.Detail, refusal.Limit is { } hit ? new LimitHit(account.UserId, hit, "stake", amount, account.Currency) : null);
        }

        await transaction.AddSpendAsync(account.AccountId, now, amount, 0, 0);
    }
}
