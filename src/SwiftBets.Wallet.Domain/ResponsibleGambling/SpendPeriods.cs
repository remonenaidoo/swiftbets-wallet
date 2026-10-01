namespace SwiftBets.Wallet.Domain.ResponsibleGambling;

/// <summary>Limit periods follow the South African calendar (UTC+2, no daylight saving); weeks start on Monday.</summary>
public static class SpendPeriods
{
    private static readonly TimeSpan Offset = TimeSpan.FromHours(2);

    public static DateOnly StartOf(SpendPeriod period, DateTimeOffset at)
    {
        var local = DateOnly.FromDateTime(at.ToOffset(Offset).DateTime);
        return period switch
        {
            SpendPeriod.Day => local,
            SpendPeriod.Week => local.AddDays(-(((int)local.DayOfWeek + 6) % 7)),
            _ => new DateOnly(local.Year, local.Month, 1),
        };
    }
}
