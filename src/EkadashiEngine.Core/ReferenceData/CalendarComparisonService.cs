using EkadashiEngine.Core.Rules;

namespace EkadashiEngine.Core.ReferenceData;

public enum CalendarComparisonStatus { Match, Mismatch, MissingCalculated, UnexpectedCalculated, NoReference }

public sealed record CalendarComparisonRow(DateOnly? Calculated, DateOnly? Official, CalendarComparisonStatus Status);

public sealed record CalendarComparisonResult(CalendarReference? Reference, IReadOnlyList<CalendarComparisonRow> Rows,
    int? ReferenceCount, int? Matches, int? Mismatches, int? UnexpectedCalculated, double? MatchRate);

public sealed class CalendarComparisonService
{
    // Pair nearby dates for presentation only. Exact matches are reserved first.
    public const int MaximumPairingDistanceDays = 2;

    public CalendarComparisonResult Compare(TraditionYearResult calculated, CalendarReference? reference)
    {
        ArgumentNullException.ThrowIfNull(calculated);
        var dates = calculated.FastingDates.Order().ToArray();
        if (dates.Any(date => date.Year != calculated.Year) || dates.Distinct().Count() != dates.Length)
            throw new ArgumentException("Calculated dates must be unique and belong to the requested year.", nameof(calculated));
        if (reference is null)
            return new(null, Array.AsReadOnly(dates.Select(date => new CalendarComparisonRow(date, null, CalendarComparisonStatus.NoReference)).ToArray()),
                null, null, null, null, null);
        var location = reference.ComparisonLocation;
        if (reference.Year != calculated.Year || reference.Tradition != calculated.Tradition
            || location.Latitude != calculated.Location.Latitude || location.Longitude != calculated.Location.Longitude
            || location.TimeZoneId != calculated.Location.TimeZoneId)
            throw new ArgumentException("The reference is for a different year, tradition or comparison location.", nameof(reference));
        var official = reference.FastingDates.Order().ToArray();
        if (official.Length == 0 || official.Any(date => date.Year != reference.Year) || official.Distinct().Count() != official.Length)
            throw new ArgumentException("Reference dates must be nonempty, unique and within the requested year.", nameof(reference));
        var remaining = dates.ToHashSet();
        var exact = dates.Intersect(official).ToHashSet();
        remaining.ExceptWith(exact);
        var rows = new List<CalendarComparisonRow>();
        foreach (var date in official)
        {
            if (exact.Contains(date))
            {
                rows.Add(new(date, date, CalendarComparisonStatus.Match));
                continue;
            }
            var nearby = remaining.Where(candidate => Math.Abs(candidate.DayNumber - date.DayNumber) <= MaximumPairingDistanceDays)
                .OrderBy(candidate => Math.Abs(candidate.DayNumber - date.DayNumber)).ThenBy(candidate => candidate)
                .Select(candidate => (DateOnly?)candidate).FirstOrDefault();
            if (nearby is { } paired)
            {
                remaining.Remove(paired);
                rows.Add(new(paired, date, CalendarComparisonStatus.Mismatch));
            }
            else rows.Add(new(null, date, CalendarComparisonStatus.MissingCalculated));
        }
        rows.AddRange(remaining.Select(date => new CalendarComparisonRow(date, null, CalendarComparisonStatus.UnexpectedCalculated)));
        return new(reference, Array.AsReadOnly(rows.OrderBy(row => row.Official ?? row.Calculated).ToArray()),
            official.Length, exact.Count, official.Length - exact.Count, remaining.Count, 100d * exact.Count / official.Length);
    }
}
