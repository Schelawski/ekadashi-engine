using EkadashiEngine.Astronomy.SwissEphemeris;
using EkadashiEngine.Core;
using EkadashiEngine.Core.Astronomy;
using EkadashiEngine.Core.ReferenceData;
using EkadashiEngine.Core.Rules;

namespace EkadashiEngine.Astronomy.Tests;

public sealed class VaishnavaRuleTests
{
    private static readonly Location Berlin = LocationCatalog.DefaultLocations[0];
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public void Berlin2026MatchesAllPublishedGcalDatesAndShowsOneDayDifferenceFromAm()
    {
        var sky = new SwissEphemerisAstronomyService();
        var data = new TraditionDataCalculator(sky, new TithiIntervalCalculator(sky, new TithiCalculator())).Calculate(2026, Berlin, Token, true);
        var result = new VaishnavaRule().Evaluate(data, Token);
        var reference = new EmbeddedCalendarReferenceProvider().Find(2026, Berlin, Tradition.GaudiyaVaishnava)!;
        Assert.Equal(reference.FastingDates, result.FastingDates);
        Assert.Equal(24, result.Decisions.Count);
        Assert.False(result.HasUnresolvedDecisions);
        var am = new AnandaMargaRule().Evaluate(data, Token);
        Assert.Contains(new DateOnly(2026, 3, 28), am.FastingDates);
        Assert.Contains(new DateOnly(2026, 3, 29), result.FastingDates);
        Assert.Contains(result.Decisions, d => d.FastingDate == new DateOnly(2026, 3, 29) && d.Explanation.Contains("Viddha"));
        Assert.Contains(result.Decisions, d => d.FastingDate == new DateOnly(2026, 8, 24) && d.Explanation.Contains("Paksha-vardhini"));
    }

    [Theory]
    [InlineData("pure", 12, "Suddha")]
    [InlineData("viddha", 13, "Viddha")]
    [InlineData("omitted", 12, "omitted")]
    [InlineData("unmilani", 13, "Unmilani")]
    [InlineData("trisprsha", 12, "Trisprsha")]
    [InlineData("combined", 13, "Unmilani-Trisprsha")]
    [InlineData("vyanjuli", 13, "Vyanjuli")]
    [InlineData("paksha", 13, "Paksha-vardhini")]
    [InlineData("jaya", 13, "Jaya")]
    [InlineData("jayanti", 13, "Jayanti")]
    [InlineData("papanasini", 13, "Papa-nasini")]
    [InlineData("vijaya", 13, "Vijaya")]
    public void PureRuleCoversGaurabdaDecisionBranches(string scenario, int expectedDay, string reason)
    {
        var decision = Assert.Single(new VaishnavaRule().Evaluate(Prepared(scenario), Token).Decisions);
        Assert.Equal(new DateOnly(2026, 1, expectedDay), decision.FastingDate);
        Assert.Equal(RuleDecisionStatus.Selected, decision.Status);
        Assert.Contains(reason, decision.Explanation);
    }

    [Fact]
    public void MissingPolarOrNakshatraDataNeverProducesAnAssumedDate()
    {
        foreach (var scenario in new[] { "missing-rise", "missing-nakshatra", "missing-lookahead" })
        {
            var decision = Assert.Single(new VaishnavaRule().Evaluate(Prepared(scenario), Token).Decisions);
            Assert.Null(decision.FastingDate);
            Assert.Equal(RuleDecisionStatus.MissingRuleData, decision.Status);
        }
    }

    [Fact]
    public void SelectionPrecedesYearFilteringAndCancellationIsHonored()
    {
        var input = Prepared("pure");
        Assert.Empty(new VaishnavaRule().Evaluate(input with { Year = 2025 }, Token).Decisions);
        Assert.Throws<OperationCanceledException>(() => new VaishnavaRule().Evaluate(input, new CancellationToken(true)));
        Assert.Throws<ArgumentException>(() => new VaishnavaRule().Evaluate(input with { VaishnavaDays = [input.VaishnavaDays![0], input.VaishnavaDays[0]] }, Token));
    }

