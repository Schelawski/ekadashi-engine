using EkadashiEngine.Core;
using EkadashiEngine.Core.Astronomy;
using EkadashiEngine.Core.Diagnostics;
using SwissEphNet;

namespace EkadashiEngine.Astronomy.SwissEphemeris;

public sealed class SwissEphemerisAstronomyService : IAstronomyService, ISunriseCalculator, IVaishnavaAstronomy
{
    public static AstronomyProvenance Provenance => new("SwissEphSharp",
        typeof(SwissEph).Assembly.GetName().Version!.ToString(), "Moshier (SEFLG_MOSEPH)");
    // Use the analytic ephemeris explicitly: no data files or silent file fallback.
    private const int EphemerisFlags = SwissEph.SEFLG_MOSEPH;

    public PlanetaryPositions GetPositions(DateTimeOffset instant)
    {
        // Each call owns its mutable SwissEph instance, including in parallel use.
        using var swiss = new SwissEph();
        var julianDay = ToJulianDayUt1(swiss, instant);
        return new PlanetaryPositions(
            GetLongitude(swiss, julianDay, SwissEph.SE_SUN),
            GetLongitude(swiss, julianDay, SwissEph.SE_MOON));
    }

    public SunriseResult CalculateSunrise(DateOnly localDate, Location location,
        SunriseDefinition definition = SunriseDefinition.UpperLimbWithRefraction)
        => CalculateSolarEvent(localDate, location, definition, false);

    public DateTimeOffset? GetSunsetUtc(DateOnly date, Location location) =>
        CalculateSolarEvent(date, location, SunriseDefinition.UpperLimbWithRefraction, true).SunriseUtc;

    public int GetNakshatra(DateTimeOffset instant)
    {
        using var swiss = new SwissEph();
        swiss.swe_set_sid_mode(SwissEph.SE_SIDM_LAHIRI, 0, 0);
        var jd = ToJulianDayUt1(swiss, instant);
        var longitude = GetLongitude(swiss, jd, SwissEph.SE_MOON) - swiss.swe_get_ayanamsa_ut(jd);
        return (int)Math.Floor(((longitude % 360 + 360) % 360) / (360.0 / 27));
    }

    private static SunriseResult CalculateSolarEvent(DateOnly localDate, Location location,
        SunriseDefinition definition, bool sunset)
    {
        ArgumentNullException.ThrowIfNull(location);
        if (!Enum.IsDefined(definition))
            throw new ArgumentOutOfRangeException(nameof(definition));
        var start = location.GetStartOfDayUtc(localDate);
        if (start is null)
            return new(localDate, definition, SunriseStatus.SkippedLocalDate, null, null);

        using var swiss = new SwissEph();
        var searchStart = ToJulianDayUt1(swiss, start.Value);
        var riseFlags = sunset ? SwissEph.SE_CALC_SET : SwissEph.SE_CALC_RISE;
        if (definition == SunriseDefinition.HinduGeometricCenter)
            riseFlags |= SwissEph.SE_BIT_HINDU_RISING;

        double eventTime = 0;
        var error = string.Empty;
        var status = swiss.swe_rise_trans(searchStart, SwissEph.SE_SUN, string.Empty,
            EphemerisFlags, riseFlags, [location.Longitude, location.Latitude, 0],
            1013.25, 15, ref eventTime, ref error);

        if (status == -2)
            return new(localDate, definition, SunriseStatus.NoSunriseOnLocalDate, null, null);
        if (status < 0)
            throw new AstronomyCalculationException($"Swiss Ephemeris sunrise calculation failed: {error}");

        var utc = FromJulianDayUt1(swiss, eventTime);
        var local = location.GetLocalTime(utc);
        // The API searches for the next event; never assign a later day's sunrise to this day.
        if (DateOnly.FromDateTime(local.DateTime) != localDate)
            return new(localDate, definition, SunriseStatus.NoSunriseOnLocalDate, null, null);

        return new(localDate, definition, SunriseStatus.Available, utc, local);
    }

    private static double GetLongitude(SwissEph swiss, double julianDay, int body)
    {
        var values = new double[6];
        var error = string.Empty;
        var flags = swiss.swe_calc_ut(julianDay, body, EphemerisFlags, values, ref error);
        if (flags < 0)
            throw new AstronomyCalculationException($"Swiss Ephemeris position calculation failed: {error}");
        if ((flags & EphemerisFlags) == 0 || !double.IsFinite(values[0]))
            throw new AstronomyCalculationException("Swiss Ephemeris returned an unexpected ephemeris or longitude.");
        return values[0];
    }

    private static double ToJulianDayUt1(SwissEph swiss, DateTimeOffset instant)
    {
        var utc = instant.UtcDateTime;
        var result = new double[2];
        var error = string.Empty;
        var seconds = utc.Second + (utc.Ticks % TimeSpan.TicksPerSecond) / (double)TimeSpan.TicksPerSecond;
        var status = swiss.swe_utc_to_jd(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute,
            seconds, SwissEph.SE_GREG_CAL, result, ref error);
        if (status < 0)
            throw new AstronomyCalculationException($"Swiss Ephemeris UTC conversion failed: {error}");
        return result[1];
    }

    private static DateTimeOffset FromJulianDayUt1(SwissEph swiss, double julianDay)
    {
        int year = 0, month = 0, day = 0, hour = 0, minute = 0;
        double seconds = 0;
        swiss.swe_jdut1_to_utc(julianDay, SwissEph.SE_GREG_CAL,
            ref year, ref month, ref day, ref hour, ref minute, ref seconds);
        // DateTimeOffset cannot represent 23:59:60; AddSeconds maps it to the next minute.
        return new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero).AddSeconds(seconds);
    }
}
