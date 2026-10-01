using SwiftBets.Wallet.Application.Ports;
using SwiftBets.Wallet.Domain.ResponsibleGambling;

namespace SwiftBets.Wallet.Infrastructure.Tests;

/// <summary>Rules set directly by a test instead of read from compliance's topic.</summary>
internal sealed class FixedRules(GamblingRules rules) : IGamblingRules
{
    public static FixedRules None { get; } = new(GamblingRules.None);

    public GamblingRules For(Guid userId) => rules;
}
