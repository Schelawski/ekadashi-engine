using System.Text.Json;
using System.Text.Json.Serialization;
using EkadashiEngine.Astronomy.SwissEphemeris;
using EkadashiEngine.Core;
using EkadashiEngine.Core.Astronomy;
using EkadashiEngine.Core.Diagnostics;
using EkadashiEngine.Core.Export;
using EkadashiEngine.Core.Rules;

namespace EkadashiEngine.Astronomy.Tests;

public sealed class PortableTestVectorTests
{
    [Theory]
    [InlineData("berlin-2026-v1.json")]
    [InlineData("sydney-2026-v1.json")]
    [InlineData("tromso-2026-v1.json")]
    [InlineData("apia-2011-v1.json")]
    [InlineData("rule-edges-2026-v1.json")]
    [InlineData("vaishnava-berlin-2026-v1.json")]
    [InlineData("vaishnava-edges-2026-v1.json")]
    public void VersionedVectorsReplayAstronomyAndPureTraditionDecisions(string file)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
        var document = JsonSerializer.Deserialize<TestVectorDocument>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestVectors", file)), options)!;
        Assert.Equal(1, document.SchemaVersion);
        Assert.Equal("testVectors", document.Kind);
        var location = new Location(document.Location.DisplayName, document.Location.Country, document.Location.Latitude,
            document.Location.Longitude, document.Location.TimeZone);
        var decisions = new List<RuleDecision>();
        foreach (var testCase in document.RuleCases)
        {
            var prepared = new PreparedTraditionYear(location, document.Year,
                testCase.SolarDays.Select(day => new SolarDay(day.Date, day.SunriseUtc, day.NextSunriseUtc)).ToArray(),
                [new(testCase.Tithi, testCase.StartUtc, testCase.EndUtc)], testCase.VaishnavaDays);
            ITraditionRule rule = document.Rule.Tradition == Tradition.GaudiyaVaishnava ? new VaishnavaRule() : new AnandaMargaRule();
            var result = rule.Evaluate(prepared, TestContext.Current.CancellationToken);
            var decision = Assert.Single(result.Decisions);
            Assert.Equal(testCase.ExpectedFastingDate, decision.FastingDate);
            Assert.Equal(testCase.ExpectedStatus, decision.Status);
            foreach (var candidate in decision.Candidates)
                Assert.InRange(Math.Abs(candidate.Overlap.TotalSeconds - testCase.SolarDays.Single(day => day.Date == candidate.LocalDate).OverlapSeconds), 0, 0.00001);
            decisions.Add(decision);
        }
        var sky = new SwissEphemerisAstronomyService();
        var calculator = new DayAstronomyCalculator(sky, sky, new TithiCalculator());
        var actualDays = document.Vectors.Select(vector => calculator.Calculate(vector.Date, location)).ToArray();
        var calendar = document.Rule.Tradition == Tradition.TechnicalSunriseCandidate ? null
            : new TraditionYearResult(location, document.Year, document.Rule.Tradition, document.Rule.Version, document.Rule.Source!,
                false, decisions.Where(d => d.FastingDate.HasValue).Select(d => d.FastingDate!.Value).Distinct().ToArray(), decisions);
        var snapshot = new CalculationSnapshot(new(location, document.Year, actualDays), calendar, null, [], document.Astronomy);
        for (var index = 0; index < actualDays.Length; index++)
        {
            var actual = actualDays[index];
            var expected = document.Vectors[index];
            Assert.Equal(expected.SunriseStatus, actual.Sunrise.Status);
            Assert.Equal(expected.Tithi, actual.Tithi?.TithiNumber);
            Assert.Equal(expected.IsEkadashiCandidate, actual.IsEkadashiCandidate);
            Assert.Equal(expected.Fasting, snapshot.IsFasting(actual));
            Assert.Equal(expected.OfficialFasting, document.Reference?.FastingDates.Contains(expected.Date));
            AssertNullableClose(expected.SunLongitude, actual.Positions?.SunLongitude, document.Tolerances.LongitudeDegrees);
            AssertNullableClose(expected.MoonLongitude, actual.Positions?.MoonLongitude, document.Tolerances.LongitudeDegrees);
            AssertNullableClose(expected.AngularDifference, actual.Tithi?.AngularDifference, document.Tolerances.LongitudeDegrees);
            if (expected.SunriseUtc.HasValue)
            {
                Assert.NotNull(actual.Sunrise.SunriseUtc);
                Assert.InRange(Math.Abs((expected.SunriseUtc.Value - actual.Sunrise.SunriseUtc.Value).TotalSeconds), 0, document.Tolerances.SunriseSeconds);
                Assert.Equal(expected.SunriseLocal!.Value.Offset, actual.Sunrise.SunriseLocal!.Value.Offset);
                Assert.Equal(expected.Date, DateOnly.FromDateTime(actual.Sunrise.SunriseLocal.Value.DateTime));
            }
            else { Assert.Null(actual.Sunrise.SunriseUtc); Assert.Null(actual.Sunrise.SunriseLocal); }
        }
        if (document.Astronomy.Provider != "Synthetic")
        {
            foreach (var observation in document.RuleCases.SelectMany(testCase => testCase.VaishnavaDays ?? []).DistinctBy(day => day.LocalDate))
            {
                var rise = sky.CalculateSunrise(observation.LocalDate, location).SunriseUtc;
                var sunset = sky.GetSunsetUtc(observation.LocalDate, location);
                Assert.NotNull(rise);
                Assert.NotNull(sunset);
                Assert.InRange(Math.Abs((sunset.Value - observation.SunsetUtc!.Value).TotalSeconds), 0, document.Tolerances.SunriseSeconds);
                Assert.Equal(observation.SunriseUtc!.Value.AddMinutes(-96), observation.ArunodayaUtc);
                Assert.Equal(observation.Nakshatra, sky.GetNakshatra(rise.Value));
                foreach (var (instant, expected) in new[] { (rise.Value, observation.SunriseTithi), (rise.Value.AddMinutes(-96), observation.ArunodayaTithi), (sunset.Value, observation.SunsetTithi) })
                {
                    var position = sky.GetPositions(instant);
                    Assert.Equal(expected, new TithiCalculator().Calculate(position.SunLongitude, position.MoonLongitude).TithiNumber);
                }
            }
            // Recorded lunar boundaries are checked against the ephemeris as well as the rule.
            foreach (var testCase in document.RuleCases)
            {
                var start = sky.GetPositions(testCase.StartUtc.AddSeconds(document.Tolerances.TithiBoundarySeconds));
                var before = sky.GetPositions(testCase.StartUtc.AddSeconds(-document.Tolerances.TithiBoundarySeconds));
                Assert.Equal(testCase.Tithi, new TithiCalculator().Calculate(start.SunLongitude, start.MoonLongitude).TithiNumber);
                Assert.Equal(testCase.Tithi - 1, new TithiCalculator().Calculate(before.SunLongitude, before.MoonLongitude).TithiNumber);
                var end = sky.GetPositions(testCase.EndUtc.AddSeconds(document.Tolerances.TithiBoundarySeconds));
                var beforeEnd = sky.GetPositions(testCase.EndUtc.AddSeconds(-document.Tolerances.TithiBoundarySeconds));
                Assert.Equal(testCase.Tithi, new TithiCalculator().Calculate(beforeEnd.SunLongitude, beforeEnd.MoonLongitude).TithiNumber);
                Assert.Equal(testCase.Tithi + 1, new TithiCalculator().Calculate(end.SunLongitude, end.MoonLongitude).TithiNumber);
            }
        }
    }

    private static void AssertNullableClose(double? expected, double? actual, double tolerance)
    {
        if (!expected.HasValue) { Assert.Null(actual); return; }
        Assert.NotNull(actual);
        Assert.InRange(Math.Abs(expected.Value - actual.Value), 0, tolerance);
    }
}
