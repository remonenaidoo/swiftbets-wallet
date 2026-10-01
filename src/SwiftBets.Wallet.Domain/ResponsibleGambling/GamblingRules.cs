namespace SwiftBets.Wallet.Domain.ResponsibleGambling;

public enum SpendKind
{
    Deposit,
    Stake,
    Loss,
}

public enum SpendPeriod
{
    Day,
    Week,
    Month,
}

public enum BlockKind
{
    Exclusion,
    NoDeposits,
    NoBetting,
    NoWithdrawals,
}

/// <summary>A limit as compliance published it: Amount applies now, PendingAmount (null: no limit) from PendingEffectiveAt.</summary>
public sealed record SpendLimit(SpendKind Kind, SpendPeriod Period, long Amount, long? PendingAmount, DateTimeOffset? PendingEffectiveAt)
{
    /// <summary>The cap at <paramref name="now"/>, or null when a due removal has lifted it.</summary>
    public long? CapAt(DateTimeOffset now) => PendingEffectiveAt is { } due && due <= now ? PendingAmount : Amount;
}

public sealed record SpendBlock(BlockKind Kind, DateTimeOffset StartsAt, DateTimeOffset? EndsAt)
{
    public bool IsActive(DateTimeOffset now) => StartsAt <= now && (EndsAt is null || EndsAt > now);
}

/// <summary>One account's responsible-gambling rules; an account compliance has never touched has none.</summary>
public sealed record GamblingRules(IReadOnlyList<SpendLimit> Limits, IReadOnlyList<SpendBlock> Blocks)
{
    public static GamblingRules None { get; } = new([], []);
}
