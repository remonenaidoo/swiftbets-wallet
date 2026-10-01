using SwiftBets.Wallet.Domain;
using SwiftBets.Wallet.Domain.ResponsibleGambling;

namespace SwiftBets.Wallet.Application.Ports;

public interface IWalletTransaction : IAsyncDisposable
{
    /// <summary>Looks the key up without locking; a concurrent duplicate is caught by the unique index on save.</summary>
    Task<StoredPosting?> FindPostingAsync(string idempotencyKey);

    Task<Account?> LockAccountAsync(Guid accountId);

    Task<Reservation?> LockReservationAsync(Guid reservationId);

    /// <summary>Opens an empty punter account if none exists and returns it locked.</summary>
    Task<Account> OpenPunterAccountAsync(Guid accountId, string currency);

    /// <exception cref="DuplicateIdempotencyKeyException">Another transaction committed the same key.</exception>
    Task SaveAsync(Posting posting, StoredPosting record, IReadOnlyList<Account> changedAccounts, Reservation? reservation, bool isNewReservation);

    /// <summary>The account's totals for the periods containing <paramref name="now"/>; call it with the account locked.</summary>
    Task<SpendCounters> GetSpendAsync(Guid accountId, DateTimeOffset now);

    /// <summary>Adds to the day, week and month totals containing <paramref name="at"/>; amounts may be negative.</summary>
    Task AddSpendAsync(Guid accountId, DateTimeOffset at, long staked, long won, long deposited);

    Task<DateTimeOffset> ReservedAtAsync(Guid reservationId);

    Task CommitAsync();
}
