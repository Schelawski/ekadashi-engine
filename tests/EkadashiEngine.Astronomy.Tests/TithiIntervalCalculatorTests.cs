using EkadashiEngine.Core.Astronomy;

namespace EkadashiEngine.Astronomy.Tests;

public sealed class TithiIntervalCalculatorTests
{
    private static readonly DateTimeOffset Epoch = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FindsCompleteBoundariesAcrossNewMoonAndBothEkadashis()
    {
        var result = new TithiIntervalCalculator(new LinearSky(), new TithiCalculator())
            .Calculate(Epoch.AddHours(1), Epoch.AddDays(32), TestContext.Current.CancellationToken);
        Assert.Equal(30, result[0].TithiNumber);
        Assert.Equal(1, result[1].TithiNumber);
        Assert.Contains(result, t => t.TithiNumber == 11);
        Assert.Contains(result, t => t.TithiNumber == 26);
        Assert.All(result, t => Assert.InRange(Math.Abs((t.EndUtc - t.StartUtc).TotalSeconds - 86400), 0, .2));
        Assert.InRange(Math.Abs((result[0].StartUtc - Epoch).TotalSeconds), 0, .1);
        Assert.All(result.Zip(result.Skip(1)), pair => Assert.Equal(pair.First.EndUtc, pair.Second.StartUtc));
        Assert.True(result[^1].EndUtc >= Epoch.AddDays(32));
    }

    [Fact]
    public void RejectsInvalidRange() => Assert.Throws<ArgumentException>(() =>
        new TithiIntervalCalculator(new LinearSky(), new TithiCalculator())
            .Calculate(Epoch, Epoch, TestContext.Current.CancellationToken));

    [Fact]
    public void CancellationIsObservedBeforeEphemerisAccess()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => new TithiIntervalCalculator(new LinearSky(), new TithiCalculator())
            .Calculate(Epoch, Epoch.AddDays(1), cancellation.Token));
    }

    private sealed class LinearSky : IAstronomyService
    {
        public PlanetaryPositions GetPositions(DateTimeOffset instant) => new(0, 348 + (instant - Epoch).TotalDays * 12);
    }
}
