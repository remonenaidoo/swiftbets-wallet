using SwiftBets.Wallet.Application.Ledger;

namespace SwiftBets.Wallet.Application.Ports;

/// <summary>Tells the customer a limit refused them; best effort, after the refused transaction has rolled back.</summary>
public interface ILimitReachedNotifier
{
    Task NotifyAsync(LimitHit hit);
}
