using SwiftBets.Wallet.Application.Ports;
using SwiftBets.Wallet.Domain;
using SwiftBets.Wallet.Domain.ResponsibleGambling;

namespace SwiftBets.Wallet.Infrastructure.Persistence;

internal sealed record AccountRow(Guid AccountId, byte Kind, string Currency, long Available, long Reserved, bool IsBlacklisted, Guid? UserId, long Bonus)
{
    public Account ToDomain() => new(AccountId, (AccountKind)Kind, Currency, Available, Reserved, IsBlacklisted, UserId, Bonus);
}

internal sealed record ReservationRow(Guid ReservationId, Guid AccountId, long Amount, string Currency, string Reference, byte State, byte Purpose)
{
    public Reservation ToDomain() => new(ReservationId, AccountId, Amount, Currency, Reference, (ReservationState)State, (ReservationPurpose)Purpose);
}

internal sealed record StatementRow(long Sequence, Guid PostingId, byte Kind, long Amount, long AvailableAfter, string Reference, DateTimeOffset PostedAt)
{
    public StatementLine ToLine() => new(Sequence, PostingId, (PostingKind)Kind, Amount, AvailableAfter, Reference, PostedAt);
}

internal sealed record PostingRow(Guid PostingId, byte Kind, Guid AccountId, long Amount, Guid? ReservationId);

internal sealed record SpendRow(byte Period, long Staked, long Won, long Deposited)
{
    public SpendTotals ToTotals() => new(Staked, Won, Deposited);
}
