namespace SwiftBets.Wallet.Domain.ResponsibleGambling;

/// <summary>What an account has staked, won and deposited in one period. Loss is stakes less winnings, never below zero.</summary>
public sealed record SpendTotals(long Staked, long Won, long Deposited)
{
    public static SpendTotals Zero { get; } = new(0, 0, 0);

    public long Loss => Math.Max(0, Staked - Won);
}

/// <summary>The account's totals for the current day, week and month.</summary>
public sealed record SpendCounters(SpendTotals Day, SpendTotals Week, SpendTotals Month)
{
    public static SpendCounters Zero { get; } = new(SpendTotals.Zero, SpendTotals.Zero, SpendTotals.Zero);

    public SpendTotals For(SpendPeriod period) => period switch
    {
        SpendPeriod.Day => Day,
        SpendPeriod.Week => Week,
        _ => Month,
    };
}
