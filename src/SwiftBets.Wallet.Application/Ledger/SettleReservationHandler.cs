using SwiftBets.Wallet.Application.Ports;
using SwiftBets.Wallet.Domain;

namespace SwiftBets.Wallet.Application.Ledger;

/// <summary>
/// Ends a held reservation: a stake is captured into the house, a withdrawal is paid out to the funding account, and
/// either can be released back to the punter. Capturing a withdrawal as a stake (or the reverse) is refused.
/// </summary>
public sealed class SettleReservationHandler(PostingRunner runner, IWalletStore store)
{
    public Task<WalletOutcome> CaptureAsync(string idempotencyKey, Guid reservationId, CancellationToken cancellationToken) =>
        RunAsync(idempotencyKey, reservationId, Ending.Capture, cancellationToken);

    public Task<WalletOutcome> CompleteWithdrawalAsync(string idempotencyKey, Guid reservationId, CancellationToken cancellationToken) =>
        RunAsync(idempotencyKey, reservationId, Ending.PayOut, cancellationToken);

    public Task<WalletOutcome> ReleaseAsync(string idempotencyKey, Guid reservationId, CancellationToken cancellationToken) =>
        RunAsync(idempotencyKey, reservationId, Ending.Release, cancellationToken);

    private enum Ending
    {
        Capture,
        PayOut,
        Release,
    }

    private async Task<WalletOutcome> RunAsync(string idempotencyKey, Guid reservationId, Ending ending, CancellationToken cancellationToken)
    {
        var known = await store.GetReservationAsync(reservationId, cancellationToken);
        if (known is null)
        {
            return WalletOutcome.Failed(WalletFailure.ReservationNotFound);
        }

        var isWithdrawal = known.Purpose == ReservationPurpose.Withdrawal;
        if ((ending == Ending.Capture && isWithdrawal) || (ending == Ending.PayOut && !isWithdrawal))
        {
            return WalletOutcome.Failed(WalletFailure.InvalidState);
        }

        if ((ending == Ending.Capture ? WellKnownAccounts.HouseFor(known.Currency) : WellKnownAccounts.FundingFor(known.Currency)) is not { } counterparty)
        {
            return WalletOutcome.Failed(WalletFailure.CurrencyMismatch);
        }

        var kind = ending switch
        {
            Ending.Capture => PostingKind.Capture,
            Ending.PayOut => PostingKind.WithdrawalPaid,
            _ => isWithdrawal ? PostingKind.WithdrawalReturned : PostingKind.Release,
        };
        return await runner.RunAsync(idempotencyKey, kind, known.AccountId, known.Amount, async transaction =>
        {
            var reservation = (await transaction.LockReservationAsync(reservationId))!;
            var account = (await transaction.LockAccountAsync(reservation.AccountId))!;
            var release = ending == Ending.Release;
            var failure = reservation.MoveTo(release ? ReservationState.Released : ReservationState.Captured)
                ?? (release ? account.ReturnReservation(reservation.Amount) : account.ConsumeReservation(reservation.Amount));
            if (failure is not null)
            {
                return (failure, null, [], null, false);
            }

            // A released stake was never at risk, so it comes off the totals of the periods it was placed in.
            if (release && !isWithdrawal && account.Kind == AccountKind.Punter)
            {
                await transaction.AddSpendAsync(account.AccountId, await transaction.ReservedAtAsync(reservationId), -reservation.Amount, 0, 0);
            }

            IReadOnlyList<LedgerEntry> entries = release
                ? [new(account.AccountId, Bucket.Reserved, -reservation.Amount), new(account.AccountId, Bucket.Available, reservation.Amount)]
                : [new(account.AccountId, Bucket.Reserved, -reservation.Amount), new(counterparty, Bucket.Available, reservation.Amount)];
            return (null, new Posting(Guid.CreateVersion7(), kind, idempotencyKey, reservation.Reference, entries), [account], reservation, false);
        });
    }
}
