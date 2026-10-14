using EkadashiEngine.Core.Astronomy;

namespace EkadashiEngine.Core.Rules;

public enum RuleDecisionStatus { Selected, MissingSunriseCoverage, AmbiguousOverlap }

public sealed record DayOverlap(DateOnly LocalDate, DateTimeOffset SunriseUtc,
    DateTimeOffset NextSunriseUtc, TimeSpan Overlap);

public sealed record RuleDecision(TithiInterval Tithi, DateOnly? FastingDate,
    RuleDecisionStatus Status, IReadOnlyList<DayOverlap> Candidates, string Explanation)
{
    public bool TouchesDate(DateOnly date, Location location) =>
        Candidates.Any(candidate => candidate.LocalDate == date) ||
        (DateOnly.FromDateTime(location.GetLocalTime(Tithi.StartUtc).DateTime) <= date &&
         DateOnly.FromDateTime(location.GetLocalTime(Tithi.EndUtc.AddTicks(-1)).DateTime) >= date);
}

public sealed record TraditionYearResult(Location Location, int Year, Tradition Tradition,
    string RuleVersion, string RuleSource, bool IsCalendarValidated,
    IReadOnlyList<DateOnly> FastingDates, IReadOnlyList<RuleDecision> Decisions)
{
    public bool HasUnresolvedDecisions => Decisions.Any(d => d.Status != RuleDecisionStatus.Selected);

    public bool? IsFasting(DailyAstronomyResult day)
    {
        if (day.LocalDate.Year != Year) return null;
        if (FastingDates.Contains(day.LocalDate)) return true;
        if (day.Sunrise.Status != SunriseStatus.Available || Decisions.Any(decision =>
            decision.Status != RuleDecisionStatus.Selected && decision.TouchesDate(day.LocalDate, Location))) return null;
        return false;
    }
}
