namespace EkadashiEngine.Core.Astronomy;

public sealed record YearAstronomyResult(Location Location, int Year, IReadOnlyList<DailyAstronomyResult> Days);
