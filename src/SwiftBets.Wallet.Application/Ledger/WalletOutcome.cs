using SwiftBets.Wallet.Domain.ResponsibleGambling;
using SwiftBets.Wallet.Domain;

namespace SwiftBets.Wallet.Application.Ledger;

/// <summary>Detail says why a refusal happened when the failure alone does not, such as which limit and what is left.</summary>
public sealed record WalletOutcome(bool WasApplied, WalletFailure? Failure, Guid? PostingId, Account? Account, Reservation? Reservation, string? Detail = null)
{
    public static WalletOutcome Failed(WalletFailure failure, string? detail = null) => new(false, failure, null, null, null, detail);
}

/// <summary>Thrown inside a posting to refuse it with a reason; the runner rolls back and returns the refusal.</summary>
public sealed class WalletRefusedException(WalletFailure failure, string detail, LimitHit? limit = null) : Exception(detail)
{
    public WalletFailure Failure { get; } = failure;

    /// <summary>Set when the customer's own limit refused the movement, so they can be told once the transaction is gone.</summary>
    public LimitHit? Limit { get; } = limit;
}

/// <summary>A stake or deposit refused at one of the customer's limits.</summary>
public sealed record LimitHit(Guid UserId, SpendLimit Limit, string Refused, long Attempted, string Currency);
