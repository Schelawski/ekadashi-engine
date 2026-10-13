namespace EkadashiEngine.Core.Astronomy;

public interface ISunriseCalculator
{
    SunriseResult CalculateSunrise(DateOnly localDate, Location location,
        SunriseDefinition definition = SunriseDefinition.UpperLimbWithRefraction);
}
