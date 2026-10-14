namespace EkadashiEngine.Core.Astronomy;

/// <summary>Additional observations needed by the nakshatra Mahadvadashi rules.</summary>
public interface IVaishnavaAstronomy
{
    DateTimeOffset? GetSunsetUtc(DateOnly date, Location location);
    int GetNakshatra(DateTimeOffset instant);
}
