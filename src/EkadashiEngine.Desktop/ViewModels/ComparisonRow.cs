using System.Globalization;
using Avalonia.Media;
using EkadashiEngine.Core;
using EkadashiEngine.Core.ReferenceData;
using EkadashiEngine.Core.Rules;

namespace EkadashiEngine.Desktop.ViewModels;

public sealed class ComparisonRow(CalendarComparisonRow row, Location location, RuleDecision? decision)
{
    public CalendarComparisonRow Data => row;
    public string Calculated => Format(row.Calculated);
    public string Official => Format(row.Official);
    public bool IsMismatch => row.Status is CalendarComparisonStatus.Mismatch or CalendarComparisonStatus.MissingCalculated or CalendarComparisonStatus.UnexpectedCalculated;
    public string Status => row.Status switch
    {
        CalendarComparisonStatus.Match => "MATCH",
        CalendarComparisonStatus.Mismatch => "MISMATCH",
        CalendarComparisonStatus.MissingCalculated => "FEHLT BERECHNET",
        CalendarComparisonStatus.UnexpectedCalculated => "ZUSÄTZLICH BERECHNET",
        _ => "KEINE REFERENZ"
    };
    public IBrush StatusBackground => row.Status == CalendarComparisonStatus.Match
        ? Brush.Parse("#E1F2E9") : IsMismatch ? Brush.Parse("#FCE7E3") : Brush.Parse("#EDF0F4");
    public IBrush StatusForeground => row.Status == CalendarComparisonStatus.Match
        ? Brush.Parse("#176143") : IsMismatch ? Brush.Parse("#A52B26") : Brush.Parse("#556579");
    public string Difference => row.Calculated is { } calculated && row.Official is { } official
        ? (calculated.DayNumber - official.DayNumber) switch
        {
            0 => "Gleiches Datum",
            var difference => $"{difference:+0;-0} Tag(e)"
        }
        : row.Status == CalendarComparisonStatus.MissingCalculated ? "Kein zugehöriges Rechendatum"
        : row.Status == CalendarComparisonStatus.UnexpectedCalculated ? "Kein zugehöriges Referenzdatum"
        : "Offizieller Vergleich nicht verfügbar";
    public string DetailTitle => $"{location.DisplayName} · berechnet {Calculated} · offiziell {Official} · {Status}";
    public string Diagnosis
    {
        get
        {
            if (decision is null) return "Für diese Referenzzeile liegt keine gewählte Regelentscheidung vor.";
            var start = location.GetLocalTime(decision.Tithi.StartUtc);
            var end = location.GetLocalTime(decision.Tithi.EndUtc);
            var candidates = string.Join(" · ", decision.Candidates.Select(candidate =>
                $"{Format(candidate.LocalDate)}: {candidate.Overlap.TotalHours.ToString("F2", CultureInfo.CurrentCulture)} h"));
            return $"Tithi {decision.Tithi.TithiNumber}: {start:yyyy-MM-dd HH:mm:ss zzz} bis {end:yyyy-MM-dd HH:mm:ss zzz} (lokal). "
                + $"Überschneidung mit Sonnenaufgangstagen: {candidates}. Gewählt wurde die größte Überschneidung.";
        }
    }

    private static string Format(DateOnly? date) => date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—";
}
