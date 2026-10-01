using SwiftBets.Wallet.Domain.ResponsibleGambling;

namespace SwiftBets.Wallet.Application.Ports;

/// <summary>Compliance's published limits and blocks for a customer, keyed by user id; filter with <c>In(currency)</c> per account.</summary>
public interface IGamblingRules
{
    GamblingRules For(Guid userId);
}
