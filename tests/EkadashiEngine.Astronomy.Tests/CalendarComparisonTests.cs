using System.Text;
using EkadashiEngine.Core;
using EkadashiEngine.Core.ReferenceData;
using EkadashiEngine.Core.Rules;

namespace EkadashiEngine.Astronomy.Tests;

public sealed class CalendarComparisonTests
{
    private static readonly Location Berlin = LocationCatalog.DefaultLocations[0];
    private static readonly DateOnly First = new(2026, 1, 14);

    [Fact]
    public void EmbeddedReferenceMatchesOnlyItsYearTraditionAndComparisonCoordinates()
    {
        var provider = new EmbeddedCalendarReferenceProvider();
        var reference = provider.Find(2026, Berlin, Tradition.AnandaMarga);
        Assert.NotNull(reference);
        Assert.Equal(24, reference.FastingDates.Count);
        Assert.Equal(First, reference.FastingDates[0]);
        Assert.Contains("anandamarga.eu", reference.Source);
        Assert.Null(provider.Find(2025, Berlin, Tradition.AnandaMarga));
        Assert.Null(provider.Find(2027, Berlin, Tradition.AnandaMarga));
        Assert.NotNull(provider.Find(2026, Berlin, Tradition.GaudiyaVaishnava));
        Assert.Null(provider.Find(2026, Berlin, Tradition.TechnicalSunriseCandidate));
        Assert.Null(provider.Find(2026, LocationCatalog.DefaultLocations[2], Tradition.AnandaMarga));
        Assert.Null(provider.Find(2026, new("Other coordinates", "DE", 52, 13, "Europe/Berlin"), Tradition.AnandaMarga));
        Assert.NotNull(provider.Find(2026, new("Renamed Berlin", "DE", Berlin.Latitude, Berlin.Longitude, Berlin.TimeZoneId), Tradition.AnandaMarga));
    }

    [Theory]
    [InlineData("2026-01-14", "2026-01-14")]
    [InlineData("2026-01-14", "2025-12-31")]
    public void ReaderRejectsDuplicateOrWrongYearDates(string first, string second)
    {
        var json = $$"""
            {"source":"https://example.org/calendar", "title":"Official calendar", "retrievedOn":"2026-10-13",
             "scope":"Test", "year":2026, "tradition":"AnandaMarga",
             "comparisonLocation":{"displayName":"Berlin", "country":"DE", "latitude":52.52,"longitude":13.405,"timeZoneId":"Europe/Berlin"},
             "fastingDates":["{{first}}","{{second}}"]}
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        Assert.Throws<InvalidDataException>(() => CalendarReferenceReader.Read(stream));
    }

    [Fact]
    public void ReaderRejectsIncompleteMetadata()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{\"year\":2026,\"fastingDates\":[\"2026-01-14\"]}"));
        Assert.Throws<InvalidDataException>(() => CalendarReferenceReader.Read(stream));
    }

    [Fact]
    public void OneDayMismatchIsPairedAndStatisticsUseReferenceCount()
    {
        var result = new CalendarComparisonService().Compare(Calculated(First.AddDays(-1), First.AddDays(15)), Reference(First, First.AddDays(15)));
        Assert.Equal(2, result.ReferenceCount);
        Assert.Equal(1, result.Matches);
        Assert.Equal(1, result.Mismatches);
        Assert.Equal(0, result.UnexpectedCalculated);
        Assert.Equal(50, result.MatchRate);
        Assert.Equal(CalendarComparisonStatus.Mismatch, result.Rows[0].Status);
        Assert.Equal(First.AddDays(-1), result.Rows[0].Calculated);
        Assert.Equal(CalendarComparisonStatus.Match, result.Rows[1].Status);
    }

    [Fact]
    public void MissingAndExtraDatesDoNotShiftSubsequentMatches()
    {
        var result = new CalendarComparisonService().Compare(Calculated(First.AddDays(15), First.AddDays(25)), Reference(First, First.AddDays(15)));
        Assert.Equal(3, result.Rows.Count);
        Assert.Contains(result.Rows, row => row.Status == CalendarComparisonStatus.MissingCalculated && row.Official == First && row.Calculated is null);
        Assert.Contains(result.Rows, row => row.Status == CalendarComparisonStatus.Match && row.Official == First.AddDays(15));
        Assert.Contains(result.Rows, row => row.Status == CalendarComparisonStatus.UnexpectedCalculated && row.Official is null);
        Assert.Equal(1, result.Matches);
        Assert.Equal(1, result.Mismatches);
        Assert.Equal(1, result.UnexpectedCalculated);
    }

    [Fact]
    public void NearbyPairCannotStealAnExactMatch()
    {
        var result = new CalendarComparisonService().Compare(Calculated(First.AddDays(1)), Reference(First, First.AddDays(1)));
        Assert.Equal(CalendarComparisonStatus.MissingCalculated, result.Rows[0].Status);
        Assert.Equal(CalendarComparisonStatus.Match, result.Rows[1].Status);
    }

    [Fact]
    public void EmptyCalculatedListIsZeroPercentRatherThanAnEmptySuccessfulComparison()
    {
        var result = new CalendarComparisonService().Compare(Calculated(), Reference(First));
        Assert.Equal(0, result.Matches);
        Assert.Equal(1, result.Mismatches);
        Assert.Equal(0, result.MatchRate);
        Assert.Equal(CalendarComparisonStatus.MissingCalculated, Assert.Single(result.Rows).Status);
    }

    [Fact]
    public void MissingReferenceDoesNotClaimZeroPercentOrMatches()
    {
        var result = new CalendarComparisonService().Compare(Calculated(First), null);
        Assert.Null(result.ReferenceCount);
        Assert.Null(result.MatchRate);
        Assert.Null(result.Matches);
        Assert.Null(result.Mismatches);
        Assert.Equal(CalendarComparisonStatus.NoReference, Assert.Single(result.Rows).Status);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RejectsComparisonWithAnotherLocationOrYear(bool changeYear)
    {
        var reference = Reference(First);
        reference = changeYear ? reference with { Year = 2025 } : reference with { ComparisonLocation = LocationCatalog.DefaultLocations[2] };
        Assert.Throws<ArgumentException>(() => new CalendarComparisonService().Compare(Calculated(First), reference));
    }

    [Fact]
    public void RejectsDuplicateCalculatedDates() => Assert.Throws<ArgumentException>(() =>
        new CalendarComparisonService().Compare(Calculated(First, First), Reference(First)));

    private static TraditionYearResult Calculated(params DateOnly[] dates) =>
        new(Berlin, 2026, Tradition.AnandaMarga, AnandaMargaRule.Version, AnandaMargaRule.Source, false, dates, []);

    private static CalendarReference Reference(params DateOnly[] dates) =>
        new("https://example.org/calendar", "Test official calendar", new(2026, 10, 13), "Test scope", 2026,
            Tradition.AnandaMarga, Berlin, dates);
}
