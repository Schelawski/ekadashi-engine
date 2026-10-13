namespace EkadashiEngine.Core.Rules;

public interface ITraditionRule
{
    Tradition Tradition { get; }
    TraditionYearResult Evaluate(PreparedTraditionYear data, CancellationToken cancellationToken = default);
}
