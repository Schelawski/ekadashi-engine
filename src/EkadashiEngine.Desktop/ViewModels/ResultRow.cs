using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using EkadashiEngine.Core;
using EkadashiEngine.Core.Astronomy;

namespace EkadashiEngine.Desktop.ViewModels;

public sealed class ResultRow
{
    public Location Location { get; }
    public DailyAstronomyResult Day { get; }
    public IRelayCommand ShowDetailsCommand { get; }
    public string LocalDate => Day.LocalDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    public string Sunrise => Day.Sunrise.SunriseLocal?.ToString("HH:mm:ss zzz", CultureInfo.InvariantCulture) ?? "—";
    public string TithiAtSunrise => Day.Tithi?.TithiNumber.ToString(CultureInfo.InvariantCulture) ?? "—";
    public string Candidate => Day.IsEkadashiCandidate switch { true => "Ja · Kandidat", false => "Nein", null => "Nicht auswertbar" };
    public string DetailTitle => $"{LocalDate} · {Location.DisplayName} · {Location.TimeZoneId}";
    public string LocationDetails => FormattableString.Invariant($"{Location.Country} · Breite {Location.Latitude:F4}° · Länge {Location.Longitude:F4}°");
    public string SunriseLocal => Day.Sunrise.SunriseLocal?.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture) ?? "—";
    public string SunriseUtc => Day.Sunrise.SunriseUtc?.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture) ?? "—";
    public string SunLongitude => FormatAngle(Day.Positions?.SunLongitude);
    public string MoonLongitude => FormatAngle(Day.Positions?.MoonLongitude);
    public string AngularDifference => FormatAngle(Day.Tithi?.AngularDifference);
    public string Explanation => Day.Sunrise.Status switch
    {
        SunriseStatus.SkippedLocalDate => "Dieser lokale Kalendertag wurde durch eine Zeitzonenänderung übersprungen. Keine Kandidatenentscheidung möglich.",
        SunriseStatus.NoSunriseOnLocalDate => "Kein Sonnenaufgang an diesem lokalen Tag. Keine Kandidatenentscheidung möglich.",
        _ => $"Tithi {TithiAtSunrise} bei Sonnenaufgang → {Candidate}. Technisches Kriterium: Tithi 11 oder 26; keine validierte traditionelle Fastenregel."
    };

    public ResultRow(Location location, DailyAstronomyResult day, Action<ResultRow> showDetails)
    {
        Location = location;
        Day = day;
        ShowDetailsCommand = new RelayCommand(() => showDetails(this));
    }

    private static string FormatAngle(double? value) => value?.ToString("F6", CultureInfo.InvariantCulture) is { } text ? $"{text}°" : "—";
}
