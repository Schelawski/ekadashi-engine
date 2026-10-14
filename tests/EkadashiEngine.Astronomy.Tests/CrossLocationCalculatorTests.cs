using EkadashiEngine.Astronomy.SwissEphemeris;
using EkadashiEngine.Core;
using EkadashiEngine.Core.Astronomy;
using EkadashiEngine.Core.Comparison;
using EkadashiEngine.Core.Rules;

namespace EkadashiEngine.Astronomy.Tests;

public sealed class CrossLocationCalculatorTests
{
    [Fact]
    public void SameTimeZoneDoesNotReplaceCoordinatesOrSunrise()
    {
        var berlin = LocationCatalog.DefaultLocations[0];
        var munich = new Location("Munich", "DE", 48.1372, 11.5756, "Europe/Berlin");
        var sky = new SwissEphemerisAstronomyService();
        var result = new CrossLocationCalculator(new YearAstronomyCalculator(sky, sky, new TithiCalculator()), new NeverCalledCalendar())
            .Calculate(2026, [berlin, munich], Tradition.TechnicalSunriseCandidate, TestContext.Current.CancellationToken);
        Assert.Equal(365, result.Days.Count);
        Assert.NotEqual(LocationKey.From(berlin), LocationKey.From(munich));
        var january = result.Days[0].Locations;
        Assert.Equal(january[0].Location.TimeZoneId, january[1].Location.TimeZoneId);
        Assert.True(Math.Abs((january[0].Astronomy.Sunrise.SunriseUtc!.Value - january[1].Astronomy.Sunrise.SunriseUtc!.Value).TotalMinutes) > 1);
    }

    [Fact]
    public void AllNineCatalogLocationsUseTheSameLocalYearAndKeepTheirOwnOffsets()
    {
        var sky = new SwissEphemerisAstronomyService();
        var result = new CrossLocationCalculator(new YearAstronomyCalculator(sky, sky, new TithiCalculator()), new NeverCalledCalendar())
            .Calculate(2026, LocationCatalog.DefaultLocations, Tradition.TechnicalSunriseCandidate, TestContext.Current.CancellationToken);
        Assert.Equal(9, result.Locations.Count);
        Assert.Equal(new DateOnly(2026, 1, 1), result.Days[0].LocalDate);
        Assert.Equal(new DateOnly(2026, 12, 31), result.Days[^1].LocalDate);
        Assert.Contains(result.Days, day => day.HasDifferences);
        Assert.All(result.Days, day =>
        {
            Assert.Equal(9, day.Locations.Count);
            Assert.All(day.Locations, item =>
            {
                Assert.Equal(day.LocalDate, DateOnly.FromDateTime(item.Astronomy.Sunrise.SunriseLocal!.Value.DateTime));
                Assert.Equal(item.Astronomy.IsEkadashiCandidate, item.Selected);
            });
        });
    }

    [Fact]
    public void TraditionalComparisonUsesTheRuleRatherThanSunriseCandidates()
    {
        var sky = new SwissEphemerisAstronomyService();
        var fasting = new FastingCalendarCalculator(new TraditionDataCalculator(sky, new TithiIntervalCalculator(sky, new TithiCalculator())), new AnandaMargaRule());
        var result = new CrossLocationCalculator(new YearAstronomyCalculator(sky, sky, new TithiCalculator()), fasting)
            .Calculate(2026, [LocationCatalog.DefaultLocations[0], LocationCatalog.DefaultLocations[6]], Tradition.AnandaMarga, TestContext.Current.CancellationToken);
        var berlinJanuary = result.Days.Single(day => day.LocalDate == new DateOnly(2026, 1, 13)).Locations[0];
        Assert.True(berlinJanuary.Selected);
        Assert.False(berlinJanuary.Astronomy.IsEkadashiCandidate);
        Assert.Contains(result.Days, day => day.HasDifferences);
        Assert.All(result.Locations, location => Assert.False(location.Calendar!.IsCalendarValidated));
    }

