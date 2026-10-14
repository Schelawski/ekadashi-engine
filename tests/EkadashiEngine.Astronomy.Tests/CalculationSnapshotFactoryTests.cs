using EkadashiEngine.Core;
using EkadashiEngine.Core.Astronomy;
using EkadashiEngine.Core.Diagnostics;
using EkadashiEngine.Core.ReferenceData;
using EkadashiEngine.Core.Rules;

namespace EkadashiEngine.Astronomy.Tests;

public sealed class CalculationSnapshotFactoryTests
{
    [Fact]
    public void YearBoundaryDiagnosticsFetchNeighboringDaysAndDoNotClaimTheirFastingStatus()
    {
        var location = LocationCatalog.DefaultLocations[0];
        var dawn = new DateTimeOffset(2025, 12, 31, 7, 0, 0, TimeSpan.Zero);
        var data = new PreparedTraditionYear(location, 2026,
            [new(new(2025, 12, 31), dawn, dawn.AddDays(1)), new(new(2026, 1, 1), dawn.AddDays(1), dawn.AddDays(2))],
            [new(11, dawn.AddHours(16), dawn.AddHours(41))]);
        var calendar = new AnandaMargaRule().Evaluate(data, TestContext.Current.CancellationToken);
        Assert.Equal(new DateOnly(2026, 1, 1), Assert.Single(calendar.FastingDates));
        var dayCalculator = new RecordingDayCalculator();
        var january = dayCalculator.Calculate(new(2026, 1, 1), location);
        dayCalculator.Dates.Clear();
        var comparison = new CalendarComparisonService().Compare(calendar, null);
        var snapshot = new CalculationSnapshotFactory(dayCalculator, new("Synthetic", "1", "Test"))
            .Create(new(location, 2026, [january]), calendar, comparison, TestContext.Current.CancellationToken);
        Assert.Equal(new[] { new DateOnly(2025, 12, 31), new DateOnly(2026, 1, 2) }, dayCalculator.Dates);
        var days = Assert.Single(snapshot.Diagnostics).Days;
        Assert.Equal(3, days.Count);
        Assert.Null(snapshot.IsFasting(days[0]));
        Assert.True(snapshot.IsFasting(days[1]));
    }

    [Fact]
    public void CancellationPreventsFallbackAstronomy()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var calculator = new RecordingDayCalculator();
        Assert.Throws<OperationCanceledException>(() => new CalculationSnapshotFactory(calculator, new("Synthetic", "1", "Test"))
            .Create(new(LocationCatalog.DefaultLocations[0], 2026, []), cancellationToken: cancellation.Token));
        Assert.Empty(calculator.Dates);
    }

    private sealed class RecordingDayCalculator : IDayAstronomyCalculator
    {
        public List<DateOnly> Dates { get; } = [];
        public DailyAstronomyResult Calculate(DateOnly date, Location location)
        {
            Dates.Add(date);
            var instant = new DateTimeOffset(date.Year, date.Month, date.Day, 7, 0, 0, TimeSpan.Zero);
            return new(date, new(date, SunriseDefinition.UpperLimbWithRefraction, SunriseStatus.Available, instant, location.GetLocalTime(instant)), new(0, 120), new(120, 11));
        }
    }
}
