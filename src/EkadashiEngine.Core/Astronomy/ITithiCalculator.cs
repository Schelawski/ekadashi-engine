namespace EkadashiEngine.Core.Astronomy;

public interface ITithiCalculator
{
    TithiInfo Calculate(double sunLongitude, double moonLongitude);
}
