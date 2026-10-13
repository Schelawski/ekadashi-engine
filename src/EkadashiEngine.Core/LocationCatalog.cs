namespace EkadashiEngine.Core;

public static class LocationCatalog
{
    public static IReadOnlyList<Location> DefaultLocations { get; } = Array.AsReadOnly<Location>(
    [
        new("Berlin", "DE", 52.5200, 13.4050, "Europe/Berlin"),
        new("Moscow", "RU", 55.7558, 37.6173, "Europe/Moscow"),
        new("Delhi", "IN", 28.6139, 77.2090, "Asia/Kolkata"),
        new("London", "GB", 51.5074, -0.1278, "Europe/London"),
        new("New York", "US", 40.7128, -74.0060, "America/New_York"),
        new("Los Angeles", "US", 34.0522, -118.2437, "America/Los_Angeles"),
        new("Tokyo", "JP", 35.6762, 139.6503, "Asia/Tokyo"),
        new("Sydney", "AU", -33.8688, 151.2093, "Australia/Sydney"),
        new("São Paulo", "BR", -23.5505, -46.6333, "America/Sao_Paulo")
    ]);
}
