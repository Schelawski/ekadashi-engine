using EkadashiEngine.Core;
using EkadashiEngine.Core.Astronomy;
using EkadashiEngine.Core.Diagnostics;

namespace EkadashiEngine.Tests;

internal static class TestCalculationServices
{
    public static CalculationSnapshotFactory Factory => new(new SyntheticDayCalculator(), new("Synthetic", "1", "Test"));

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
}
