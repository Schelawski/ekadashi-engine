namespace EkadashiEngine.Core.Rules;

public interface IFastingCalendarCalculator
{
    TraditionYearResult Calculate(int year, Location location, CancellationToken cancellationToken = default);
}
