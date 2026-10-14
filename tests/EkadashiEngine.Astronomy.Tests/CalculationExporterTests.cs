using System.Globalization;
using System.Text;
using System.Text.Json;
using EkadashiEngine.Core;
using EkadashiEngine.Core.Astronomy;
using EkadashiEngine.Core.Diagnostics;
using EkadashiEngine.Core.Export;
using EkadashiEngine.Core.ReferenceData;
using EkadashiEngine.Core.Rules;
using Microsoft.VisualBasic.FileIO;

namespace EkadashiEngine.Astronomy.Tests;

public sealed class CalculationExporterTests
{
    [Fact]
    public void CsvTreatsFormulaLikeLocationNamesAsText()
    {
        var payload = new CalculationExporter().Create(CreateSnapshot("=1+1"), ExportFormat.Csv);
        Assert.Contains("\"'=1+1\"", Encoding.UTF8.GetString(payload.Content));
    }

    [Fact]
    public void JsonContainsBothDatesAllRequiredDiagnosticsAndRuleProvenance()
    {
        var snapshot = CreateSnapshot();
        var payload = new CalculationExporter().Create(snapshot, ExportFormat.Json);
        using var json = JsonDocument.Parse(payload.Content);
        var root = json.RootElement;
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("Europe/Berlin", root.GetProperty("location").GetProperty("timeZone").GetString());
        Assert.Equal(AnandaMargaRule.Version, root.GetProperty("rule").GetProperty("version").GetString());
        Assert.False(root.GetProperty("rule").GetProperty("isCalendarValidated").GetBoolean());
        var comparison = Assert.Single(root.GetProperty("comparisons").EnumerateArray());
        Assert.Equal("2026-01-13", comparison.GetProperty("calculatedFastingDate").GetString());
        Assert.Equal("2026-01-14", comparison.GetProperty("officialFastingDate").GetString());
        var days = comparison.GetProperty("days").EnumerateArray().ToArray();
        Assert.Equal(2, days.Length);
        Assert.Equal("2026-01-13", days[0].GetProperty("date").GetString());
        Assert.Equal("2026-01-14", days[1].GetProperty("date").GetString());
        foreach (var day in days)
        {
            Assert.NotEqual(JsonValueKind.Null, day.GetProperty("sunriseLocal").ValueKind);
            Assert.NotEqual(JsonValueKind.Null, day.GetProperty("sunriseUtc").ValueKind);
            Assert.NotEqual(JsonValueKind.Null, day.GetProperty("sunLongitude").ValueKind);
            Assert.NotEqual(JsonValueKind.Null, day.GetProperty("moonLongitude").ValueKind);
            Assert.NotEqual(JsonValueKind.Null, day.GetProperty("angularDifference").ValueKind);
            Assert.NotEqual(JsonValueKind.Null, day.GetProperty("tithi").ValueKind);
        }
        var decision = Assert.Single(comparison.GetProperty("decisions").EnumerateArray());
        foreach (var field in new[] { "startUtc", "startLocal", "endUtc", "endLocal" })
            Assert.NotNull(decision.GetProperty(field).GetString());
        Assert.Equal(2, decision.GetProperty("solarDays").GetArrayLength());
    }

