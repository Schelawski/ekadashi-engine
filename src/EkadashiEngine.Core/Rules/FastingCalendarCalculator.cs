using EkadashiEngine.Core.Astronomy;

namespace EkadashiEngine.Core.Rules;

public sealed class FastingCalendarCalculator(TraditionDataCalculator dataCalculator, ITraditionRule rule) : IFastingCalendarCalculator
{
    public TraditionYearResult Calculate(int year, Location location, CancellationToken cancellationToken = default) =>
        rule.Evaluate(dataCalculator.Calculate(year, location, cancellationToken,
            rule.Tradition == Tradition.GaudiyaVaishnava), cancellationToken);

    public TraditionYearResult Calculate(int year, Location location, Tradition tradition, CancellationToken cancellationToken = default)
    {
        ITraditionRule selected = tradition switch
        {
            Tradition.AnandaMarga => rule.Tradition == tradition ? rule : new AnandaMargaRule(),
            Tradition.GaudiyaVaishnava => rule.Tradition == tradition ? rule : new VaishnavaRule(),
            _ => throw new ArgumentOutOfRangeException(nameof(tradition))
        };
        return selected.Evaluate(dataCalculator.Calculate(year, location, cancellationToken,
            tradition == Tradition.GaudiyaVaishnava), cancellationToken);
    }
}
