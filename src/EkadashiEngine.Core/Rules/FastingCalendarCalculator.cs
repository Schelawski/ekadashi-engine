using EkadashiEngine.Core.Astronomy;

namespace EkadashiEngine.Core.Rules;

public sealed class FastingCalendarCalculator(TraditionDataCalculator dataCalculator, ITraditionRule rule) : IFastingCalendarCalculator
{
    public TraditionYearResult Calculate(int year, Location location, CancellationToken cancellationToken = default) =>
        rule.Evaluate(dataCalculator.Calculate(year, location, cancellationToken), cancellationToken);
}