    [Fact]
    public void NineInternationalLocationsDispatchVaishnavaWithoutSubstitutingAm()
    {
        var sky = new SwissEphemerisAstronomyService();
        var tithi = new TithiCalculator();
        var fasting = new FastingCalendarCalculator(new TraditionDataCalculator(sky, new TithiIntervalCalculator(sky, tithi)), new AnandaMargaRule());
        var result = new EkadashiEngine.Core.Comparison.CrossLocationCalculator(new YearAstronomyCalculator(sky, sky, tithi), fasting)
            .Calculate(2026, LocationCatalog.DefaultLocations, Tradition.GaudiyaVaishnava, Token);
        Assert.Equal(365, result.Days.Count);
        Assert.All(result.Locations, place =>
        {
            Assert.Equal(Tradition.GaudiyaVaishnava, place.Calendar!.Tradition);
            Assert.InRange(place.Calendar.FastingDates.Count, 24, 26);
            Assert.False(place.Calendar.HasUnresolvedDecisions);
        });
    }

    [Theory]
    [InlineData(1900, "Berlin", 52.52, 13.405, "Europe/Berlin", false)]
    [InlineData(2100, "Sydney", -33.8688, 151.2093, "Australia/Sydney", false)]
    [InlineData(2026, "Tromso", 69.6492, 18.9553, "Europe/Oslo", true)]
    [InlineData(2011, "Apia", -13.8333, -171.7667, "Pacific/Apia", false)]
    public void SupportedYearBoundsAndPolarInputsRemainExplicit(int year, string name, double lat, double lon, string zone, bool unresolved)
    {
        var sky = new SwissEphemerisAstronomyService();
        var input = new TraditionDataCalculator(sky, new TithiIntervalCalculator(sky, new TithiCalculator()))
            .Calculate(year, new Location(name, "test", lat, lon, zone), Token, true);
        var result = new VaishnavaRule().Evaluate(input, Token);
        Assert.Equal(unresolved, result.HasUnresolvedDecisions);
        Assert.All(result.FastingDates, date => Assert.Equal(year, date.Year));
    }

    public static PreparedTraditionYear Prepared(string scenario)
    {
        var days = Enumerable.Range(10, 17).Select(day =>
        {
            var rise = new DateTimeOffset(2026, 1, day, 6, 0, 0, TimeSpan.Zero);
            return new VaishnavaDay(new(2026, 1, day), rise, rise.AddMinutes(-96), rise.AddHours(12), day - 1, day - 1, day - 1, day % 27);
        }).ToArray();
        void Set(int day, Func<VaishnavaDay, VaishnavaDay> change) => days[day - 10] = change(days[day - 10]);
        var start = new DateTimeOffset(2026, 1, 12, 1, 0, 0, TimeSpan.Zero);
        var end = start.AddDays(1);
        switch (scenario)
        {
            case "viddha": Set(12, d => d with { ArunodayaTithi = 10 }); start = start.AddHours(4); break;
            case "omitted": Set(12, d => d with { SunriseTithi = 12, ArunodayaTithi = 12, SunsetTithi = 12 }); start = start.AddDays(-1).AddHours(6); end = end.AddDays(-1); break;
            case "unmilani": case "combined":
                Set(13, d => d with { SunriseTithi = 11, ArunodayaTithi = 11 }); end = end.AddHours(6);
                if (scenario == "unmilani") Set(14, d => d with { SunriseTithi = 12 });
                break;
            case "trisprsha": Set(13, d => d with { SunriseTithi = 13 }); break;
            case "vyanjuli": Set(14, d => d with { SunriseTithi = 12 }); break;
            case "paksha": Set(17, d => d with { SunriseTithi = 15 }); break;
            case "jaya": case "jayanti": case "papanasini": case "vijaya":
                var nak = scenario switch { "jaya" => 6, "jayanti" => 3, "papanasini" => 7, _ => 21 };
                Set(13, d => d with { Nakshatra = nak }); Set(14, d => d with { Nakshatra = nak }); break;
            case "missing-rise": Set(12, d => d with { SunriseUtc = null, SunriseTithi = null, ArunodayaTithi = null }); break;
            case "missing-nakshatra": Set(13, d => d with { Nakshatra = null }); break;
            case "missing-lookahead": days = days.Take(6).ToArray(); break;
        }
        var solar = days.Select(d => new SolarDay(d.LocalDate, d.SunriseUtc, d.SunriseUtc?.AddDays(1))).ToArray();
        return new(Berlin, 2026, solar, [new(11, start, end)], days);
    }
}
