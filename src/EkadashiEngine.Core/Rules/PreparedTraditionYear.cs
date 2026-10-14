using EkadashiEngine.Core.Astronomy;

namespace EkadashiEngine.Core.Rules;

/// <summary>Prepared data includes adjacent years so selection can precede year filtering.</summary>
public sealed record PreparedTraditionYear(Location Location, int Year,
    IReadOnlyList<SolarDay> SolarDays, IReadOnlyList<TithiInterval> Tithis,
    IReadOnlyList<VaishnavaDay>? VaishnavaDays = null);

/// <summary>Tithis are 1..30 (waxing first); nakshatra is 0..26 using Lahiri.</summary>
public sealed record VaishnavaDay(DateOnly LocalDate, DateTimeOffset? SunriseUtc,
    DateTimeOffset? ArunodayaUtc, DateTimeOffset? SunsetUtc, int? SunriseTithi,
    int? ArunodayaTithi, int? SunsetTithi, int? Nakshatra);

/// <summary>Null boundaries indicate an unavailable sunrise; no artificial fallback is used.</summary>
public sealed record SolarDay(DateOnly LocalDate, DateTimeOffset? SunriseUtc, DateTimeOffset? NextSunriseUtc);
