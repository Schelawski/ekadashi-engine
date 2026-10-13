# Astronomische Basis (Issue #3)

## Aufbau und Nutzung

`EkadashiEngine.Core` enthält die Schnittstellen `IAstronomyService`,
`ISunriseCalculator`, `ITithiCalculator`, den `TithiCalculator` und den
`AstronomyDiagnosticsService`. Der separate Adapter
`EkadashiEngine.Astronomy.SwissEphemeris` implementiert Positionen und Sonnenaufgang
mit **SwissEphSharp 2.10.3.1 / Swiss Ephemeris 2.10.03**.
Core kennt weder SwissEphSharp noch Avalonia. NodaTime 3.3.4 liefert die
plattformunabhängigen IANA-Zeitzonen; Drittanbieter-Typen erscheinen nicht in der
öffentlichen Astronomie-API. Zum Wechsel der astronomischen Bibliothek müssen
nur die beiden Provider-Schnittstellen neu implementiert und in der DI registriert werden.

Beispiel ohne Desktop-Abhängigkeit:

```csharp
using EkadashiEngine.Core;
using EkadashiEngine.Core.Astronomy;
using EkadashiEngine.Astronomy.SwissEphemeris;

var location = new Location("Berlin", "DE", 52.52, 13.405, "Europe/Berlin");
var astronomy = new SwissEphemerisAstronomyService();
var tithi = new TithiCalculator();
var diagnostics = new AstronomyDiagnosticsService(astronomy, astronomy, tithi);

var result = diagnostics.Calculate(
    new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero), location);
// result: InstantUtc, Location, SunLongitude, MoonLongitude,
// AngularDifference, TithiNumber, Sunrise (status, UTC and local time).

var sunrise = astronomy.CalculateSunrise(
    new DateOnly(2026, 1, 15), location, SunriseDefinition.HinduGeometricCenter);
```

Die Diagnose berechnet die Tithi **am angefragten Zeitpunkt**, nicht automatisch
am Sonnenaufgang. Ihr Sonnenaufgang gehört zum lokalen Kalenderdatum dieses
Zeitpunkts am gewählten Ort. Um die Tithi am Sonnenaufgang zu ermitteln, erst
`CalculateSunrise` aufrufen, `Status == Available` prüfen und dann
`GetPositions(sunrise.SunriseUtc.Value)` mit `ITithiCalculator.Calculate` kombinieren.
Noch keine dieser Funktionen entscheidet über einen traditionellen Fastentag.

## Rechenkonventionen

- **Positionen:** scheinbare geozentrische ekliptikale Längen des Datums in Grad,
  tropisch, normiert auf `[0, 360)`. Keine topozentrische Mondparallaxe, keine
  siderische Ayanamsha. Bei gleichem Zeitpunkt sind diese Werte ortsunabhängig.
- **Ephemeride:** explizit `SEFLG_MOSEPH`, also die in Swiss Ephemeris enthaltene
  analytische Moshier-Ephemeride. Keine `.se1`-Dateien und keine nativen DLLs
  erforderlich. Es wird kein dateibasiertes `SEFLG_SWIEPH` vorgetäuscht; der
  Adapter kontrolliert die zurückgegebenen Positionsflags.
- **Tithi:** `normalize(moon - sun)`, geteilt durch 12 Grad, abgerundet und plus 1.
  Ergebnis 1–30; Ekadashi entspricht später 11 bzw. 26. Exakte 12-Grad-Grenzen
  gehören zur neuen Tithi. Eingaben werden vor der Subtraktion normiert;
  NaN und unendliche Werte werden abgewiesen.
- **Zeit:** `DateTimeOffset`-Eingaben werden auf UTC normalisiert. Swiss Ephemeris
  erhält über `swe_utc_to_jd` UT1; Rückwerte werden über `swe_jdut1_to_utc` wieder
  zu UTC. UTC wird nicht stillschweigend mit UT1 oder TT gleichgesetzt. Kalender:
  proleptisch gregorianisch.
- **Ort:** Breitengrad −90…90, Längengrad −180…180; Osten/Norden positiv.
  Ungültige Koordinaten und unbekannte IANA-Zeitzonen werden früh abgewiesen.
- **Lokaler Tag:** Die Suche startet am ersten existierenden Zeitpunkt des Tages
  in dessen Zeitzone. Mitternachtslücken und doppelte Mitternächte werden über
  NodaTime aufgelöst. Der lokale Offset wird erneut am gefundenen Sonnenaufgang
  bestimmt, damit Sommerzeitwechsel innerhalb eines Tages berücksichtigt sind.
- **Nebenläufigkeit:** Jeder Aufruf besitzt eine eigene `SwissEph`-Instanz und
  gibt sie frei. Ein gemeinsam genutzter Adapter enthält keinen veränderlichen
  Ephemeridenzustand. Die API ist synchron; spätere Jahresberechnungen sollen
  außerhalb des UI-Threads laufen.

## Sonnenaufgang ist eine explizite Konvention

