using EkadashiEngine.Core.Astronomy;
using EkadashiEngine.Core.ReferenceData;
using EkadashiEngine.Core.Rules;

namespace EkadashiEngine.Core.Diagnostics;

public sealed record AstronomyProvenance(string Provider, string Version, string Ephemeris);

public sealed record ComparisonDiagnostic(CalendarComparisonRow Comparison,
    IReadOnlyList<DailyAstronomyResult> Days, IReadOnlyList<RuleDecision> Decisions);

/// <summary>Complete, unfiltered calculation; exports and UI share this snapshot.</summary>
public sealed record CalculationSnapshot(YearAstronomyResult Astronomy, TraditionYearResult? Calendar,
    CalendarComparisonResult? Comparison, IReadOnlyList<ComparisonDiagnostic> Diagnostics,
    AstronomyProvenance Provenance)
{
    public bool? IsFasting(DailyAstronomyResult day)
    {
        if (Calendar is null || day.LocalDate.Year != Calendar.Year) return null;
        if (Calendar.FastingDates.Contains(day.LocalDate)) return true;
        if (day.Sunrise.Status != SunriseStatus.Available || Calendar.Decisions.Any(decision =>
            decision.Status != RuleDecisionStatus.Selected && TouchesDate(decision, day.LocalDate, Astronomy.Location))) return null;
        return false;
    }

    public static bool TouchesDate(RuleDecision decision, DateOnly date, Location location) =>
        decision.Candidates.Any(candidate => candidate.LocalDate == date) ||
        (DateOnly.FromDateTime(location.GetLocalTime(decision.Tithi.StartUtc).DateTime) <= date &&
         DateOnly.FromDateTime(location.GetLocalTime(decision.Tithi.EndUtc.AddTicks(-1)).DateTime) >= date);
}
