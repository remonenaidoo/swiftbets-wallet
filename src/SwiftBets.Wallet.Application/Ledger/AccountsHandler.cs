using SwiftBets.Wallet.Application.Ports;
using SwiftBets.Wallet.Domain;

namespace SwiftBets.Wallet.Application.Ledger;

/// <summary>
/// A customer's accounts, one per currency. The first one takes the user id as its account id, so placement and payout,
/// which address the account by user id, keep working; later currencies get new ids.
/// </summary>
public sealed class AccountsHandler(IWalletStore store)
{
    public Task<IReadOnlyList<Account>> ListAsync(Guid userId, CancellationToken cancellationToken) => store.ListAccountsAsync(userId, cancellationToken);

    public async Task<(Account? Account, WalletFailure? Failure)> OpenAsync(Guid userId, string currency, CancellationToken cancellationToken)
    {
        if (WellKnownAccounts.HouseFor(currency) is null)
        {
            return (null, WalletFailure.CurrencyMismatch);
        }

        // Two attempts: a concurrent open of the same or another first account can take the id between list and insert.
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var accounts = await store.ListAccountsAsync(userId, cancellationToken);
            if (accounts.FirstOrDefault(a => a.Currency == currency) is { } existing)
            {
                return (existing, null);
            }

            var accountId = accounts.Count == 0 ? userId : Guid.CreateVersion7();
            if (await store.OpenAccountAsync(accountId, userId, currency, cancellationToken) is { } opened)
            {
                return (opened, null);
            }
        }

        return (await store.ListAccountsAsync(userId, cancellationToken)).FirstOrDefault(a => a.Currency == currency) is { } raced
            ? (raced, null)
            : (null, WalletFailure.InvalidState);
    }
}
