namespace EkadashiEngine.Core.Astronomy;

public interface IAstronomyService
{
    // Geocentric apparent ecliptic longitudes of date, in degrees [0, 360).
    // The same instant has the same positions for every terrestrial location.
    PlanetaryPositions GetPositions(DateTimeOffset instant);
}