| Definition | Bedeutung |
| --- | --- |
| `UpperLimbWithRefraction` (Standard) | Oberer Sonnenrand am ebenen Horizont, atmosphärische Refraktion, Meereshöhe, 1013,25 hPa und 15 °C |
| `HinduGeometricCenter` | `SE_BIT_HINDU_RISING`: geozentrischer Sonnenmittelpunkt am Horizont, ohne Refraktion, Sonnenbreite ignoriert |

Beide Definitionen werden getestet. Keine wird in dieser Phase als verbindliche
Ananda-Marga-Regel festgelegt. Gelände, Hindernisse, individuelle Höhe und
tatsächliche Wetterbedingungen werden noch nicht modelliert.

`SunriseResult` enthält `LocalDate`, `Definition`, `Status`, `SunriseUtc` und
`SunriseLocal`. Bei `NoSunriseOnLocalDate` (z. B. Polartag/-nacht) und
`SkippedLocalDate` (z. B. Apia am 30.12.2011) bleiben die Zeiten `null`.
Ein späterer Sonnenaufgang wird niemals als Ergebnis des angefragten Tages
ausgegeben. Ephemeriden-/Berechnungsfehler werfen `AstronomyCalculationException`
und werden nicht als fehlender Sonnenaufgang verschleiert.

## Grenzen

Moshier deckt nicht den gesamten .NET-Datumsbereich ab (laut Swiss Ephemeris etwa
3000 v. Chr. bis 3000 n. Chr.). Anfragen außerhalb des vom Backend akzeptierten
Zeitraums schlagen ausdrücklich fehl. Die Jahresauswahl der UI ist noch kein
fachlich zugesicherter Berechnungszeitraum. Die festen Vergleichsfälle prüfen 2026;
eine umfassende Validierung anderer Epochen und offizieller Kalender steht aus.

Die UTC-Konvertierung folgt den Schaltsekunden- und Delta-T-Modellen der
Bibliothek. Vor 1972 behandelt Swiss Ephemeris Eingaben als UT1; zukünftige
Schaltsekunden sind nicht vorhersagbar. `DateTimeOffset` kann die Sekunde 23:59:60
nicht darstellen; ein solches Rückgabeereignis wird auf die nächste Minute abgebildet.
Historische Zeitzonenoffsets mit Sekundenanteil sind in `DateTimeOffset` ebenfalls
nicht vollständig darstellbar. Schwerpunkt dieser Implementierung sind moderne
Kalenderdaten, nicht sekundengenaue historische Rekonstruktion.

## Referenztests und Reproduktion

`data/reference/astronomy/swetest-2.10.03.json` wurde mit dem **nativen C-Programm
von Astrodienst** erzeugt, nicht durch diesen Adapter. Enthalten sind Positionen,
Tithi und beide Sonnenaufgangsdefinitionen für:

- Berlin: 15.01., 21.06., 29.03. und 25.10.2026
- Moskau und Delhi: jeweils 15.01. und 21.06.2026
- Kiritimati (UTC+14) und Los Angeles: jeweils 15.01.2026

Die JSON-Datei enthält den Commit-Link des Referenzprogramms, seinen SHA-256-Hash,
die genauen Aufrufargumente und die Toleranzen. Positionen werden auf **0,00001°**
geprüft. `swetest -rise` gibt **UT1** aus; diese Werte sind bewusst ohne UTC-Suffix
gespeichert. Der Vergleich mit UTC erlaubt **2 Sekunden**, einschließlich
UT1−UTC und der Ausgabeauflösung von 0,1 Sekunden für diese modernen Fälle.
Die Tithi-Nummer muss exakt übereinstimmen. Das prüft den C#-Port/Adapter gegen
das C-Original, ist aber keine unabhängige Bestätigung des astronomischen Modells
und keine Validierung religiöser Fastentage.

Die normalen Tests lesen nur diese fest gespeicherte Datei. Weder Python noch
`swetest` oder Netzwerkzugriff sind dafür erforderlich. Zur bewussten Regeneration
das in der JSON-Datei verlinkte Programm herunterladen und mit Python 3.9+ ausführen
(unter Windows mit installierter `tzdata`-Datenbank für `zoneinfo`):

```powershell
python tools/generate_astronomy_reference.py <path-to-swetest> data/reference/astronomy/swetest-2.10.03.json
```

Die Generator-Datei ist Prüfwerkzeug, keine Laufzeitabhängigkeit. Neue Referenzwerte
nicht automatisch bei einem fehlschlagenden Test ersetzen. Änderungen an
Astronomie- oder Zeitzonenpaketen erfordern eine Prüfung der Abweichungen.

Zusätzliche Tests prüfen alle Tithi-Grenzen, ungültige Eingaben, UTC-Offsets,
Sommerzeit, Polartag/-nacht, einen übersprungenen Kalendertag, parallele Nutzung
und das Zusammenspiel der abstrahierten Dienste.

## Quellen und Lizenz

- [Swiss-Ephemeris-Programmierschnittstelle](https://www.astro.com/swisseph/swephprg.htm)
- [SwissEphSharp](https://github.com/Tim81/SwissEphNet)
- [NodaTime: DateTimeZone.AtStartOfDay](https://nodatime.org/3.3.x/api/NodaTime.DateTimeZone.html)
- [Lizenz- und Urheberhinweise](../THIRD-PARTY-NOTICES.md)
