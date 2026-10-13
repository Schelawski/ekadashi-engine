namespace EkadashiEngine.Core.ReferenceData;

public sealed record CalendarReference(string Source, string Title, DateOnly RetrievedOn,
    string Scope, int Year, Tradition Tradition, Location ComparisonLocation,
    IReadOnlyList<DateOnly> FastingDates);

public interface ICalendarReferenceProvider
{
    CalendarReference? Find(int year, Location location, Tradition tradition);
}
