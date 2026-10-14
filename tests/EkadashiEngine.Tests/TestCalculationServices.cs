using EkadashiEngine.Core;
using EkadashiEngine.Core.Astronomy;
using EkadashiEngine.Core.Diagnostics;
using EkadashiEngine.Core.Comparison;
using EkadashiEngine.Core.Rules;
using EkadashiEngine.Desktop.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace EkadashiEngine.Tests;

internal static class TestCalculationServices
{
    public static CalculationSnapshotFactory Factory => new(new SyntheticDayCalculator(), new("Synthetic", "1", "Test"));
    public static CrossLocationViewModel CrossViewModel => new(new CrossLocationCalculator(new SyntheticYearCalculator(), new SyntheticCalendarCalculator()), NullLogger<CrossLocationViewModel>.Instance);

    public static YearAstronomyResult Year(int year, Location location) => new(location, year,
        Enumerable.Range(0, DateTime.IsLeapYear(year) ? 366 : 365)
            .Select(index => new SyntheticDayCalculator().Calculate(new DateOnly(year, 1, 1).AddDays(index), location)).ToArray());

    private sealed class SyntheticDayCalculator : IDayAstronomyCalculator
    {
        public DailyAstronomyResult Calculate(DateOnly date, Location location)
        {
            var instant = location.GetStartOfDayUtc(date)?.AddHours(6);
            return new(date, new(date, SunriseDefinition.UpperLimbWithRefraction,
                instant is null ? SunriseStatus.SkippedLocalDate : SunriseStatus.Available,
                instant, instant is null ? null : location.GetLocalTime(instant.Value)),
                instant is null ? null : new(10, 130), instant is null ? null : new(120, 11));
        }
    }
    private sealed class SyntheticYearCalculator : IYearAstronomyCalculator
    {
        public YearAstronomyResult Calculate(int year, Location location, CancellationToken cancellationToken = default) => Year(year, location);
    }
    private sealed class SyntheticCalendarCalculator : IFastingCalendarCalculator
    {
        public TraditionYearResult Calculate(int year, Location location, CancellationToken cancellationToken = default) =>
            new(location, year, Tradition.AnandaMarga, AnandaMargaRule.Version, AnandaMargaRule.Source, false, [], []);
    }
}
