using EkadashiEngine.Core.Astronomy;
using EkadashiEngine.Core.ReferenceData;
using EkadashiEngine.Core.Rules;

namespace EkadashiEngine.Core.Diagnostics;

public sealed class CalculationSnapshotFactory(IDayAstronomyCalculator dayCalculator, AstronomyProvenance provenance)
{
    public CalculationSnapshot Create(YearAstronomyResult astronomy, TraditionYearResult? calendar = null,
        CalendarComparisonResult? comparison = null, CancellationToken cancellationToken = default)
    {
        if (calendar is not null && (calendar.Year != astronomy.Year || calendar.Location != astronomy.Location))
            throw new ArgumentException("Astronomy and calendar must describe the same location and year.", nameof(calendar));
        if ((calendar is null) != (comparison is null))
            throw new ArgumentException("A calendar and its comparison must be supplied together.", nameof(comparison));
        var days = astronomy.Days.ToDictionary(day => day.LocalDate);
        var diagnostics = new List<ComparisonDiagnostic>();
        foreach (var row in comparison?.Rows ?? [])
        {
            cancellationToken.ThrowIfCancellationRequested();
            var decisions = calendar!.Decisions.Where(decision =>
                (row.Calculated.HasValue && decision.FastingDate == row.Calculated)
                || (row.Official.HasValue && CalculationSnapshot.TouchesDate(decision, row.Official.Value, astronomy.Location)))
                .OrderBy(decision => decision.Tithi.StartUtc).ToArray();
            var dates = decisions.SelectMany(decision => decision.Candidates.Select(candidate => candidate.LocalDate))
                .Concat(decisions.SelectMany(decision => new[]
                {
                    DateOnly.FromDateTime(astronomy.Location.GetLocalTime(decision.Tithi.StartUtc).DateTime),
                    DateOnly.FromDateTime(astronomy.Location.GetLocalTime(decision.Tithi.EndUtc.AddTicks(-1)).DateTime)
                }))
                .Concat(new[] { row.Calculated, row.Official }.OfType<DateOnly>()).Distinct().Order().ToArray();
            var diagnosticDays = new List<DailyAstronomyResult>();
            foreach (var date in dates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!days.TryGetValue(date, out var day))
                {
                    day = dayCalculator.Calculate(date, astronomy.Location);
                    days.Add(date, day);
                }
                diagnosticDays.Add(day);
            }
            diagnostics.Add(new(row, diagnosticDays.AsReadOnly(), Array.AsReadOnly(decisions)));
        }
        cancellationToken.ThrowIfCancellationRequested();
        return new(astronomy, calendar, comparison, diagnostics.AsReadOnly(), provenance);
    }
}
