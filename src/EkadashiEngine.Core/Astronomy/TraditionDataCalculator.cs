using EkadashiEngine.Core.Rules;

namespace EkadashiEngine.Core.Astronomy;

/// <summary>Prepares astronomy without choosing any fasting dates.</summary>
public sealed class TraditionDataCalculator(ISunriseCalculator sunrise, ITithiIntervalCalculator intervals)
{
    public PreparedTraditionYear Calculate(int year, Location location, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);
        if (year is < YearAstronomyCalculator.MinimumYear or > YearAstronomyCalculator.MaximumYear)
            throw new ArgumentOutOfRangeException(nameof(year));
        var first = new DateOnly(year, 1, 1).AddDays(-4);
        var stop = new DateOnly(year + 1, 1, 1).AddDays(4);
        var rises = new List<SunriseResult>();
        for (var date = first; date <= stop; date = date.AddDays(1))
        {
            cancellationToken.ThrowIfCancellationRequested();
            rises.Add(sunrise.CalculateSunrise(date, location, SunriseDefinition.UpperLimbWithRefraction));
        }
        var days = rises.Zip(rises.Skip(1), (a, b) => new SolarDay(a.LocalDate,
            a.Status == SunriseStatus.Available ? a.SunriseUtc : null,
            b.Status == SunriseStatus.Available ? b.SunriseUtc : null)).ToArray();
        // UTC padding includes every local year boundary, even at international date-line offsets.
        var tithis = intervals.Calculate(new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(-2),
            new DateTimeOffset(year + 1, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(2), cancellationToken);
        return new(location, year, Array.AsReadOnly(days), tithis);
    }
}
