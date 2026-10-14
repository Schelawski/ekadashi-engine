using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EkadashiEngine.Core.Diagnostics;
using EkadashiEngine.Core.Astronomy;
using EkadashiEngine.Core.Rules;
using NodaTime;

namespace EkadashiEngine.Core.Export;

public sealed class CalculationExporter
{
    public const int SchemaVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public ExportPayload Create(CalculationSnapshot snapshot, ExportFormat format)
    {
        var document = CreateAnalysis(snapshot);
        var content = format switch
        {
            ExportFormat.Json => Json(document),
            ExportFormat.TestVectors => Json(CreateTestVectors(snapshot)),
            ExportFormat.Csv when snapshot.Comparison is not null => Encoding.UTF8.GetBytes("\uFEFF" + CreateCsv(document)),
            ExportFormat.Csv => throw new InvalidOperationException("CSV comparison export requires a traditional calendar calculation."),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
        var extension = format == ExportFormat.Csv ? "csv" : "json";
        var suffix = format == ExportFormat.TestVectors ? "test-vectors-v1" : format == ExportFormat.Csv ? "comparison" : "analysis-v1";
        var slug = string.Concat(snapshot.Astronomy.Location.DisplayName.Select(character => char.IsLetterOrDigit(character) ? character : '-'));
        var tradition = snapshot.Calendar?.Tradition.ToString() ?? "Technical";
        return new($"{slug}-{snapshot.Astronomy.Year}-{tradition}-{suffix}.{extension}", extension, content);
    }

    public AnalysisDocument CreateAnalysis(CalculationSnapshot snapshot)
    {
        var location = snapshot.Astronomy.Location;
        var referenceDates = snapshot.Comparison?.Reference?.FastingDates.ToHashSet();
        ExportDay Day(DailyAstronomyResult day) => new(day.LocalDate, day.Sunrise.Status, day.Sunrise.Definition,
            day.Sunrise.SunriseUtc, day.Sunrise.SunriseLocal, day.Positions?.SunLongitude, day.Positions?.MoonLongitude,
            day.Tithi?.AngularDifference, day.Tithi?.TithiNumber, day.IsEkadashiCandidate, snapshot.IsFasting(day),
            day.LocalDate.Year == snapshot.Astronomy.Year ? referenceDates?.Contains(day.LocalDate) : null);
        ExportDecision Decision(RuleDecision decision) => new(decision.Tithi.TithiNumber, decision.Tithi.StartUtc,
            location.GetLocalTime(decision.Tithi.StartUtc), decision.Tithi.EndUtc, location.GetLocalTime(decision.Tithi.EndUtc),
            decision.FastingDate, decision.Status, decision.Explanation,
            decision.Candidates.OrderBy(candidate => candidate.LocalDate).Select(candidate => new ExportSolarDay(
                candidate.LocalDate, candidate.SunriseUtc, candidate.NextSunriseUtc, candidate.Overlap.TotalSeconds)).ToArray(), decision.VaishnavaDays);
        var comparison = snapshot.Comparison;
        var rule = snapshot.Calendar is { } calendar
            ? new ExportRule(calendar.Tradition, calendar.RuleVersion, calendar.RuleSource, calendar.IsCalendarValidated)
            : new ExportRule(Tradition.TechnicalSunriseCandidate, "technical-sunrise-v1", null, false);
        return new(SchemaVersion, "analysis", new(location.DisplayName, location.Country, location.Latitude, location.Longitude, location.TimeZoneId),
            snapshot.Astronomy.Year, snapshot.Provenance,
            new(DateTimeZoneProviders.Tzdb.VersionId, "apparent geocentric tropical ecliptic of date", "degrees [0,360)", "half-open [start,end); elapsed durations in UTC"),
            rule, comparison?.Reference, comparison is null ? null : new(comparison.ReferenceCount, comparison.Matches, comparison.Mismatches, comparison.UnexpectedCalculated, comparison.MatchRate),
            snapshot.Astronomy.Days.OrderBy(day => day.LocalDate).Select(Day).ToArray(),
            snapshot.Calendar?.Decisions.OrderBy(decision => decision.Tithi.StartUtc).Select(Decision).ToArray() ?? [],
            snapshot.Diagnostics.Select(diagnostic => new ExportComparison(diagnostic.Comparison.Calculated, diagnostic.Comparison.Official,
                diagnostic.Comparison.Status, diagnostic.Days.OrderBy(day => day.LocalDate).Select(Day).ToArray(),
                diagnostic.Decisions.Select(Decision).ToArray())).ToArray());
    }

    public TestVectorDocument CreateTestVectors(CalculationSnapshot snapshot)
    {
        var analysis = CreateAnalysis(snapshot);
        return new(SchemaVersion, "testVectors", analysis.Location, analysis.Year, analysis.Astronomy, analysis.Conventions,
            analysis.Rule, analysis.Reference, new(0.00001, 2, 1), analysis.Days,
            analysis.Decisions.Select(decision => new RuleTestCase(decision.Tithi, decision.StartUtc, decision.EndUtc,
                decision.SolarDays, decision.FastingDate, decision.Status, decision.VaishnavaDays)).ToArray());
    }

    public static byte[] Json<T>(T document) => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(document, JsonOptions) + "\n");

