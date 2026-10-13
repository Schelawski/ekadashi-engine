namespace EkadashiEngine.Core.Astronomy;

public sealed record DailyAstronomyResult(
    DateOnly LocalDate,
    SunriseResult Sunrise,
    PlanetaryPositions? Positions,
    TithiInfo? Tithi)
{
    // A missing sunrise cannot be classified as either a candidate or a non-candidate.
    public bool? IsEkadashiCandidate => Tithi is null ? null : Tithi.TithiNumber is 11 or 26;
}
