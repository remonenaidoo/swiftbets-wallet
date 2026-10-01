using SwiftBets.Wallet.Application.Ports;
using SwiftBets.Wallet.Domain;
using SwiftBets.Wallet.Domain.ResponsibleGambling;

namespace SwiftBets.Wallet.Application.Ledger;

/// <summary>Holds a stake. Stake and loss limits and betting blocks are judged here, under the account lock (ADR 0006).</summary>
public sealed class ReserveFundsHandler(PostingRunner runner, IGamblingRules rules, TimeProvider time)
{
    public Task<WalletOutcome> HandleAsync(string idempotencyKey, Guid accountId, long amount, string currency, string reference) =>
        runner.RunAsync(idempotencyKey, PostingKind.Reserve, accountId, amount, async transaction =>
        {
            var account = await transaction.LockAccountAsync(accountId);
            if (account is null)
            {
                return (WalletFailure.AccountNotFound, null, [], null, false);
            }

            if (account.Reserve(amount, currency) is { } failure)
            {
                return (failure, null, [], null, false);
            }

            if (account.Kind == AccountKind.Punter)
            {
                var now = time.GetUtcNow();
                if (SpendPolicy.CheckStake(rules.For(accountId), await transaction.GetSpendAsync(accountId, now), amount, now) is { } refusal)
                {
                    throw new WalletRefusedException(refusal.Failure, refusal.Detail);
                }

                await transaction.AddSpendAsync(accountId, now, amount, 0, 0);
            }

            var reservation = new Reservation(Guid.CreateVersion7(), accountId, amount, currency, reference, ReservationState.Held);
            var posting = new Posting(Guid.CreateVersion7(), PostingKind.Reserve, idempotencyKey, reference,
            [
                new LedgerEntry(accountId, Bucket.Available, -amount),
                new LedgerEntry(accountId, Bucket.Reserved, amount),
            ]);
            return (null, posting, [account], reservation, true);
        });
}
