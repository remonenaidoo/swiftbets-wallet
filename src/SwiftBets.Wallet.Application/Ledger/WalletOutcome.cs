using SwiftBets.Wallet.Domain;

namespace SwiftBets.Wallet.Application.Ledger;

/// <summary>Detail says why a refusal happened when the failure alone does not, such as which limit and what is left.</summary>
public sealed record WalletOutcome(bool WasApplied, WalletFailure? Failure, Guid? PostingId, Account? Account, Reservation? Reservation, string? Detail = null)
{
    public static WalletOutcome Failed(WalletFailure failure, string? detail = null) => new(false, failure, null, null, null, detail);
}

/// <summary>Thrown inside a posting to refuse it with a reason; the runner rolls back and returns the refusal.</summary>
public sealed class WalletRefusedException(WalletFailure failure, string detail) : Exception(detail)
{
    public WalletFailure Failure { get; } = failure;
}
