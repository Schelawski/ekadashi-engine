using EkadashiEngine.Core.Rules;

namespace EkadashiEngine.Core.Astronomy;

/// <summary>Prepares astronomy without choosing any fasting dates.</summary>
public sealed class TraditionDataCalculator(ISunriseCalculator sunrise, ITithiIntervalCalculator intervals)
{
    public PreparedTraditionYear Calculate(int year, Location location, CancellationToken cancellationToken = default,
        bool includeVaishnava = false)
    {
        ArgumentNullException.ThrowIfNull(location);
        if (year is < YearAstronomyCalculator.MinimumYear or > YearAstronomyCalculator.MaximumYear)
            throw new ArgumentOutOfRangeException(nameof(year));
        var padding = includeVaishnava ? 12 : 4;
        var first = new DateOnly(year, 1, 1).AddDays(-padding);
        var stop = new DateOnly(year + 1, 1, 1).AddDays(padding);
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
        var tithis = intervals.Calculate(new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(includeVaishnava ? -14 : -2),
            new DateTimeOffset(year + 1, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(includeVaishnava ? 14 : 2), cancellationToken);
        if (!includeVaishnava) return new(location, year, Array.AsReadOnly(days), tithis);
        var extra = sunrise as IVaishnavaAstronomy ?? throw new InvalidOperationException("Vaishnava astronomy is unavailable.");
        int? TithiAt(DateTimeOffset? time) => time is null ? null : tithis.FirstOrDefault(t => t.StartUtc <= time && time < t.EndUtc)?.TithiNumber;
        var observations = days.Select(day =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var arunodaya = day.SunriseUtc?.AddMinutes(-96);
            var sunset = extra.GetSunsetUtc(day.LocalDate, location);
            return new VaishnavaDay(day.LocalDate, day.SunriseUtc, arunodaya, sunset,
                TithiAt(day.SunriseUtc), TithiAt(arunodaya), TithiAt(sunset),
                day.SunriseUtc is { } rise ? extra.GetNakshatra(rise) : null);
        }).ToArray();
        return new(location, year, Array.AsReadOnly(days), tithis, Array.AsReadOnly(observations));
    }
}
