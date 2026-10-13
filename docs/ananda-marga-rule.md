# Ananda-Marga-Regelstrategie

Stand: 13.10.2026, [Issue #5](https://github.com/Schelawski/ekadashi-engine/issues/5).

## Verwendete Methode und Geltungsbereich

`AnandaMargaRule` implementiert die auf der offiziellen Website des
[New York Sector veröffentlichte Methode](https://ampsnys.org/fasting-calendar/):
Für jedes Ekadashi wird der Sonnenaufgangstag mit der größten zeitlichen
Überschneidung mit dem betreffenden Tithi gewählt. Ein solcher Tag reicht vom
lokalen Sonnenaufgang bis zum nächsten lokalen Sonnenaufgang. Ausgegeben wird
das lokale Kalenderdatum seines Beginns, also der Morgen des Fastentags.

Die Quelle nennt das Ananda Marga Diary bzw. Astroica mit Bezug auf Varanasi als
Lieferanten der Tithi-Grenzen. Diese Implementierung berechnet die Grenzen aus
geozentrischen Sonne-/Mondlängen mit dem vorhandenen Astronomieadapter. Das ist
eine ausdrücklich dokumentierte Ersetzung der Quelldaten, keine Zusicherung,
dass die Diary-Werte exakt reproduziert werden. Die Quelle legt weder die genaue
Sonnenaufgangsdefinition noch eine Regel für Gleichstände oder Polargebiete fest.
Hier wird der bereits dokumentierte astronomische Sonnenaufgang mit Refraktion
und oberem Sonnenrand verwendet. Die Koordinaten gehören zur ausgewählten Location.

Nach Rücksprache wird diese dokumentierte Methode trotz der unten aufgeführten
Abweichungen verwendet. Regelversion: `am-max-overlap-v1`. Das Ergebnis trägt
`IsCalendarValidated = false`; eine Übereinstimmung mit dem Berlin-Sector-Kalender
oder anderen offiziellen Kalendern wird nicht behauptet.

## Trennung von Astronomie und Tradition

1. `TithiIntervalCalculator` findet vollständige Tithi-Intervalle durch Abtastung
   in sechs Stunden Abstand und Bisektion der 12°-Grenzen bis auf 0,1 Sekunden
   Intervallbreite. Das ist eine numerische Suchgenauigkeit, keine Zusicherung der
   physikalischen Genauigkeit. Übergang 30 → 1 wird über den 360°-Sprung verfolgt.
2. `TraditionDataCalculator` bereitet Sonnenaufgangsintervalle und Tithis für eine
   Location und ein Jahr von 1900 bis 2100 vor. Gepufferte Nachbarjahresdaten
   verhindern abgeschnittene Entscheidungen am Jahreswechsel.
3. `ITraditionRule.Evaluate(PreparedTraditionYear)` arbeitet ausschließlich auf
   diesen vorbereiteten Daten. `AnandaMargaRule` benötigt weder einen
   Astronomiedienst noch UI oder SwissEphSharp. Weitere Traditionen können eigene
   Implementierungen derselben Schnittstelle erhalten.
4. `FastingCalendarCalculator` verbindet Vorbereitung und Strategie. Die Dienste
   sind im Desktop-DI registriert. Die technische UI und der Modus
   **Ananda Marga · Kalendervergleich** machen beide Berechnungsarten zugänglich;
   siehe [Kalendervergleich](calendar-comparison.md).

Beispiel für den Engine-Aufruf:

```csharp
var data = new TraditionDataCalculator(sunriseCalculator, tithiIntervalCalculator);
var engine = new FastingCalendarCalculator(data, new AnandaMargaRule());
var result = engine.Calculate(2026, berlin, cancellationToken);
IReadOnlyList<DateOnly> fastingDates = result.FastingDates;
```

`TraditionYearResult` enthält zusätzlich Regelversion, Quellen-URL, Location,
Jahr und Entscheidungen. Jede Entscheidung enthält Tithi-Grenzen in UTC, die
geprüften Tagesintervalle, deren Überschneidungsdauern, Status und Begründung.
Diese Daten erklären das gewählte Datum; sie sind keine Fastenbeginn-/Ende-UX.

## Randfälle

- Tithis 11 und 26 sind Ekadashi. Andere Tithis wählen keinen Fastentag.
- Intervalle sind halboffen `[Start, Ende)`. Zeitdauern werden in UTC verglichen,
  nicht mit einer pauschalen Tageslänge von 24 Stunden. Zeitzonenwechsel und
  Sommerzeit verändern nicht die Bedeutung des lokalen Datums.
- Kurze Tithis zwischen zwei Sonnenaufgängen gehen nicht verloren. Lange Tithis,
  die zwei Sonnenaufgänge einschließen, ergeben eine einzige Auswahl.
- Die größte Überschneidung wird vor dem Filtern auf das angefragte Jahr gewählt.
- Fehlt zur vollständigen Abdeckung eines Tithis ein Sonnenaufgang, lautet der
  Status `MissingSunriseCoverage`. Es wird kein Datum erfunden. Das gilt auch für
  übersprungene lokale Kalenderdaten.
- Bei einem Abstand der beiden größten Überschneidungen von höchstens einer
  Sekunde lautet der Status `AmbiguousOverlap`. Die Toleranz ist eine technische
  Behandlung numerischer Unsicherheit, keine tradierte AM-Gleichstandsregel.
- `HasUnresolvedDecisions` muss von Aufrufern beachtet werden: `FastingDates`
  allein ist bei fehlenden Sonnenaufgängen oder Gleichständen keine vollständige
  Jahresliste. `IsCalendarValidated` ist davon unabhängig.
- Ein CancellationToken wird während Vorbereitung, Grenzsuche und Regelauswertung
  geprüft. Abgebrochene Läufe liefern kein Teilergebnis.

## Vergleich mit dem offiziellen Kalender 2026

Die 24 Ekadashii-Daten des
[Berlin Sector](https://anandamarga.eu/fastingChart.php/1000) sind mit Quellenangabe
und Abrufdatum in `data/reference/ananda-marga/berlin-sector-2026.json` erfasst.
Die Seite veröffentlicht einen Sektorkalender ohne konkrete Ortskoordinaten
oder Berechnungsmethode. Für den Vergleich wurde Berlin (52,52° N, 13,405° E,
Europe/Berlin) verwendet. Die Referenzdaten fließen niemals in die Regelauswahl ein.

Ergebnis: **24 berechnete Tage, 12 Übereinstimmungen, 12 Abweichungen, 50 %**.
Alle Abweichungen liegen einen Tag vor dem veröffentlichten Datum:

| Berechnet | Offiziell |
| --- | --- |
| 2026-01-13 | 2026-01-14 |
| 2026-01-28 | 2026-01-29 |
| 2026-02-12 | 2026-02-13 |
| 2026-04-26 | 2026-04-27 |
| 2026-05-12 | 2026-05-13 |
| 2026-06-24 | 2026-06-25 |
| 2026-08-08 | 2026-08-09 |
| 2026-09-06 | 2026-09-07 |
| 2026-09-21 | 2026-09-22 |
| 2026-10-21 | 2026-10-22 |
| 2026-12-03 | 2026-12-04 |
| 2026-12-19 | 2026-12-20 |

Beispiel Januar: Der berechnete Tithi 26 reicht vom 13.01.2026, 09:48:24,8 UTC
bis zum 14.01.2026, 12:23:14,7 UTC. Die Überschneidung beträgt für den
Sonnenaufgangstag 13.01. etwa 21 h 22 min und für den 14.01. etwa 5 h 12 min.
Damit folgt der 13.01. eindeutig aus der verwendeten Methode. Der veröffentlichte
14.01. lässt sich daraus nicht begründen. Eine pauschale Verschiebung um einen
Tag würde die zwölf bisherigen Übereinstimmungen zerstören.

Die Ursache der Abweichungen ist fachlich offen. Denkbare Unterschiede in
Quelldaten oder sektorbezogenen Konventionen sind nicht als Regeln implementiert.
Für spätere Präzisierungen werden belastbare Erläuterungen der Kalenderherausgeber
benötigt. Der Vergleich ist seit Issue #6 im UI sichtbar; für 2025/2027 fehlen
bislang belastbare Berlin-Sector-Daten. Grüne Regressionstests bedeuten korrekte Umsetzung dieser Methode,
keine erfolgreiche offizielle Kalender-Validierung.

## Reproduzieren

```powershell
dotnet test tests/EkadashiEngine.Astronomy.Tests --configuration Release --filter FullyQualifiedName~AnandaMargaCalendarTests.Berlin2026 --logger "console;verbosity=detailed"
```

Der Test gibt alle 24 Datumspaare aus und sichert die bekannten Abweichungen.
Weitere Tests prüfen die reine Regel mit synthetischen Daten, die Grenzsuche
an einer analytisch bekannten Mondbewegung, reale Grenzübergänge, alle neun
Orte des Katalogs, Schaltjahr, unterstützte Jahresgrenzen und einen Polarort.
