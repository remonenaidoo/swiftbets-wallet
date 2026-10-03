using SwiftBets.Wallet.Domain.ResponsibleGambling;

namespace SwiftBets.Wallet.Domain.Tests;

public sealed class SpendPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 10, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("2026-10-04T21:59:00Z", "2026-10-04", "2026-09-28", "2026-10-01")]
    [InlineData("2026-10-04T22:00:00Z", "2026-10-05", "2026-10-05", "2026-10-01")]
    [InlineData("2026-09-30T22:30:00Z", "2026-10-01", "2026-09-28", "2026-10-01")]
    public void Periods_follow_the_south_african_calendar_with_monday_weeks(string at, string day, string week, string month)
    {
        var instant = DateTimeOffset.Parse(at, System.Globalization.CultureInfo.InvariantCulture);

        SpendPeriods.StartOf(SpendPeriod.Day, instant).ShouldBe(DateOnly.Parse(day, System.Globalization.CultureInfo.InvariantCulture));
        SpendPeriods.StartOf(SpendPeriod.Week, instant).ShouldBe(DateOnly.Parse(week, System.Globalization.CultureInfo.InvariantCulture));
        SpendPeriods.StartOf(SpendPeriod.Month, instant).ShouldBe(DateOnly.Parse(month, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Stake_up_to_the_limit_passes_and_one_cent_more_is_refused()
    {
        var limit = new SpendLimit(SpendKind.Stake, SpendPeriod.Day, 1_000, null, null);
        var rules = new GamblingRules([limit], []);
        var counters = new SpendCounters(new SpendTotals(600, 0, 0), SpendTotals.Zero, SpendTotals.Zero);

        SpendPolicy.CheckStake(rules, counters, 400, Now).ShouldBeNull();
        SpendPolicy.CheckStake(rules, counters, 401, Now).ShouldBe(new SpendRefusal(WalletFailure.LimitExceeded, "stake limit per day: 400 left", limit));
    }

    [Fact]
    public void Tightest_period_is_reported_first()
    {
        var rules = new GamblingRules(
            [new SpendLimit(SpendKind.Stake, SpendPeriod.Month, 100, null, null), new SpendLimit(SpendKind.Stake, SpendPeriod.Day, 100, null, null)], []);

        SpendPolicy.CheckStake(rules, SpendCounters.Zero, 101, Now)!.Detail.ShouldStartWith("stake limit per day");
    }

    [Fact]
    public void Loss_counts_stakes_less_winnings_and_never_goes_negative()
    {
        new SpendTotals(500, 800, 0).Loss.ShouldBe(0);
        var rules = new GamblingRules([new SpendLimit(SpendKind.Loss, SpendPeriod.Week, 1_000, null, null)], []);
        var counters = new SpendCounters(SpendTotals.Zero, new SpendTotals(1_500, 700, 0), SpendTotals.Zero);

        SpendPolicy.CheckStake(rules, counters, 200, Now).ShouldBeNull();
        SpendPolicy.CheckStake(rules, counters, 201, Now)!.Failure.ShouldBe(WalletFailure.LimitExceeded);
    }

    [Fact]
    public void Due_removal_lifts_the_cap()
    {
        var limit = new SpendLimit(SpendKind.Deposit, SpendPeriod.Day, 100, null, Now.AddHours(-1));

        limit.CapAt(Now).ShouldBeNull();
        SpendPolicy.CheckDeposit(new GamblingRules([limit], []), SpendCounters.Zero, 1_000_000, Now).ShouldBeNull();
    }

    [Fact]
    public void Blocks_apply_only_to_their_movement_and_only_while_active()
    {
        var noBetting = new GamblingRules([], [new SpendBlock(BlockKind.NoBetting, Now.AddDays(-1), null)]);
        var ended = new GamblingRules([], [new SpendBlock(BlockKind.Exclusion, Now.AddDays(-10), Now.AddDays(-1))]);

        SpendPolicy.CheckStake(noBetting, SpendCounters.Zero, 1, Now)!.Detail.ShouldBe("account blocked: NoBetting");
        SpendPolicy.CheckDeposit(noBetting, SpendCounters.Zero, 1, Now).ShouldBeNull();
        SpendPolicy.CheckStake(ended, SpendCounters.Zero, 1, Now).ShouldBeNull();
    }
}
