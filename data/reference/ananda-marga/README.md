# Offizielle Ananda-Marga-Referenzen

Stand der Recherche: **13.10.2026**.

| Jahr | Berlin Sector | Status |
| --- | --- | --- |
| 2025 | Kein verlässlicher Datensatz gefunden | Kein Vergleich verfügbar |
| 2026 | 24 Ekadashii-Daten importiert | Vergleich verfügbar |
| 2027 | Kein verlässlicher Datensatz gefunden | Kein Vergleich verfügbar |

Quelle 2026: [Offizieller AMPS Berlin Sector](https://anandamarga.eu/fastingChart.php/1000).
Gespeichert sind ausschließlich Ekadashii-Daten; Purnima, Amavasya und Wochentage
werden nicht als Ekadashi-Referenz importiert. Die Quelle ist ein Sektorkalender,
ohne konkrete Ortskoordinaten oder erklärte Datumsauswahl. `comparisonLocation`
legt deshalb unseren Vergleichsort **Berlin** fest und ist keine Behauptung über
die Koordinaten, mit denen der Herausgeber gerechnet hat.

Die Suche nach 2025/2027 umfasste die offizielle Seite, Websuche nach Kalendern
des Berlin Sector, eine Downloadseite (404) und Jahresparameter. Die URLs
`fastingChart.php?year=2025` und `fastingChart.php?year=2027` lieferten beide den
Kalender **2026**; der Parameter ist keine Jahresauswahl. Ein Archivabruf war
mit HTTP 503 nicht verfügbar. Für 2025 gefundene Daten des Manila Sector wurden
wegen des anderen Geltungsbereichs nicht als Berlin-Referenz übernommen.
Aus der Recherche folgt keine Aussage, dass es diese Kalender grundsätzlich
nicht gibt. Sobald belegte Daten vorliegen, können sie ergänzt werden.

## Reproduzierbarer Import

Die HTML-Seite lokal speichern und mit Python 3.8 oder neuer importieren:

```powershell
Invoke-WebRequest 'https://anandamarga.eu/fastingChart.php/1000' -OutFile artifacts/berlin-sector-current.html
python tools/import_am_reference.py --html artifacts/berlin-sector-current.html --year 2026 --retrieved-on 2026-10-13 --output data/reference/ananda-marga/berlin-sector-2026.json
```

Das Skript prüft die Jahresüberschrift, das Datumsformat, die Vollständigkeit
(24 oder 25 Ekadashii-Daten) und doppelte Daten **vor** dem Schreiben. Ein Abruf
des 2026-Kalenders kann so nicht versehentlich zu einer Referenz für 2025 oder
2027 umbenannt werden. Für spätere Jahre muss tatsächlich eine offizielle Quelle
für das betreffende Jahr vorliegen; Quelle und Abrufdatum sind mitzuführen.

Dateien `berlin-sector-*.json` werden als Ressourcen in Core eingebettet und
sind auch nach Build/Publish ohne Repo-Pfad und ohne Netzwerk nutzbar.
`CalendarReferenceReader` validiert Metadaten, HTTPS-Quelle, Location, Tradition,
Jahr und eindeutige Jahresdaten. Leere Referenzen sind nicht zulässig: fehlende
Daten werden als fehlende Referenz behandelt, nicht als Kalender mit null Tagen.

Der Vergleich ist nur für passende Koordinaten, IANA-Zeitzone, Jahr und Tradition
verfügbar. Für andere Orte/Jahre zeigt die UI berechnete Tage mit
**KEINE REFERENZ** und keine prozentuale Trefferquote.
