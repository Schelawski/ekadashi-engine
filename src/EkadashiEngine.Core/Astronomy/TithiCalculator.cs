namespace EkadashiEngine.Core.Astronomy;

public sealed class TithiCalculator : ITithiCalculator
{
    public TithiInfo Calculate(double sunLongitude, double moonLongitude)
    {
        if (!double.IsFinite(sunLongitude))
            throw new ArgumentOutOfRangeException(nameof(sunLongitude));
        if (!double.IsFinite(moonLongitude))
            throw new ArgumentOutOfRangeException(nameof(moonLongitude));

        // Reduce each operand first to avoid overflowing the subtraction.
        var difference = Normalize(Normalize(moonLongitude) - Normalize(sunLongitude));
        return new TithiInfo(difference, (int)Math.Floor(difference / 12) + 1);
    }

    private static double Normalize(double angle)
    {
        var reduced = angle % 360;
        if (reduced < 0)
            reduced += 360;
        // Adding 360 to a tiny negative angle can round up to the excluded endpoint.
        return reduced >= 360 ? Math.BitDecrement(360d) : reduced;
    }
}
