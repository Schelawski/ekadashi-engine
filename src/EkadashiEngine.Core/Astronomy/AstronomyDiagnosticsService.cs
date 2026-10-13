namespace EkadashiEngine.Core.Astronomy;

public sealed class AstronomyDiagnosticsService(
    IAstronomyService astronomy,
    ISunriseCalculator sunrise,
    ITithiCalculator tithi)
{
    public AstronomyDiagnostics Calculate(DateTimeOffset instant, Location location,
        SunriseDefinition definition = SunriseDefinition.UpperLimbWithRefraction)
    {
        ArgumentNullException.ThrowIfNull(location);
        var utc = instant.ToUniversalTime();
        var localDate = DateOnly.FromDateTime(location.GetLocalTime(utc).DateTime);
        var positions = astronomy.GetPositions(utc);
        var lunarDay = tithi.Calculate(positions.SunLongitude, positions.MoonLongitude);
        return new AstronomyDiagnostics(utc, location, positions.SunLongitude, positions.MoonLongitude,
            lunarDay.AngularDifference, lunarDay.TithiNumber,
            sunrise.CalculateSunrise(localDate, location, definition));
    }
}
