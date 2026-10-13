using EkadashiEngine.Core.Astronomy;

namespace EkadashiEngine.Astronomy.Tests;

public sealed class TithiCalculatorTests
{
    private readonly TithiCalculator _calculator = new();

    [Theory]
    [InlineData(0, 0, 0, 1)]
    [InlineData(350, 2, 12, 2)]
    [InlineData(360, 120, 120, 11)]
    [InlineData(0, 300, 300, 26)]
    [InlineData(0, 359.999, 359.999, 30)]
    [InlineData(-10, 2, 12, 2)]
    [InlineData(720, -12, 348, 30)]
    [InlineData(double.Epsilon, 0, 359.99999999999994, 30)]
    public void NormalizesLongitudesAndReturnsOneBasedTithi(double sun, double moon, double angle, int number)
    {
        var result = _calculator.Calculate(sun, moon);
        Assert.Equal(angle, result.AngularDifference, 8);
        Assert.Equal(number, result.TithiNumber);
    }

    [Fact]
    public void EveryTwelveDegreeBoundaryBelongsToTheNextTithi()
    {
        for (var number = 1; number <= 30; number++)
        {
            var boundary = number * 12d;
            Assert.Equal(number, _calculator.Calculate(0, boundary - 1e-8).TithiNumber);
            Assert.Equal(number % 30 + 1, _calculator.Calculate(0, boundary).TithiNumber);
            Assert.Equal(number % 30 + 1, _calculator.Calculate(0, boundary + 1e-8).TithiNumber);
        }
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void RejectsNonFiniteAngles(double invalid)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.Calculate(invalid, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.Calculate(0, invalid));
    }

    [Fact]
    public void FiniteExtremeAnglesDoNotOverflow()
    {
        var result = _calculator.Calculate(-double.MaxValue, double.MaxValue);
        Assert.InRange(result.TithiNumber, 1, 30);
        Assert.InRange(result.AngularDifference, 0, Math.BitDecrement(360d));
    }
}
