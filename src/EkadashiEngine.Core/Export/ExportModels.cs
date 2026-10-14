using EkadashiEngine.Core.Astronomy;
using EkadashiEngine.Core.ReferenceData;
using EkadashiEngine.Core.Diagnostics;
using EkadashiEngine.Core.Rules;

namespace EkadashiEngine.Core.Export;

public enum ExportFormat { Json, Csv, TestVectors }
public sealed record ExportPayload(string SuggestedFileName, string Extension, byte[] Content);
public sealed record ExportLocation(string DisplayName, string Country, double Latitude, double Longitude, string TimeZone);
public sealed record ExportRule(Tradition Tradition, string Version, string? Source, bool IsCalendarValidated);
public sealed record ExportConventions(string TimeZoneDatabase, string Longitudes, string Angles, string Intervals);
public sealed record ExportDay(DateOnly Date, SunriseStatus SunriseStatus, SunriseDefinition SunriseDefinition,
    DateTimeOffset? SunriseUtc, DateTimeOffset? SunriseLocal, double? SunLongitude, double? MoonLongitude,
    double? AngularDifference, int? Tithi, bool? IsEkadashiCandidate, bool? Fasting, bool? OfficialFasting);
public sealed record ExportDecision(int Tithi, DateTimeOffset StartUtc, DateTimeOffset StartLocal,
    DateTimeOffset EndUtc, DateTimeOffset EndLocal, DateOnly? FastingDate,
    RuleDecisionStatus Status, string Explanation, IReadOnlyList<ExportSolarDay> SolarDays);
public sealed record ExportSolarDay(DateOnly Date, DateTimeOffset SunriseUtc, DateTimeOffset NextSunriseUtc, double OverlapSeconds);
public sealed record ExportComparison(DateOnly? CalculatedFastingDate, DateOnly? OfficialFastingDate,
    CalendarComparisonStatus Status, IReadOnlyList<ExportDay> Days, IReadOnlyList<ExportDecision> Decisions);
public sealed record ExportStatistics(int? ReferenceCount, int? Matches, int? Mismatches, int? UnexpectedCalculated, double? MatchRate);
public sealed record AnalysisDocument(int SchemaVersion, string Kind, ExportLocation Location, int Year,
    AstronomyProvenance Astronomy, ExportConventions Conventions, ExportRule Rule, CalendarReference? Reference,
    ExportStatistics? Statistics, IReadOnlyList<ExportDay> Days, IReadOnlyList<ExportDecision> Decisions,
    IReadOnlyList<ExportComparison> Comparisons);

public sealed record VectorTolerances(double LongitudeDegrees, double SunriseSeconds, double TithiBoundarySeconds);
public sealed record RuleTestCase(int Tithi, DateTimeOffset StartUtc, DateTimeOffset EndUtc,
    IReadOnlyList<ExportSolarDay> SolarDays, DateOnly? ExpectedFastingDate, RuleDecisionStatus ExpectedStatus);
public sealed record TestVectorDocument(int SchemaVersion, string Kind, ExportLocation Location, int Year,
    AstronomyProvenance Astronomy, ExportConventions Conventions, ExportRule Rule, CalendarReference? Reference, VectorTolerances Tolerances,
    IReadOnlyList<ExportDay> Vectors, IReadOnlyList<RuleTestCase> RuleCases);
