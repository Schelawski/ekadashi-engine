using EkadashiEngine.Astronomy.SwissEphemeris;
using EkadashiEngine.Core;
using EkadashiEngine.Core.Astronomy;

namespace EkadashiEngine.Astronomy.Tests;

public sealed class YearAstronomyCalculatorTests
{
    [Theory]
    [InlineData(2026, 365)]
    [InlineData(2028, 366)]
    public void VisitsEveryLocalDateAndUsesTheSunriseInstant(int year, int count)
    {
        var provider = new RecordingProvider();
        var result = new YearAstronomyCalculator(provider, provider, new TithiCalculator())
            .Calculate(year, LocationCatalog.DefaultLocations[0], TestContext.Current.CancellationToken);

        Assert.Equal(count, result.Days.Count);
        Assert.Equal(new DateOnly(year, 1, 1), result.Days[0].LocalDate);
        Assert.Equal(new DateOnly(year, 12, 31), result.Days[^1].LocalDate);
        Assert.Equal(count, result.Days.Select(day => day.LocalDate).Distinct().Count());
        Assert.Equal(result.Days.Select(day => day.Sunrise.SunriseUtc!.Value), provider.PositionInstants);
        Assert.All(result.Days, day => Assert.True(day.IsEkadashiCandidate));
    }

    [Theory]
    [InlineData(SunriseStatus.NoSunriseOnLocalDate)]
    [InlineData(SunriseStatus.SkippedLocalDate)]
    public void MissingSunriseIsNotMisclassifiedAsANonCandidate(SunriseStatus status)
    {
        var provider = new RecordingProvider { Status = status };
        var result = new YearAstronomyCalculator(provider, provider, new TithiCalculator())
            .Calculate(2026, LocationCatalog.DefaultLocations[0], TestContext.Current.CancellationToken);
        Assert.Empty(provider.PositionInstants);
        Assert.All(result.Days, day =>
        {
            Assert.Null(day.Tithi);
            Assert.Null(day.Positions);
            Assert.Null(day.IsEkadashiCandidate);
        });
    }

    [Fact]
    public void CancellationDuringCalculationStopsBeforeTheNextDay()
    {
        using var cancellation = new CancellationTokenSource();
        var provider = new RecordingProvider { OnPosition = cancellation.Cancel };
        var calculator = new YearAstronomyCalculator(provider, provider, new TithiCalculator());
        Assert.Throws<OperationCanceledException>(() => calculator.Calculate(2026, LocationCatalog.DefaultLocations[0], cancellation.Token));
        Assert.Single(provider.PositionInstants);
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2101)]
    public void RejectsYearsOutsideTheWorkflowRange(int year)
    {
        var provider = new RecordingProvider();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new YearAstronomyCalculator(provider, provider, new TithiCalculator()).Calculate(year, LocationCatalog.DefaultLocations[0], TestContext.Current.CancellationToken));
        Assert.Empty(provider.PositionInstants);
    }

    [Theory]
    [InlineData(10, false)]
    [InlineData(11, true)]
    [InlineData(12, false)]
    [InlineData(25, false)]
    [InlineData(26, true)]
    [InlineData(27, false)]
    public void OnlyTheTwoEkadashiTithisAreCandidates(int number, bool expected)
    {
        var date = new DateOnly(2026, 1, 1);
        var day = new DailyAstronomyResult(date,
            new(date, SunriseDefinition.UpperLimbWithRefraction, SunriseStatus.Available, null, null),
            new(0, (number - 1) * 12), new((number - 1) * 12, number));
        Assert.Equal(expected, day.IsEkadashiCandidate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void EveryCatalogLocationProducesAComplete2026Year(int locationIndex)
    {
        var provider = new SwissEphemerisAstronomyService();
        var location = LocationCatalog.DefaultLocations[locationIndex];
        var result = new YearAstronomyCalculator(provider, provider, new TithiCalculator()).Calculate(2026, location, TestContext.Current.CancellationToken);
        Assert.Equal(365, result.Days.Count);
        Assert.Contains(result.Days, day => day.IsEkadashiCandidate == true);
        Assert.All(result.Days, day =>
        {
            Assert.Equal(SunriseStatus.Available, day.Sunrise.Status);
            Assert.Equal(day.LocalDate, DateOnly.FromDateTime(day.Sunrise.SunriseLocal!.Value.DateTime));
            Assert.Equal(day.Sunrise.SunriseLocal.Value.ToUniversalTime(), day.Sunrise.SunriseUtc);
            Assert.InRange(day.Tithi!.TithiNumber, 1, 30);
        });
    }

    private sealed class RecordingProvider : IAstronomyService, ISunriseCalculator
    {
        public List<DateTimeOffset> PositionInstants { get; } = [];
        public SunriseStatus Status { get; init; } = SunriseStatus.Available;
        public Action? OnPosition { get; init; }

        public PlanetaryPositions GetPositions(DateTimeOffset instant)
        {
            PositionInstants.Add(instant);
            OnPosition?.Invoke();
            return new(350, 110);
        }

        public SunriseResult CalculateSunrise(DateOnly date, Location location, SunriseDefinition definition)
        {
            if (Status != SunriseStatus.Available)
                return new(date, definition, Status, null, null);
            var utc = new DateTimeOffset(date.Year, date.Month, date.Day, 6, 30, 0, TimeSpan.Zero);
            return new(date, definition, Status, utc, location.GetLocalTime(utc));
        }
    }
}
