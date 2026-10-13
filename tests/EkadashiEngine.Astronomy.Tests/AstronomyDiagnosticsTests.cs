using EkadashiEngine.Core;
using EkadashiEngine.Core.Astronomy;

namespace EkadashiEngine.Astronomy.Tests;

public sealed class AstronomyDiagnosticsTests
{
    [Fact]
    public void DiagnosticsUseLocationDateAndIndependentProviders()
    {
        var provider = new RecordingProvider();
        var service = new AstronomyDiagnosticsService(provider, provider, new TithiCalculator());
        var delhi = new Location("Delhi", "IN", 28.6139, 77.209, "Asia/Kolkata");
        var instant = new DateTimeOffset(2026, 1, 15, 22, 0, 0, TimeSpan.Zero);

        var result = service.Calculate(instant, delhi, SunriseDefinition.HinduGeometricCenter);

        Assert.Equal(instant, result.InstantUtc);
        Assert.Equal(new DateOnly(2026, 1, 16), result.Sunrise.LocalDate);
        Assert.Equal(SunriseDefinition.HinduGeometricCenter, result.Sunrise.Definition);
        Assert.Equal(120, result.AngularDifference);
        Assert.Equal(11, result.TithiNumber);
        Assert.Equal(instant, provider.LastInstant);
        Assert.Same(delhi, provider.LastLocation);
        Assert.Null(result.Sunrise.SunriseUtc);
    }

    [Theory]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(0, -181)]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.PositiveInfinity)]
    public void InvalidCoordinatesAreRejected(double latitude, double longitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Location("Invalid", "DE", latitude, longitude, "Europe/Berlin"));
    }

    [Fact]
    public void UnknownTimeZoneIsRejected()
    {
        Assert.Throws<TimeZoneNotFoundException>(() => new Location("Invalid", "DE", 0, 0, "Invalid/Zone"));
    }

    private sealed class RecordingProvider : IAstronomyService, ISunriseCalculator
    {
        public DateTimeOffset? LastInstant { get; private set; }
        public Location? LastLocation { get; private set; }

        public PlanetaryPositions GetPositions(DateTimeOffset instant)
        {
            LastInstant = instant;
            return new(350, 110);
        }

        public SunriseResult CalculateSunrise(DateOnly localDate, Location location, SunriseDefinition definition)
        {
            LastLocation = location;
            return new(localDate, definition, SunriseStatus.NoSunriseOnLocalDate, null, null);
        }
    }
}
