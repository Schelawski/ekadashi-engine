using EkadashiEngine.Core;
using EkadashiEngine.Core.Astronomy;
using EkadashiEngine.Core.Rules;

namespace EkadashiEngine.Astronomy.Tests;

public sealed class AnandaMargaRuleTests
{
    private static readonly DateTimeOffset Dawn = new(2026, 1, 13, 6, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(11)]
    [InlineData(26)]
    public void SelectsGreatestOverlapRatherThanTithiAtSunrise(int tithi)
    {
        var result = Evaluate(new(tithi, Dawn.AddHours(4), Dawn.AddHours(31)));
        Assert.Equal(new DateOnly(2026, 1, 13), Assert.Single(result.FastingDates));
        var decision = Assert.Single(result.Decisions);
        Assert.Equal(TimeSpan.FromHours(20), decision.Candidates[0].Overlap);
        Assert.Equal(TimeSpan.FromHours(7), decision.Candidates[1].Overlap);
        Assert.Equal(RuleDecisionStatus.Selected, decision.Status);
        Assert.Contains("greatest", decision.Explanation);
        Assert.False(result.IsCalendarValidated);
    }

    [Fact]
    public void ShortTithiEntirelyBetweenSunrisesIsNotLost()
    {
        var result = Evaluate(new(11, Dawn.AddHours(2), Dawn.AddHours(20)));
        Assert.Equal(new DateOnly(2026, 1, 13), Assert.Single(result.FastingDates));
    }

    [Fact]
    public void LongTithiSpanningTwoSunrisesProducesOnlyOneDate()
    {
        var result = Evaluate(new(26, Dawn.AddHours(-1), Dawn.AddHours(25)));
        Assert.Single(result.FastingDates);
        Assert.Equal(3, Assert.Single(result.Decisions).Candidates.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    public void TiesWithinNumericalUncertaintyRemainUnresolved(double extraSeconds)
    {
        var result = Evaluate(new(11, Dawn.AddHours(12), Dawn.AddHours(36).AddSeconds(extraSeconds)));
        Assert.Empty(result.FastingDates);
        Assert.True(result.HasUnresolvedDecisions);
        Assert.Equal(RuleDecisionStatus.AmbiguousOverlap, Assert.Single(result.Decisions).Status);
    }

    [Fact]
    public void MissingSunriseDoesNotBecomeAnArtificialTwentyFourHourDay()
    {
        var data = Data(new(11, Dawn.AddHours(4), Dawn.AddHours(31)));
        data = data with { SolarDays = data.SolarDays.Select(d => d.LocalDate.Day == 13 ? d with { NextSunriseUtc = null } : d).ToArray() };
        var result = new AnandaMargaRule().Evaluate(data, TestContext.Current.CancellationToken);
        Assert.Empty(result.FastingDates);
        Assert.Equal(RuleDecisionStatus.MissingSunriseCoverage, Assert.Single(result.Decisions).Status);
    }

    [Fact]
    public void UsesActualUtcDurationAcrossOffsetChanges()
    {
        var data = Data(new(11, Dawn.AddHours(12), Dawn.AddHours(37)));
        data = data with { SolarDays = [new(new(2026, 1, 13), Dawn, Dawn.AddHours(23)), new(new(2026, 1, 14), Dawn.AddHours(23), Dawn.AddHours(47))] };
        var result = new AnandaMargaRule().Evaluate(data, TestContext.Current.CancellationToken);
        Assert.Equal(new DateOnly(2026, 1, 14), Assert.Single(result.FastingDates));
        Assert.Equal(TimeSpan.FromHours(14), Assert.Single(result.Decisions).Candidates[0].Overlap);
    }

    [Fact]
    public void SelectsBeforeFilteringTheRequestedYear()
    {
        var start = new DateTimeOffset(2025, 12, 31, 6, 0, 0, TimeSpan.Zero);
        var data = new PreparedTraditionYear(LocationCatalog.DefaultLocations[0], 2026,
            [new(new(2025, 12, 31), start, start.AddDays(1)), new(new(2026, 1, 1), start.AddDays(1), start.AddDays(2))],
            [new(11, start.AddHours(2), start.AddHours(28))]);
        var result = new AnandaMargaRule().Evaluate(data, TestContext.Current.CancellationToken);
        Assert.Empty(result.FastingDates);
        Assert.Empty(result.Decisions);
        result = new AnandaMargaRule().Evaluate(data with { Tithis = [new(11, start.AddHours(20), start.AddHours(44))] }, TestContext.Current.CancellationToken);
        Assert.Equal(new DateOnly(2026, 1, 1), Assert.Single(result.FastingDates));
    }

    [Fact]
    public void HalfOpenIntervalsDoNotIncludeTheFollowingDay()
    {
        var result = Evaluate(new(11, Dawn, Dawn.AddDays(1)));
        Assert.Single(Assert.Single(result.Decisions).Candidates);
    }

    [Fact]
    public void OtherTithisAreIgnored() => Assert.Empty(Evaluate(new(12, Dawn, Dawn.AddDays(1))).FastingDates);

    [Fact]
    public void CancellationIsObserved()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => new AnandaMargaRule().Evaluate(Data(new(11, Dawn, Dawn.AddDays(1))), cancellation.Token));
    }

    [Fact]
    public void OverlappingSolarWindowsAreRejected()
    {
        var data = Data(new(11, Dawn, Dawn.AddDays(1)));
        data = data with { SolarDays = [new(new(2026, 1, 13), Dawn, Dawn.AddDays(2)), new(new(2026, 1, 14), Dawn.AddDays(1), Dawn.AddDays(3))] };
        Assert.Throws<ArgumentException>(() => new AnandaMargaRule().Evaluate(data, TestContext.Current.CancellationToken));
    }

    private static TraditionYearResult Evaluate(TithiInterval interval) =>
        new AnandaMargaRule().Evaluate(Data(interval), TestContext.Current.CancellationToken);

    private static PreparedTraditionYear Data(TithiInterval interval) =>
        new(LocationCatalog.DefaultLocations[0], 2026,
            Enumerable.Range(-1, 4).Select(i => new SolarDay(new DateOnly(2026, 1, 13).AddDays(i), Dawn.AddDays(i), Dawn.AddDays(i + 1))).ToArray(), [interval]);
}
