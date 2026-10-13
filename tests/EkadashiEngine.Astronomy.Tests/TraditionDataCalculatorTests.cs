using EkadashiEngine.Core;
using EkadashiEngine.Core.Astronomy;

namespace EkadashiEngine.Astronomy.Tests;

public sealed class TraditionDataCalculatorTests
{
    [Theory]
    [InlineData(1899)]
    [InlineData(2101)]
    public void RejectsUnsupportedYearsBeforeAccessingAstronomy(int year) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new TraditionDataCalculator(new UnusedSunrise(), new UnusedIntervals())
            .Calculate(year, LocationCatalog.DefaultLocations[0], TestContext.Current.CancellationToken));

    [Fact]
    public void CancellationPreventsAstronomyAccess()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => new TraditionDataCalculator(new UnusedSunrise(), new UnusedIntervals())
            .Calculate(2026, LocationCatalog.DefaultLocations[0], cancellation.Token));
    }

    private sealed class UnusedSunrise : ISunriseCalculator
    {
        public SunriseResult CalculateSunrise(DateOnly date, Location location, SunriseDefinition definition) =>
            throw new InvalidOperationException("Astronomy must not be accessed.");
    }

    private sealed class UnusedIntervals : ITithiIntervalCalculator
    {
        public IReadOnlyList<TithiInterval> Calculate(DateTimeOffset startUtc, DateTimeOffset endUtc, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Astronomy must not be accessed.");
    }
}
