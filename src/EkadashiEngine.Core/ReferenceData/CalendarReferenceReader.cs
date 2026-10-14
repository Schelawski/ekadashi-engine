using System.Text.Json;

namespace EkadashiEngine.Core.ReferenceData;

public static class CalendarReferenceReader
{
    public static CalendarReference Read(Stream stream)
    {
        var document = JsonSerializer.Deserialize<ReferenceDocument>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidDataException("Empty calendar reference.");
        if (string.IsNullOrWhiteSpace(document.Title) || string.IsNullOrWhiteSpace(document.Scope)
            || !Uri.TryCreate(document.Source, UriKind.Absolute, out var source) || source.Scheme != "https"
            || document.Year is < 1900 or > 2100 || document.RetrievedOn == default
            || document.Tradition is not ("AnandaMarga" or "GaudiyaVaishnava") || document.ComparisonLocation is null
            || document.FastingDates is not { Length: > 0 })
            throw new InvalidDataException("Incomplete or unsupported calendar reference metadata.");
        if (document.FastingDates.Any(date => date.Year != document.Year)
            || document.FastingDates.Distinct().Count() != document.FastingDates.Length)
            throw new InvalidDataException("Reference dates must be unique and belong to the reference year.");
        var location = document.ComparisonLocation;
        return new(document.Source!, document.Title, document.RetrievedOn, document.Scope, document.Year,
            Enum.Parse<Tradition>(document.Tradition!),
            new Location(location.DisplayName, location.Country, location.Latitude, location.Longitude, location.TimeZoneId),
            Array.AsReadOnly(document.FastingDates.Order().ToArray()));
    }

    private sealed class ReferenceDocument
    {
        public string? Source { get; set; }
        public string? Title { get; set; }
        public DateOnly RetrievedOn { get; set; }
        public string? Scope { get; set; }
        public int Year { get; set; }
        public string? Tradition { get; set; }
        public LocationDocument? ComparisonLocation { get; set; }
        public DateOnly[]? FastingDates { get; set; }
    }

    private sealed class LocationDocument
    {
        public required string DisplayName { get; set; }
        public required string Country { get; set; }
        public required double Latitude { get; set; }
        public required double Longitude { get; set; }
        public required string TimeZoneId { get; set; }
    }
}
