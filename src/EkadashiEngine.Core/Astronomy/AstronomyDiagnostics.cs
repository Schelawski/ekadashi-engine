namespace EkadashiEngine.Core.Astronomy;

public sealed record AstronomyDiagnostics(
    DateTimeOffset InstantUtc,
    Location Location,
    double SunLongitude,
    double MoonLongitude,
    double AngularDifference,
    int TithiNumber,
    SunriseResult Sunrise);
