namespace EkadashiEngine.Core.Rules;

public interface IFastingCalendarCalculator
{
    TraditionYearResult Calculate(int year, Location location, CancellationToken cancellationToken = default);

    TraditionYearResult Calculate(int year, Location location, Tradition tradition, CancellationToken cancellationToken = default)
    {
        if (tradition != Tradition.AnandaMarga) throw new ArgumentOutOfRangeException(nameof(tradition));
        return Calculate(year, location, cancellationToken);
    }
}