    [Fact]
    public void CsvRoundTripsCommasQuotesNewlinesAndInvariantNumbers()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var snapshot = CreateSnapshot("Berlin, \"Mitte\"\nNord");
            var payload = new CalculationExporter().Create(snapshot, ExportFormat.Csv);
            Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, payload.Content.Take(3));
            using var reader = new StringReader(Encoding.UTF8.GetString(payload.Content).TrimStart('\uFEFF'));
            using var parser = new TextFieldParser(reader) { TextFieldType = FieldType.Delimited, HasFieldsEnclosedInQuotes = true };
            parser.SetDelimiters(",");
            var headers = parser.ReadFields()!;
            var rows = new List<string[]>();
            while (!parser.EndOfData) rows.Add(parser.ReadFields()!);
            Assert.Equal(2, rows.Count);
            Assert.All(rows, row =>
            {
                Assert.Equal(headers.Length, row.Length);
                Assert.Equal(snapshot.Astronomy.Location.DisplayName, row[Array.IndexOf(headers, "location")]);
                Assert.Equal("52.52", row[Array.IndexOf(headers, "latitude")]);
                Assert.Equal("2026-01-13", row[Array.IndexOf(headers, "calculatedFastingDate")]);
                Assert.Equal("2026-01-14", row[Array.IndexOf(headers, "officialFastingDate")]);
            });
        }
        finally { CultureInfo.CurrentCulture = original; }
    }

    [Fact]
    public void SerializationIsDeterministicAcrossCulturesAndHasNoGenerationTimestamp()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            var exporter = new CalculationExporter();
            var snapshot = CreateSnapshot();
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var first = exporter.Create(snapshot, ExportFormat.TestVectors).Content;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Assert.Equal(first, exporter.Create(snapshot, ExportFormat.TestVectors).Content);
            Assert.DoesNotContain("generatedAt", Encoding.UTF8.GetString(first));
        }
        finally { CultureInfo.CurrentCulture = original; }
    }

    [Fact]
    public void VectorsDistinguishTraditionalFastingFromSunriseCandidateAndOfficialDate()
    {
        var vectors = new CalculationExporter().CreateTestVectors(CreateSnapshot());
        Assert.True(vectors.Vectors[0].Fasting);
        Assert.False(vectors.Vectors[0].IsEkadashiCandidate);
        Assert.False(vectors.Vectors[0].OfficialFasting);
        Assert.False(vectors.Vectors[1].Fasting);
        Assert.True(vectors.Vectors[1].OfficialFasting);
        var rule = Assert.Single(vectors.RuleCases);
        Assert.Equal(new DateOnly(2026, 1, 13), rule.ExpectedFastingDate);
        Assert.Equal(RuleDecisionStatus.Selected, rule.ExpectedStatus);
    }

    [Fact]
    public void TechnicalModeDoesNotInventTraditionalOrOfficialBooleans()
    {
        var snapshot = CreateSnapshot();
        snapshot = snapshot with { Calendar = null, Comparison = null, Diagnostics = [] };
        var vectors = new CalculationExporter().CreateTestVectors(snapshot);
        Assert.All(vectors.Vectors, vector => { Assert.Null(vector.Fasting); Assert.Null(vector.OfficialFasting); });
        Assert.Empty(vectors.RuleCases);
        Assert.Throws<InvalidOperationException>(() => new CalculationExporter().Create(snapshot, ExportFormat.Csv));
    }

    [Fact]
    public void UnavailableSunriseIsNullInVectorsRatherThanZero()
    {
        var snapshot = CreateSnapshot();
        var missing = snapshot.Astronomy.Days[0] with
        {
            Sunrise = new(new(2026, 1, 13), SunriseDefinition.UpperLimbWithRefraction, SunriseStatus.NoSunriseOnLocalDate, null, null),
            Positions = null, Tithi = null
        };
        snapshot = snapshot with { Astronomy = snapshot.Astronomy with { Days = [missing] }, Calendar = null, Comparison = null, Diagnostics = [] };
        using var json = JsonDocument.Parse(new CalculationExporter().Create(snapshot, ExportFormat.TestVectors).Content);
        var day = Assert.Single(json.RootElement.GetProperty("vectors").EnumerateArray());
        Assert.Equal("noSunriseOnLocalDate", day.GetProperty("sunriseStatus").GetString());
        foreach (var field in new[] { "sunriseUtc", "sunLongitude", "moonLongitude", "angularDifference", "tithi", "fasting" })
            Assert.Equal(JsonValueKind.Null, day.GetProperty(field).ValueKind);
    }

    [Fact]
    public void MissingCalculatedDateStillIncludesItsUnresolvedInterval()
    {
        var snapshot = CreateSnapshot();
        var unresolved = snapshot.Calendar!.Decisions[0] with { FastingDate = null, Status = RuleDecisionStatus.MissingSunriseCoverage };
        var calendar = snapshot.Calendar with { FastingDates = [], Decisions = [unresolved] };
        var comparison = new CalendarComparisonService().Compare(calendar, snapshot.Comparison!.Reference);
        var result = new CalculationSnapshotFactory(new NeverCalledDayCalculator(), snapshot.Provenance)
            .Create(snapshot.Astronomy, calendar, comparison, TestContext.Current.CancellationToken);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(CalendarComparisonStatus.MissingCalculated, diagnostic.Comparison.Status);
        Assert.Equal(RuleDecisionStatus.MissingSunriseCoverage, Assert.Single(diagnostic.Decisions).Status);
        Assert.All(new CalculationExporter().CreateTestVectors(result).Vectors, day => Assert.Null(day.Fasting));
    }

    private static CalculationSnapshot CreateSnapshot(string name = "Berlin")
    {
        var location = new Location(name, "DE", 52.52, 13.405, "Europe/Berlin");
        var first = new DateTimeOffset(2026, 1, 13, 7, 0, 0, TimeSpan.Zero);
        var date = new DateOnly(2026, 1, 13);
        var days = Enumerable.Range(0, 2).Select(i => new DailyAstronomyResult(date.AddDays(i),
            new(date.AddDays(i), SunriseDefinition.UpperLimbWithRefraction, SunriseStatus.Available, first.AddDays(i), location.GetLocalTime(first.AddDays(i))),
            new(293 + i, 221 + i * 13), new(288 + i * 12, 25 + i))).ToArray();
        var prepared = new PreparedTraditionYear(location, 2026,
            [new(date, first, first.AddDays(1)), new(date.AddDays(1), first.AddDays(1), first.AddDays(2))],
            [new(26, first.AddHours(2), first.AddHours(30))]);
        var calendar = new AnandaMargaRule().Evaluate(prepared, TestContext.Current.CancellationToken);
        var reference = new CalendarReference("https://example.org/calendar", "Official test calendar", new(2026, 10, 14), "Test", 2026,
            Tradition.AnandaMarga, location, [date.AddDays(1)]);
        return new CalculationSnapshotFactory(new NeverCalledDayCalculator(), new("Synthetic", "1", "Test"))
            .Create(new(location, 2026, days), calendar, new CalendarComparisonService().Compare(calendar, reference), TestContext.Current.CancellationToken);
    }

    private sealed class NeverCalledDayCalculator : IDayAstronomyCalculator
    {
        public DailyAstronomyResult Calculate(DateOnly date, Location location) => throw new InvalidOperationException("Cached days must be reused.");
    }
}
