using EkadashiEngine.Core.Astronomy;
using EkadashiEngine.Core.Rules;

namespace EkadashiEngine.Core.Comparison;

public sealed record LocationYearResult(YearAstronomyResult Astronomy, TraditionYearResult? Calendar);
public sealed record LocationDayResult(Location Location, DailyAstronomyResult Astronomy, bool? Selected);
public sealed record CrossLocationDay(DateOnly LocalDate, IReadOnlyList<LocationDayResult> Locations)
{
    public bool HasDifferences => Locations.Where(day => day.Selected.HasValue).Select(day => day.Selected).Distinct().Count() > 1;
    public bool HasUnknown => Locations.Any(day => !day.Selected.HasValue);
}
public sealed record CrossLocationResult(int Year, Tradition Tradition, IReadOnlyList<LocationYearResult> Locations,
    IReadOnlyList<CrossLocationDay> Days);
public sealed record CrossLocationProgress(int Completed, int Total, string LocationName);

public interface ICrossLocationCalculator
{
    CrossLocationResult Calculate(int year, IReadOnlyList<Location> locations, Tradition tradition,
        CancellationToken cancellationToken = default, IProgress<CrossLocationProgress>? progress = null);
}

public sealed class CrossLocationCalculator(IYearAstronomyCalculator astronomy, IFastingCalendarCalculator fasting)
    : ICrossLocationCalculator
{
    public CrossLocationResult Calculate(int year, IReadOnlyList<Location> locations, Tradition tradition,
        CancellationToken cancellationToken = default, IProgress<CrossLocationProgress>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(locations);
        if (year is < YearAstronomyCalculator.MinimumYear or > YearAstronomyCalculator.MaximumYear)
            throw new ArgumentOutOfRangeException(nameof(year));
        if (tradition is not (Tradition.TechnicalSunriseCandidate or Tradition.AnandaMarga))
            throw new ArgumentOutOfRangeException(nameof(tradition));
        if (locations.Count < 2 || locations.Select(LocationKey.From).Distinct().Count() != locations.Count)
            throw new ArgumentException("Select at least two distinct coordinate/time-zone locations.", nameof(locations));
        var results = new List<LocationYearResult>();
        var dayCount = DateTime.IsLeapYear(year) ? 366 : 365;
        foreach (var location in locations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sky = astronomy.Calculate(year, location, cancellationToken);
            var calendar = tradition == Tradition.AnandaMarga ? fasting.Calculate(year, location, cancellationToken) : null;
            if (sky.Year != year || sky.Location != location || sky.Days.Count != dayCount
                || !sky.Days.Select(day => day.LocalDate).SequenceEqual(Enumerable.Range(0, dayCount).Select(index => new DateOnly(year, 1, 1).AddDays(index))))
                throw new AstronomyCalculationException("Cross-location comparison requires a complete, ordered local calendar year.");
            if (calendar is not null && (calendar.Year != year || calendar.Location != location || calendar.Tradition != tradition))
                throw new AstronomyCalculationException("The traditional calendar does not match the requested location, year or tradition.");
            results.Add(new(sky, calendar));
            progress?.Report(new(results.Count, locations.Count, location.DisplayName));
        }
        var days = new List<CrossLocationDay>(dayCount);
        for (var i = 0; i < dayCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            days.Add(new(new DateOnly(year, 1, 1).AddDays(i), results.Select(result =>
            {
                var day = result.Astronomy.Days[i];
                return new LocationDayResult(result.Astronomy.Location, day,
                    tradition == Tradition.TechnicalSunriseCandidate ? day.IsEkadashiCandidate : result.Calendar!.IsFasting(day));
            }).ToArray()));
        }
        cancellationToken.ThrowIfCancellationRequested();
        return new(year, tradition, results.AsReadOnly(), days.AsReadOnly());
    }
}
