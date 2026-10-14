namespace EkadashiEngine.Core.Astronomy;

public interface IDayAstronomyCalculator
{
    DailyAstronomyResult Calculate(DateOnly date, Location location);
}

public sealed class DayAstronomyCalculator(IAstronomyService astronomy, ISunriseCalculator sunrise, ITithiCalculator tithi)
    : IDayAstronomyCalculator
{
    public DailyAstronomyResult Calculate(DateOnly date, Location location)
    {
        var rise = sunrise.CalculateSunrise(date, location, SunriseDefinition.UpperLimbWithRefraction);
        if (rise.Status != SunriseStatus.Available) return new(date, rise, null, null);
        var position = astronomy.GetPositions(rise.SunriseUtc
            ?? throw new AstronomyCalculationException("An available sunrise must have a UTC instant."));
        return new(date, rise, position, tithi.Calculate(position.SunLongitude, position.MoonLongitude));
    }
}
