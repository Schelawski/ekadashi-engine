using EkadashiEngine.Core.Astronomy;

namespace EkadashiEngine.Core.Rules;

public enum RuleDecisionStatus { Selected, MissingSunriseCoverage, AmbiguousOverlap }

public sealed record DayOverlap(DateOnly LocalDate, DateTimeOffset SunriseUtc,
    DateTimeOffset NextSunriseUtc, TimeSpan Overlap);

public sealed record RuleDecision(TithiInterval Tithi, DateOnly? FastingDate,
    RuleDecisionStatus Status, IReadOnlyList<DayOverlap> Candidates, string Explanation);

public sealed record TraditionYearResult(Location Location, int Year, Tradition Tradition,
    string RuleVersion, string RuleSource, bool IsCalendarValidated,
    IReadOnlyList<DateOnly> FastingDates, IReadOnlyList<RuleDecision> Decisions)
{
    public bool HasUnresolvedDecisions => Decisions.Any(d => d.Status != RuleDecisionStatus.Selected);
}
