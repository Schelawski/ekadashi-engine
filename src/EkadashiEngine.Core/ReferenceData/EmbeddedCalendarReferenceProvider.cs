namespace EkadashiEngine.Core.ReferenceData;

/// <summary>Versioned references travel with the engine, including published builds.</summary>
public sealed class EmbeddedCalendarReferenceProvider : ICalendarReferenceProvider
{
    private readonly IReadOnlyList<CalendarReference> _references;

    public EmbeddedCalendarReferenceProvider()
    {
        var assembly = typeof(EmbeddedCalendarReferenceProvider).Assembly;
        _references = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith("CalendarReferences.", StringComparison.Ordinal))
            .Select(name =>
            {
                using var stream = assembly.GetManifestResourceStream(name)
                    ?? throw new InvalidDataException($"Missing embedded reference {name}.");
                return CalendarReferenceReader.Read(stream);
            }).ToArray();
    }

    public CalendarReference? Find(int year, Location location, Tradition tradition) =>
        _references.SingleOrDefault(reference => reference.Year == year && reference.Tradition == tradition
            && reference.ComparisonLocation.Latitude == location.Latitude
            && reference.ComparisonLocation.Longitude == location.Longitude
            && reference.ComparisonLocation.TimeZoneId == location.TimeZoneId);
}
