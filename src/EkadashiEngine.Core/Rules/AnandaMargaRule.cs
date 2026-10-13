using EkadashiEngine.Core.Astronomy;

namespace EkadashiEngine.Core.Rules;

/// <summary>Implements the published New York Sector maximum-overlap method.</summary>
public sealed class AnandaMargaRule : ITraditionRule
{
    public const string Version = "am-max-overlap-v1";
    public const string Source = "https://ampsnys.org/fasting-calendar/";
    // Equal durations within boundary uncertainty have no documented traditional tie-break.
    public static readonly TimeSpan TieTolerance = TimeSpan.FromSeconds(1);
    public Tradition Tradition => Tradition.AnandaMarga;

    public TraditionYearResult Evaluate(PreparedTraditionYear data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        var days = data.SolarDays.OrderBy(day => day.LocalDate).ToArray();
        if (days.Select(d => d.LocalDate).Distinct().Count() != days.Length)
            throw new ArgumentException("Solar dates must be unique.", nameof(data));
        var validDays = days.Where(d => d.SunriseUtc.HasValue && d.NextSunriseUtc.HasValue).ToArray();
        for (var i = 0; i < validDays.Length; i++)
        {
            var day = validDays[i];
            if (day.NextSunriseUtc <= day.SunriseUtc ||
                (i > 0 && validDays[i - 1].NextSunriseUtc > day.SunriseUtc))
                throw new ArgumentException("Solar days must have positive, non-overlapping UTC intervals.", nameof(data));
        }

        var decisions = new List<RuleDecision>();
        foreach (var interval in data.Tithis.OrderBy(t => t.StartUtc))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (interval.TithiNumber is < 1 or > 30 || interval.EndUtc <= interval.StartUtc)
                throw new ArgumentException("Invalid lunar day.", nameof(data));
            if (interval.TithiNumber is not (11 or 26)) continue;
            var candidates = validDays.Select(day => new DayOverlap(day.LocalDate,
                    day.SunriseUtc!.Value, day.NextSunriseUtc!.Value,
                    Overlap(interval, day.SunriseUtc.Value, day.NextSunriseUtc.Value)))
                .Where(c => c.Overlap > TimeSpan.Zero).OrderByDescending(c => c.Overlap)
                .ThenBy(c => c.LocalDate).ToArray();
            var covered = TimeSpan.FromTicks(candidates.Sum(c => c.Overlap.Ticks));
            RuleDecision decision;
            if (covered != interval.EndUtc - interval.StartUtc)
                decision = new(interval, null, RuleDecisionStatus.MissingSunriseCoverage,
                    Array.AsReadOnly(candidates), "Incomplete sunrise-to-sunrise coverage; no fasting date selected.");
            else if (candidates.Length > 1 && candidates[0].Overlap - candidates[1].Overlap <= TieTolerance)
                decision = new(interval, null, RuleDecisionStatus.AmbiguousOverlap,
                    Array.AsReadOnly(candidates), "Maximum overlaps differ by at most one second; no documented tie-break is available.");
            else
            {
                var chosen = candidates[0];
                decision = new(interval, chosen.LocalDate, RuleDecisionStatus.Selected,
                    Array.AsReadOnly(candidates), FormattableString.Invariant(
                        $"Selected {chosen.LocalDate:yyyy-MM-dd}: greatest sunrise-to-sunrise overlap ({chosen.Overlap.TotalSeconds:F1} seconds) with tithi {interval.TithiNumber}."));
            }
            // Never move a winner into this year merely because its real date is outside it.
            if (decision.FastingDate is { } date ? date.Year == data.Year : TouchesYear(interval, data))
                decisions.Add(decision);
        }
        cancellationToken.ThrowIfCancellationRequested();
        var dates = decisions.Where(d => d.FastingDate.HasValue).Select(d => d.FastingDate!.Value)
            .Distinct().Order().ToArray();
        return new(data.Location, data.Year, Tradition, Version, Source, false,
            Array.AsReadOnly(dates), decisions.AsReadOnly());
    }

    private static TimeSpan Overlap(TithiInterval tithi, DateTimeOffset start, DateTimeOffset end)
    {
        var from = tithi.StartUtc > start ? tithi.StartUtc : start;
        var to = tithi.EndUtc < end ? tithi.EndUtc : end;
        return to > from ? to - from : TimeSpan.Zero;
    }

    private static bool TouchesYear(TithiInterval interval, PreparedTraditionYear data) =>
        data.Location.GetLocalTime(interval.StartUtc).Year <= data.Year &&
        data.Location.GetLocalTime(interval.EndUtc).Year >= data.Year;
}
