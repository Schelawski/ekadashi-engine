# Portable Testvektoren v1

Diese versionierten Erwartungen stammen aus der Engine mit SwissEphSharp 2.10.3.1,
Moshier und der im jeweiligen Dokument aufgezeichneten TZDB-Version. Erzeugt am
14.10.2026. Sie sind Regressions-/Portierungsdaten; offizielle AM-Referenzen
liegen getrennt unter `data/reference/ananda-marga`.

| Datei | Tageswerte | Regelfälle | Zweck |
| --- | ---: | ---: | --- |
| berlin-2026-v1.json | 9 | 24 | AM-Mismatch im Januar, AM-Treffer am 06.10., Jahresrand, europäische Sommerzeit |
| sydney-2026-v1.json | 4 | 0 | Südliche Hemisphäre und beide Sommerzeitwechsel, technischer Modus |
| tromso-2026-v1.json | 4 | 24 | AM bei Polartag/-nacht, fehlende Sonnenaufgänge, ungeklärte Entscheidungen |
| apia-2011-v1.json | 3 | 0 | Übersprungener 30.12.2011 und UTC-Offsetwechsel −10 → +14 |
| rule-edges-2026-v1.json | 0 | 7 | Synthetische kurze/lange Tithis, Gleichstand und Beinahe-Gleichstand, fehlende Abdeckung, Jahreswechsel |

Format, Einheiten, Nullwerte und Vergleichstoleranzen beschreibt
[Diagnose und Export](../../docs/diagnostics-and-export.md).
`rule-edges` enthält ausdrücklich **synthetische** Daten; seine
Sonnenaufgangsfenster sind keine astronomischen Messwerte und werden nicht mit
der Ephemeride verglichen. Die physikalischen Datensätze werden zusätzlich an
ihren aufgezeichneten Tithi-Grenzen überprüft.

Die Vektoren werden in den Tests gelesen, niemals automatisch neu geschrieben.
Eine Änderung von Astronomie, Zeitzonendaten oder Regelversion erfordert Prüfung
der Abweichungen und bewusste Aktualisierung. Das verhindert, dass eine Regression
durch unbemerkte Neugenerierung der Erwartungen verborgen wird.

## Reproduzieren

Im Repository-Stamm, mit installiertem .NET 10 SDK:

```powershell
dotnet run --project tools/EkadashiEngine.Export -- --year 2026 --location Berlin --mode am --format test-vectors --dates 2026-01-01,2026-01-13,2026-01-14,2026-03-28,2026-03-29,2026-10-06,2026-10-24,2026-10-25,2026-12-31 --output data/test-vectors/berlin-2026-v1.json
dotnet run --project tools/EkadashiEngine.Export -- --year 2026 --location Sydney --mode technical --format test-vectors --dates 2026-04-04,2026-04-05,2026-10-03,2026-10-04 --output data/test-vectors/sydney-2026-v1.json
dotnet run --project tools/EkadashiEngine.Export -- --year 2026 --location Tromso --latitude 69.6492 --longitude 18.9553 --time-zone Europe/Oslo --country NO --mode am --format test-vectors --dates 2026-01-01,2026-06-21,2026-10-06,2026-12-21 --output data/test-vectors/tromso-2026-v1.json
dotnet run --project tools/EkadashiEngine.Export -- --year 2011 --location Apia --latitude -13.8333 --longitude -171.75 --time-zone Pacific/Apia --country WS --mode technical --format test-vectors --dates 2011-12-29,2011-12-30,2011-12-31 --output data/test-vectors/apia-2011-v1.json
```

`rule-edges-2026-v1.json` wird von Hand anhand der ausdrücklich angegebenen
UTC-Fenster gepflegt. Die Regelfälle lassen sich ohne Sonnen-/Mondberechnung
direkt an `ITraditionRule` übergeben.
