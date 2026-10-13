namespace EkadashiEngine.Core;

public sealed record Location(
    string DisplayName,
    string Country,
    double Latitude,
    double Longitude,
    string TimeZoneId);
