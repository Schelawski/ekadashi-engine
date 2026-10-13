using System.Globalization;
using System.Text.Json;
using EkadashiEngine.Astronomy.SwissEphemeris;
using EkadashiEngine.Core;
using EkadashiEngine.Core.Astronomy;

namespace EkadashiEngine.Astronomy.Tests;

public sealed class SwissEphemerisReferenceTests
{
    private readonly SwissEphemerisAstronomyService _astronomy = new();

    public static TheoryData<int> ReferenceCases => new(Enumerable.Range(0, 10));

    [Theory]
    [MemberData(nameof(ReferenceCases))]
    public void PositionsAndTithiMatchNativeSwissEphemeris(int index)
    {
        var fixture = LoadFixture();
        var reference = fixture.Cases[index];
        var result = _astronomy.GetPositions(reference.InstantUtc);
        var tithi = new TithiCalculator().Calculate(result.SunLongitude, result.MoonLongitude);

        Assert.InRange(Math.Abs(result.SunLongitude - reference.SunLongitude), 0, fixture.LongitudeToleranceDegrees);
        Assert.InRange(Math.Abs(result.MoonLongitude - reference.MoonLongitude), 0, fixture.LongitudeToleranceDegrees);
        Assert.InRange(Math.Abs(tithi.AngularDifference - reference.AngularDifference), 0, 2 * fixture.LongitudeToleranceDegrees);
        Assert.Equal(reference.TithiNumber, tithi.TithiNumber);
    }

    [Theory]
    [MemberData(nameof(ReferenceCases))]
    public void BothSunriseDefinitionsMatchNativeSwissEphemerisOnTheRequestedLocalDate(int index)
    {
        var fixture = LoadFixture();
        var reference = fixture.Cases[index];
        foreach (var definition in Enum.GetValues<SunriseDefinition>())
        {
            var result = _astronomy.CalculateSunrise(reference.LocalDate, reference.Location, definition);
            // Native swetest prints UT1. The tolerance includes UT1-UTC and 0.1 s print precision.
            var expectedUt1 = DateTimeOffset.Parse(reference.SunriseUt1[definition.ToString()],
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            Assert.Equal(SunriseStatus.Available, result.Status);
            Assert.NotNull(result.SunriseUtc);
            Assert.NotNull(result.SunriseLocal);
            Assert.InRange(Math.Abs((result.SunriseUtc.Value - expectedUt1).TotalSeconds), 0, fixture.SunriseToleranceSeconds);
            Assert.Equal(TimeSpan.Zero, result.SunriseUtc.Value.Offset);
            Assert.Equal(reference.LocalDate, DateOnly.FromDateTime(result.SunriseLocal.Value.DateTime));
            Assert.Equal(result.SunriseUtc, result.SunriseLocal.Value.ToUniversalTime());
        }
    }

    [Fact]
    public void SameInstantWithDifferentOffsetsHasTheSamePositions()
    {
        var utc = new DateTimeOffset(2026, 3, 29, 23, 30, 0, TimeSpan.Zero).AddTicks(1234567);
        Assert.Equal(_astronomy.GetPositions(utc), _astronomy.GetPositions(utc.ToOffset(TimeSpan.FromHours(5.5))));
    }

    [Theory]
    [InlineData(6, 21)]
    [InlineData(12, 21)]
    public void PolarDayAndNightHaveNoInventedSunrise(int month, int day)
    {
        var tromso = new Location("Tromsø", "NO", 69.6492, 18.9553, "Europe/Oslo");
        foreach (var definition in Enum.GetValues<SunriseDefinition>())
        {
            var result = _astronomy.CalculateSunrise(new DateOnly(2026, month, day), tromso, definition);
            Assert.Equal(SunriseStatus.NoSunriseOnLocalDate, result.Status);
            Assert.Null(result.SunriseUtc);
            Assert.Null(result.SunriseLocal);
        }
    }

    [Theory]
    [InlineData(3, 28, 1)]
    [InlineData(3, 29, 2)]
    [InlineData(10, 24, 2)]
    [InlineData(10, 25, 1)]
    public void SunriseLocalUsesTheOffsetAtSunriseAcrossDaylightSavingChanges(int month, int day, int offset)
    {
        var berlin = new Location("Berlin", "DE", 52.52, 13.405, "Europe/Berlin");
        var result = _astronomy.CalculateSunrise(new DateOnly(2026, month, day), berlin);
        Assert.Equal(TimeSpan.FromHours(offset), result.SunriseLocal!.Value.Offset);
    }

    [Fact]
    public void SkippedCivilDateDoesNotReceiveAnotherDaysSunrise()
    {
        var apia = new Location("Apia", "WS", -13.8333, -171.75, "Pacific/Apia");
        var result = _astronomy.CalculateSunrise(new DateOnly(2011, 12, 30), apia);
        Assert.Equal(SunriseStatus.SkippedLocalDate, result.Status);
        Assert.Null(result.SunriseUtc);
    }

    [Fact]
    public void InvalidDefinitionIsRejected()
    {
        var berlin = LoadFixture().Cases[0].Location;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _astronomy.CalculateSunrise(new DateOnly(2026, 1, 1), berlin, (SunriseDefinition)99));
    }

    [Fact]
    public void DateOutsideAnalyticEphemerisRangeIsAnErrorRatherThanNoSunrise()
    {
        Assert.Throws<AstronomyCalculationException>(() =>
            _astronomy.GetPositions(new DateTimeOffset(5000, 1, 1, 0, 0, 0, TimeSpan.Zero)));
        Assert.Throws<AstronomyCalculationException>(() =>
            _astronomy.CalculateSunrise(new DateOnly(5000, 1, 1), LoadFixture().Cases[0].Location));
    }

    [Fact]
    public async Task SharedAdapterSupportsConcurrentCalculations()
    {
        var instant = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);
        var expected = _astronomy.GetPositions(instant);
        var results = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => Task.Run(() => _astronomy.GetPositions(instant), TestContext.Current.CancellationToken)));
        Assert.All(results, result => Assert.Equal(expected, result));
    }

    private static ReferenceFixture LoadFixture() => JsonSerializer.Deserialize<ReferenceFixture>(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "swetest-2.10.03.json")),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    private sealed record ReferenceFixture(double LongitudeToleranceDegrees, double SunriseToleranceSeconds, ReferenceCase[] Cases);

    private sealed record ReferenceCase(Location Location, DateOnly LocalDate, DateTimeOffset InstantUtc,
        double SunLongitude, double MoonLongitude, double AngularDifference, int TithiNumber,
        Dictionary<string, string> SunriseUt1);
}