    private static string CreateCsv(AnalysisDocument document)
    {
        var text = new StringBuilder();
        Write("location", "country", "latitude", "longitude", "timeZone", "localDate", "sunriseStatus", "sunriseLocal", "sunriseUtc", "tithiAtSunrise",
            "sunLongitude", "moonLongitude", "angularDifference", "ekadashiStartLocal", "ekadashiStartUtc", "ekadashiEndLocal", "ekadashiEndUtc",
            "calculatedFastingDate", "officialFastingDate", "comparisonStatus", "tradition", "ruleVersion", "ruleSource", "ruleStatus", "explanation", "referenceSource",
            "schemaVersion", "astronomyProvider", "astronomyVersion", "ephemeris", "referenceRetrievedOn", "isCalendarValidated",
            "arunodayaUtc", "arunodayaLocal", "tithiAtArunodaya", "sunsetUtc", "sunsetLocal", "tithiAtSunset", "nakshatraLahiriZeroBased");
        var location = new Location(document.Location.DisplayName, document.Location.Country,
            document.Location.Latitude, document.Location.Longitude, document.Location.TimeZone);
        foreach (var comparison in document.Comparisons)
        {
            // A row per diagnostic day and related lunar interval keeps neighboring cases inspectable.
            foreach (var day in comparison.Days)
            foreach (var decision in comparison.Decisions.Cast<ExportDecision?>().DefaultIfEmpty(null))
            {
                var observation = decision?.VaishnavaDays?.FirstOrDefault(item => item.LocalDate == day.Date);
                Write(document.Location.DisplayName, document.Location.Country, document.Location.Latitude, document.Location.Longitude,
                    document.Location.TimeZone, day.Date, day.SunriseStatus, day.SunriseLocal, day.SunriseUtc, day.Tithi,
                    day.SunLongitude, day.MoonLongitude, day.AngularDifference, decision?.StartLocal, decision?.StartUtc,
                    decision?.EndLocal, decision?.EndUtc, comparison.CalculatedFastingDate, comparison.OfficialFastingDate,
                    comparison.Status, document.Rule.Tradition, document.Rule.Version, document.Rule.Source, decision?.Status,
                    decision?.Explanation, document.Reference?.Source, document.SchemaVersion, document.Astronomy.Provider,
                    document.Astronomy.Version, document.Astronomy.Ephemeris, document.Reference?.RetrievedOn, document.Rule.IsCalendarValidated,
                    observation?.ArunodayaUtc, observation?.ArunodayaUtc is { } arun ? location.GetLocalTime(arun) : null,
                    observation?.ArunodayaTithi, observation?.SunsetUtc,
                    observation?.SunsetUtc is { } sunset ? location.GetLocalTime(sunset) : null, observation?.SunsetTithi, observation?.Nakshatra);
            }
        }
        return text.ToString();

        void Write(params object?[] cells)
        {
            text.AppendJoin(',', cells.Select(value => Quote(value switch
            {
                null => string.Empty,
                DateTimeOffset instant => instant.ToString("O", CultureInfo.InvariantCulture),
                DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
                _ => SpreadsheetText(value.ToString() ?? string.Empty)
            })));
            text.Append("\r\n");
        }
    }

    private static string Quote(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
    private static string SpreadsheetText(string value) => value.Length > 0 && "=+-@\t\r".Contains(value[0]) ? "'" + value : value;
}
