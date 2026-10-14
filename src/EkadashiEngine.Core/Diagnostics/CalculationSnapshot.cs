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
    public bool? IsFasting(DailyAstronomyResult day) => Calendar?.IsFasting(day);

    public static bool TouchesDate(RuleDecision decision, DateOnly date, Location location) =>
        decision.TouchesDate(date, location);
}
