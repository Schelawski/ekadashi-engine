namespace EkadashiEngine.Core.Astronomy;

public interface IYearAstronomyCalculator
{
    YearAstronomyResult Calculate(int year, Location location, CancellationToken cancellationToken = default);
}
