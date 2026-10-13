using EkadashiEngine.Core.Astronomy;

namespace EkadashiEngine.Core.Rules;

/// <summary>Prepared data includes adjacent years so selection can precede year filtering.</summary>
public sealed record PreparedTraditionYear(Location Location, int Year,
    IReadOnlyList<SolarDay> SolarDays, IReadOnlyList<TithiInterval> Tithis);

/// <summary>Null boundaries indicate an unavailable sunrise; no artificial fallback is used.</summary>
public sealed record SolarDay(DateOnly LocalDate, DateTimeOffset? SunriseUtc, DateTimeOffset? NextSunriseUtc);
