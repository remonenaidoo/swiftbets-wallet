using SwiftBets.Wallet.Domain.ResponsibleGambling;

namespace SwiftBets.Wallet.Application.Ports;

/// <summary>Compliance's published limits and blocks for an account (the punter's user id).</summary>
public interface IGamblingRules
{
    GamblingRules For(Guid accountId);
}
