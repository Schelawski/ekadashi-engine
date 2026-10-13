namespace EkadashiEngine.Core.Astronomy;

/// <summary>Finds complete tithis intersecting a requested UTC range, independently of tradition.</summary>
public sealed class TithiIntervalCalculator(IAstronomyService astronomy, ITithiCalculator tithi)
    : ITithiIntervalCalculator
{
    public static readonly TimeSpan BoundaryTolerance = TimeSpan.FromMilliseconds(100);

    public IReadOnlyList<TithiInterval> Calculate(DateTimeOffset startUtc, DateTimeOffset endUtc,
        CancellationToken cancellationToken = default)
    {
        if (endUtc <= startUtc)
            throw new ArgumentException("The interval must have positive duration.", nameof(endUtc));
        startUtc = startUtc.ToUniversalTime();
        endUtc = endUtc.ToUniversalTime();
        // Two days on each side exceed a lunar day's duration in the supported modern era.
        var cursor = startUtc.AddDays(-2);
        var stop = endUtc.AddDays(2);
        var phase = Phase(cursor);
        DateTimeOffset? previousBoundary = null;
        var number = 0;
        var result = new List<TithiInterval>();
        while (cursor < stop)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var next = cursor.AddHours(6);
            if (next > stop) next = stop;
            var nextPhase = Phase(next);
            var advance = Forward(phase, nextPhase);
            if (advance <= 0 || advance >= 12)
                throw new AstronomyCalculationException("Expected monotonic lunar elongation advancing less than one tithi per six hours.");
            var distance = (Math.Floor(phase / 12) + 1) * 12 - phase;
            if (advance >= distance)
            {
                var left = cursor;
                var right = next;
                while (right - left > BoundaryTolerance)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var middle = left + (right - left) / 2;
                    if (Forward(phase, Phase(middle)) < distance) left = middle;
                    else right = middle;
                }
                var boundary = left + (right - left) / 2;
                if (previousBoundary is { } start && start < endUtc && boundary > startUtc)
                    result.Add(new(number, start, boundary));
                previousBoundary = boundary;
                number = ((int)Math.Floor(phase / 12) + 1) % 30 + 1;
            }
            cursor = next;
            phase = nextPhase;
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (result.Count == 0 || result[0].StartUtc > startUtc || result[^1].EndUtc < endUtc)
            throw new AstronomyCalculationException("Could not bracket the requested range with complete lunar days.");
        return result.AsReadOnly();

        double Phase(DateTimeOffset instant)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var position = astronomy.GetPositions(instant);
            return tithi.Calculate(position.SunLongitude, position.MoonLongitude).AngularDifference;
        }
    }

    private static double Forward(double from, double to) => (to - from + 360) % 360;
}
