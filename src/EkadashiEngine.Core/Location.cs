using NodaTime;

namespace EkadashiEngine.Core;

public sealed record Location
{
    public string DisplayName { get; }
    public string Country { get; }
    public double Latitude { get; }
    public double Longitude { get; }
    public string TimeZoneId { get; }

    public Location(string displayName, string country, double latitude, double longitude, string timeZoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(country);
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZoneId);
        if (!double.IsFinite(latitude) || latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude));
        if (!double.IsFinite(longitude) || longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude));

        if (DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId) is null)
            throw new TimeZoneNotFoundException($"Unknown IANA time zone: {timeZoneId}");
        DisplayName = displayName;
        Country = country;
        Latitude = latitude;
        Longitude = longitude;
        TimeZoneId = timeZoneId;
    }

    public DateTimeOffset GetLocalTime(DateTimeOffset instant) =>
        Instant.FromDateTimeOffset(instant).InZone(DateTimeZoneProviders.Tzdb[TimeZoneId]).ToDateTimeOffset();

    public DateTimeOffset? GetStartOfDayUtc(DateOnly localDate)
    {
        try
        {
            // AtStartOfDay handles midnight gaps and selects the first occurrence in overlaps.
            return DateTimeZoneProviders.Tzdb[TimeZoneId]
                .AtStartOfDay(new LocalDate(localDate.Year, localDate.Month, localDate.Day))
                .ToInstant().ToDateTimeOffset();
        }
        catch (SkippedTimeException)
        {
            return null;
        }
    }
}