    [Fact]
    public void DisplayMetadataDoesNotDefineCalculationIdentity()
    {
        var first = new Location("First", "DE", 52.52, 13.405, "Europe/Berlin");
        var renamed = new Location("Renamed", "XX", 52.52, 13.405, "Europe/Berlin");
        Assert.Equal(LocationKey.From(first), LocationKey.From(renamed));
        var calculator = new CrossLocationCalculator(new NeverCalledAstronomy(), new NeverCalledCalendar());
        Assert.Throws<ArgumentException>(() => calculator.Calculate(2026, [first, renamed], Tradition.AnandaMarga, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void CancellationAfterOneLocationReturnsNoPartialComparison()
    {
        using var cancellation = new CancellationTokenSource();
        var provider = new SyntheticAstronomy();
        var progress = new InlineProgress(value => { Assert.Equal(1, value.Completed); cancellation.Cancel(); });
        Assert.Throws<OperationCanceledException>(() => new CrossLocationCalculator(provider, new NeverCalledCalendar())
            .Calculate(2026, LocationCatalog.DefaultLocations, Tradition.TechnicalSunriseCandidate, cancellation.Token, progress));
        Assert.Equal(1, provider.Calls);
    }

    [Fact]
    public void LeapYearAndUnknownDaysArePreserved()
    {
        var result = new CrossLocationCalculator(new SyntheticAstronomy(), new NeverCalledCalendar())
            .Calculate(2028, LocationCatalog.DefaultLocations.Take(2).ToArray(), Tradition.TechnicalSunriseCandidate, TestContext.Current.CancellationToken);
        Assert.Equal(366, result.Days.Count);
        Assert.True(result.Days[0].HasUnknown);
        Assert.False(result.Days[0].HasDifferences);
        Assert.All(result.Days[0].Locations, day => Assert.Null(day.Selected));
    }

    [Theory]
    [InlineData(1899, Tradition.TechnicalSunriseCandidate)]
    [InlineData(2101, Tradition.TechnicalSunriseCandidate)]
    [InlineData(2026, (Tradition)999)]
    public void InvalidRequestsAreRejectedBeforeAstronomy(int year, Tradition tradition) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new CrossLocationCalculator(new NeverCalledAstronomy(), new NeverCalledCalendar())
            .Calculate(year, LocationCatalog.DefaultLocations, tradition, TestContext.Current.CancellationToken));

    [Fact]
    public void AComparisonRequiresAtLeastTwoLocations() =>
        Assert.Throws<ArgumentException>(() => new CrossLocationCalculator(new NeverCalledAstronomy(), new NeverCalledCalendar())
            .Calculate(2026, [LocationCatalog.DefaultLocations[0]], Tradition.TechnicalSunriseCandidate, TestContext.Current.CancellationToken));

    private sealed class InlineProgress(Action<CrossLocationProgress> report) : IProgress<CrossLocationProgress>
    {
        public void Report(CrossLocationProgress value) => report(value);
    }
    private sealed class NeverCalledAstronomy : IYearAstronomyCalculator
    {
        public YearAstronomyResult Calculate(int year, Location location, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
    }
    private sealed class NeverCalledCalendar : IFastingCalendarCalculator
    {
        public TraditionYearResult Calculate(int year, Location location, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
    }
    private sealed class SyntheticAstronomy : IYearAstronomyCalculator
    {
        public int Calls { get; private set; }
        public YearAstronomyResult Calculate(int year, Location location, CancellationToken cancellationToken = default)
        {
            Calls++;
            return new(location, year, Enumerable.Range(0, DateTime.IsLeapYear(year) ? 366 : 365).Select(index =>
            {
                var date = new DateOnly(year, 1, 1).AddDays(index);
                return new DailyAstronomyResult(date, new(date, SunriseDefinition.UpperLimbWithRefraction, SunriseStatus.NoSunriseOnLocalDate, null, null), null, null);
            }).ToArray());
        }
    }
}
