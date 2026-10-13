using System.Text.Json;
using EkadashiEngine.Astronomy.SwissEphemeris;
using EkadashiEngine.Core;
using EkadashiEngine.Core.Astronomy;
using EkadashiEngine.Core.Rules;

namespace EkadashiEngine.Astronomy.Tests;

public sealed class AnandaMargaCalendarTests(ITestOutputHelper output)
{
    [Fact]
    public void Berlin2026ComparisonRecordsKnownDisagreementInsteadOfClaimingValidation()
    {
        var provider = new SwissEphemerisAstronomyService();
        var tithi = new TithiCalculator();
        var data = new TraditionDataCalculator(provider, new TithiIntervalCalculator(provider, tithi))
            .Calculate(2026, LocationCatalog.DefaultLocations[0], TestContext.Current.CancellationToken);
        var result = new AnandaMargaRule().Evaluate(data, TestContext.Current.CancellationToken);
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "berlin-sector-2026.json")));
        var official = fixture.RootElement.GetProperty("fastingDates").EnumerateArray().Select(v => DateOnly.ParseExact(v.GetString()!, "yyyy-MM-dd")).ToArray();
        Assert.Equal(24, official.Length);
        Assert.Equal(24, result.FastingDates.Count);
        Assert.False(result.HasUnresolvedDecisions);
        Assert.False(result.IsCalendarValidated);
        // Regression for the documented method, deliberately separate from official reference dates.
        int[] dayOffsets = [-1, -1, -1, 0, 0, 0, 0, -1, -1, 0, 0, -1, 0, 0, -1, 0, -1, -1, 0, -1, 0, 0, -1, -1];
        Assert.Equal(official.Select((date, i) => date.AddDays(dayOffsets[i])), result.FastingDates);
        output.WriteLine($"Matches: {result.FastingDates.Intersect(official).Count()}/{official.Length}");
        for (var i = 0; i < official.Length; i++)
            output.WriteLine($"{result.FastingDates[i]:yyyy-MM-dd} | {official[i]:yyyy-MM-dd} | {(result.FastingDates[i] == official[i] ? "MATCH" : "MISMATCH")}");
        foreach (var decision in result.Decisions)
        {
            // Independently inspect both sides of each solved boundary using the discrete tithi API.
            AssertBoundary(decision.Tithi.StartUtc, decision.Tithi.TithiNumber);
            AssertBoundary(decision.Tithi.EndUtc, decision.Tithi.TithiNumber % 30 + 1);
        }
        var first = result.Decisions[0];
        output.WriteLine($"January interval: {first.Tithi.StartUtc:O} – {first.Tithi.EndUtc:O}");
        foreach (var candidate in first.Candidates) output.WriteLine($"{candidate.LocalDate}: {candidate.Overlap}");

        void AssertBoundary(DateTimeOffset boundary, int after)
        {
            var beforePosition = provider.GetPositions(boundary.AddSeconds(-1));
            var afterPosition = provider.GetPositions(boundary.AddSeconds(1));
            Assert.Equal((after + 28) % 30 + 1, tithi.Calculate(beforePosition.SunLongitude, beforePosition.MoonLongitude).TithiNumber);
            Assert.Equal(after, tithi.Calculate(afterPosition.SunLongitude, afterPosition.MoonLongitude).TithiNumber);
        }
    }

    [Theory]
    [InlineData(0, 1900)]
    [InlineData(0, 2100)]
    [InlineData(0, 2028)]
    [InlineData(1, 2026)]
    [InlineData(2, 2026)]
    [InlineData(3, 2026)]
    [InlineData(4, 2026)]
    [InlineData(5, 2026)]
    [InlineData(6, 2026)]
    [InlineData(7, 2026)]
    [InlineData(8, 2026)]
    public void CalendarProducesUniqueLocalDatesAcrossLocationsAndSupportedYearLimits(int locationIndex, int year)
    {
        var location = LocationCatalog.DefaultLocations[locationIndex];
        var provider = new SwissEphemerisAstronomyService();
        var result = new FastingCalendarCalculator(
            new TraditionDataCalculator(provider, new TithiIntervalCalculator(provider, new TithiCalculator())),
            new AnandaMargaRule()).Calculate(year, location, TestContext.Current.CancellationToken);
        Assert.InRange(result.FastingDates.Count, 24, 25);
        Assert.False(result.HasUnresolvedDecisions);
        Assert.Equal(result.FastingDates.Count, result.FastingDates.Distinct().Count());
        Assert.All(result.FastingDates, date => Assert.Equal(year, date.Year));
        Assert.All(result.Decisions, decision =>
        {
            var winner = decision.Candidates[0];
            Assert.Equal(decision.FastingDate, winner.LocalDate);
            Assert.Equal(winner.LocalDate, DateOnly.FromDateTime(location.GetLocalTime(winner.SunriseUtc).DateTime));
            Assert.Equal(winner.LocalDate.AddDays(1), DateOnly.FromDateTime(location.GetLocalTime(winner.NextSunriseUtc).DateTime));
        });
    }

    [Fact]
    public void PolarYearReportsUnresolvedDecisionsWithoutInventingDates()
    {
        var location = new Location("Tromsø", "NO", 69.6492, 18.9553, "Europe/Oslo");
        var provider = new SwissEphemerisAstronomyService();
        var result = new FastingCalendarCalculator(
            new TraditionDataCalculator(provider, new TithiIntervalCalculator(provider, new TithiCalculator())),
            new AnandaMargaRule()).Calculate(2026, location, TestContext.Current.CancellationToken);
        Assert.True(result.HasUnresolvedDecisions);
        Assert.Contains(result.Decisions, d => d.Status == RuleDecisionStatus.MissingSunriseCoverage);
        Assert.All(result.Decisions.Where(d => d.Status != RuleDecisionStatus.Selected), d => Assert.Null(d.FastingDate));
    }
}
