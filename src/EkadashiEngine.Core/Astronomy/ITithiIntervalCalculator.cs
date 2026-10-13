namespace EkadashiEngine.Core.Astronomy;

public interface ITithiIntervalCalculator
{
    IReadOnlyList<TithiInterval> Calculate(DateTimeOffset startUtc, DateTimeOffset endUtc,
        CancellationToken cancellationToken = default);
}
