using System.Globalization;
using Avalonia.Media;
using EkadashiEngine.Core;
using EkadashiEngine.Core.ReferenceData;
using EkadashiEngine.Core.Rules;
using EkadashiEngine.Core.Diagnostics;

namespace EkadashiEngine.Desktop.ViewModels;

public sealed class ComparisonRow(ComparisonDiagnostic diagnostic, TraditionYearResult calendar)
{
    private CalendarComparisonRow row => diagnostic.Comparison;
    private Location location => calendar.Location;
    public ComparisonDiagnostic Diagnostic => diagnostic;
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
            var lines = new List<string>
            {
                FormattableString.Invariant($"Ort: {location.DisplayName}, {location.Country} · {location.Latitude:F5}°, {location.Longitude:F5}° · {location.TimeZoneId}"),
                $"Regel: {calendar.Tradition} / {calendar.RuleVersion} · {calendar.RuleSource}"
            };
            foreach (var day in diagnostic.Days)
            {
                lines.Add($"Lokales Datum: {Format(day.LocalDate)} · Sonnenaufgang: {day.Sunrise.Status}");
                lines.Add($"Sonnenaufgang lokal: {Instant(day.Sunrise.SunriseLocal)} · UTC: {Instant(day.Sunrise.SunriseUtc)}");
                lines.Add($"Tithi bei Sonnenaufgang: {day.Tithi?.TithiNumber.ToString(CultureInfo.InvariantCulture) ?? "—"} · "
                    + $"Sonnenlänge: {Angle(day.Positions?.SunLongitude)} · Mondlänge: {Angle(day.Positions?.MoonLongitude)} · Winkeldifferenz: {Angle(day.Tithi?.AngularDifference)}");
            }
            foreach (var decision in diagnostic.Decisions)
            {
                lines.Add($"Tithi {decision.Tithi.TithiNumber} / {decision.Status} · gewählter Fastentag: {Format(decision.FastingDate)}");
                lines.Add($"Ekadashi-Start lokal: {Instant(location.GetLocalTime(decision.Tithi.StartUtc))} · UTC: {Instant(decision.Tithi.StartUtc)}");
                lines.Add($"Ekadashi-Ende lokal: {Instant(location.GetLocalTime(decision.Tithi.EndUtc))} · UTC: {Instant(decision.Tithi.EndUtc)}");
                lines.Add("Überschneidung mit Sonnenaufgangstagen: " + string.Join(" · ", decision.Candidates.Select(candidate =>
                    $"{Format(candidate.LocalDate)}: {candidate.Overlap.TotalHours.ToString("F2", CultureInfo.CurrentCulture)} h")));
                lines.Add(decision.Explanation);
            }
            if (diagnostic.Decisions.Count == 0) lines.Add("Keine zugehörige Ekadashi-Regelentscheidung vorhanden.");
            return string.Join(Environment.NewLine, lines);
        }
    }

    private static string Format(DateOnly? date) => date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—";
    private static string Instant(DateTimeOffset? instant) => instant?.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture) ?? "—";
    private static string Angle(double? angle) => angle?.ToString("F6", CultureInfo.InvariantCulture) + (angle.HasValue ? "°" : "—");
}
