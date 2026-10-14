namespace EkadashiEngine.Core;

/// <summary>Calculation identity excludes display metadata but always includes coordinates.</summary>
public readonly record struct LocationKey(double Latitude, double Longitude, string TimeZoneId)
{
    public static LocationKey From(Location location) => new(location.Latitude, location.Longitude, location.TimeZoneId);
}
