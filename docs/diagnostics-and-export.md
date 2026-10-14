# Diagnose und Export

Umsetzung von [Issue #7](https://github.com/Schelawski/ekadashi-engine/issues/7).

## Grenzfall untersuchen

Im Modus **Ananda Marga** eine Vergleichszeile auswählen.
Die vollständige Diagnose ersetzt vorübergehend die Tabelle; **Schließen** führt
zurück. Der Diagnosebereich ist scrollbar und sein Text kann kopiert werden.

Er zeigt für den berechneten und offiziellen Tag sowie weitere beteiligte Tage:

- Location mit Koordinaten und IANA-Zeitzone, lokales Datum;
- Sonnenaufgang lokal mit Offset und in UTC, Status bei fehlendem Sonnenaufgang;
- Tithi am Sonnenaufgang, Sonnenlänge, Mondlänge und Winkeldifferenz;
- Ekadashi-Beginn und -Ende lokal und in UTC;
- berechnetes/offizielles Fastendatum, Regelversion und Regelquelle;
- geprüfte Überschneidungen, Entscheidungsstatus und Begründung.

Bei fehlendem Rechendatum werden auch zugehörige ungeklärte Tithi-Intervalle
angezeigt. Bei Jahreswechseln werden notwendige Nachbartage zusätzlich berechnet.
Tage außerhalb des angefragten Jahres erhalten keine erfundene Aussage über
Fasten oder offizielle Referenzen dieses Nachbarjahres.

`DayAstronomyCalculator` liefert die gemeinsamen Tageswerte für Jahresberechnung
und Nachbartage. `CalculationSnapshotFactory` führt die Daten zusammen, ohne die
AM-Regel zu verändern. UI und Export verwenden denselben vollständigen Snapshot.

## Export aus der App

Nach einer erfolgreichen Berechnung stehen unten die Exportaktionen bereit:

| Aktion | Inhalt |
| --- | --- |
| JSON exportieren | Alle Tageswerte, Regelentscheidungen und gegebenenfalls Referenz, Statistik und Vergleichsdiagnosen |
| Vergleich als CSV | Vergleich mit astronomischen Diagnosewerten; nur im AM-Modus verfügbar |
| Testvektoren exportieren | Tagesvektoren und vorbereitete Regelfälle für eine spätere Dart-Implementierung |

Die Aktionen öffnen den systemeigenen Speicherdialog. Abbruch schreibt keine Datei.
Ein vorhandenes Ziel wird mit der Überschreibabfrage des Speicherdialogs behandelt.
Nach Auswahl eines vorhandenen Ziels wird dessen Inhalt beim Schreiben ersetzt.
Datei- oder Schreibfehler werden im Status angezeigt und geloggt; ein erneuter
Export bleibt möglich. Während eines Exports sind weitere Exporte und eine neue
Berechnung gesperrt.

**Der Tabellenfilter beeinflusst den Export nicht.** Auch bei „Nur Referenzabweichungen“
enthält JSON alle 24 Vergleichsfälle und alle 365 Tageswerte für Berlin 2026.
Der technische Modus exportiert seine vollständige Jahresliste. Bei geänderten
Eingaben, Fehlern oder Abbruch einer Berechnung werden alte Snapshots entfernt;
es kann kein veraltetes Ergebnis exportiert werden.

Der native Dateidialog folgt der
[Avalonia StorageProvider API](https://docs.avaloniaui.net/docs/services/file-dialogs).
Die Headless-Tests prüfen Ansicht und Exportablauf mit kontrollierten Speicherzielen;
sie öffnen keinen echten Betriebssystemdialog.

## Struktur und Konventionen, Schema-Version 1

Beide JSON-Formate tragen `schemaVersion: 1` und `kind: "analysis"` bzw.
`kind: "testVectors"`. Inkompatible Formatänderungen benötigen eine neue Version.
Die Verträge stehen in `EkadashiEngine.Core/Export/ExportModels.cs`.

Gemeinsame Metadaten sind Location, Jahr, Regel mit Version/Quelle/Validierungsstatus,
Astronomieanbieter mit Version und Ephemeris, verwendete TZDB-Version und
Rechenkonventionen. Eine vorhandene offizielle Referenz enthält Quelle, Abrufdatum,
Geltungsbereich und Daten. Kein Export erhält einen laufzeitabhängigen
Erzeugungszeitpunkt, Rechnerpfad oder zufällige Kennung.

| Feld im Tagesdatensatz | Bedeutung |
| --- | --- |
| `date` | Lokaler Kalendertag `yyyy-MM-dd` |
| `sunriseUtc`, `sunriseLocal` | ISO-8601-Zeitpunkte; lokal mit dem am Zeitpunkt geltenden Offset |
| `sunriseStatus`, `sunriseDefinition` | Verfügbarkeit und Sonnenaufgangskonvention |
| `sunLongitude`, `moonLongitude` | Scheinbare geozentrische tropische Längen der Ekliptik des Datums, Grad `[0,360)` |
| `angularDifference` | Normalisiertes Mondlänge minus Sonnenlänge in Grad |
| `tithi` | Ganzzahl 1–30 am Sonnenaufgang |
| `isEkadashiCandidate` | Technisches Sonnenaufgangskriterium; bei fehlenden Daten `null` |
| `fasting` | Ergebnis der angewandten Traditionsregel; im technischen Modus, außerhalb des Jahres oder bei ungeklärten relevanten Daten `null` |
| `officialFasting` | Zugehörigkeit zur offiziellen Referenzliste; ohne passende Referenz `null` |

Die Spracheinstellungen verändern Zahlen und Datumsformate nicht. Fehlende Werte
werden als JSON-`null` ausgegeben, nicht als Nullwinkel oder `false`. Ein
Fastentag kann `fasting: true` und zugleich `isEkadashiCandidate: false` haben,
beispielsweise Berlin am 13.01.2026. Die Regel beurteilt ein ganzes Tithi-Intervall.

Das Analyseformat enthält `days`, sämtliche `decisions` einschließlich ungeklärter
Entscheidungen und `comparisons`. Jeder Vergleich enthält beide Fastendaten,
Status, beteiligte Tageswerte und zugehörige Regelentscheidungen. Nicht verfügbare
Vergleichsstatistiken bleiben `null`.

CSV verwendet UTF-8 mit BOM, Komma als Trennzeichen, CRLF-Zeilenenden und
invariante Dezimalpunkte. Alle Felder sind in Anführungszeichen eingeschlossen;
eingebettete Anführungszeichen werden verdoppelt. Text mit führenden
Tabellenkalkulations-Formelzeichen wird durch ein Apostroph als Text geschützt;
JSON bewahrt den Originaltext.

**CSV-Granularität:** eine Zeile je Vergleichsfall × Diagnose-Kalendertag ×
zugehörigem Tithi-Intervall; ohne Intervall bleiben die betreffenden Felder leer.
Ein Mismatch kann daher mehrere CSV-Zeilen besitzen. Die Anzahl der CSV-Zeilen ist
keine Match-/Mismatch-Statistik. Die Spalten enthalten alle oben genannten
Diagnosewerte, Regel/Regelstatus, Quellen, Abrufdatum, Backend und Schema-Version.

## Portable Testvektoren

`vectors` enthält vorberechnete Tageswerte und erwartete Wahrheitswerte.
`ruleCases` enthält je Ekadashi Tithi-Nummer, UTC-Grenzen, vorbereitete
Sonnenaufgangsfenster samt Überschneidungsdauer und erwartetes Fastendatum/Status.
Damit lässt sich die Traditionslogik ohne eine astronomische Bibliothek portieren.
Jeder Regelfall wird einzeln ausgewertet, die Jahresfilterung erfolgt nach der
Auswahl wie in der Engine. Eingabeintervalle sind halboffen `[Start, Ende)`;
Zeitdauern werden zwischen UTC-Zeitpunkten berechnet.

Die angegebenen Vergleichstoleranzen sind 0,00001° für Winkel, zwei Sekunden für
Sonnenaufgänge und eine Sekunde für Tithi-Grenzen. Das sind Portierungs- und
Regressionsvergleichstoleranzen, keine physikalische Genauigkeitszusage.
Datums-/Tithi-/Status- und Fastenentscheidungen werden exakt verglichen.
Die traditionelle Gleichstandsbehandlung bleibt die dokumentierte technische
Ein-Sekunden-Toleranz aus `am-max-overlap-v1`.

Zeitpunktstrings sind ISO 8601 mit bis zu sieben Nachkommastellen. Eine
Dart-Implementierung soll Zeitpunkte parsen und unter der vorgesehenen Toleranz
vergleichen, nicht Strings oder eine bestimmte Zahl von Nachkommastellen.
Ein UTC-Zeitpunkt und seine lokale Darstellung müssen denselben Instant ergeben.
Lokale Daten dürfen nicht aus der UTC-Datumskomponente abgeleitet werden.

Die fünf versionierten Datensätze unter `data/test-vectors` enthalten 20 Tageswerte
und 55 Regelfälle. Ihre Wiederholung in den Tests prüft astronomische Werte,
beide Seiten der Tithi-Grenzen und die reine Strategie. Sie sichern das aktuelle
Engine-Verhalten; sie erklären die AM-Methode nicht zu einem validierten
offiziellen Kalender. Weitere Hinweise und Erzeugungsbefehle stehen im
[Testvektor-Verzeichnis](../data/test-vectors/README.md).

## Kommandozeile

Das UI-unabhängige Werkzeug verwendet dieselben Core-Dienste und Exporte:

```powershell
dotnet run --project tools/EkadashiEngine.Export -- --year 2026 --location Berlin --mode am --format json --output artifacts/exports/berlin-2026.json
dotnet run --project tools/EkadashiEngine.Export -- --year 2026 --location Berlin --mode am --format csv --output artifacts/exports/berlin-2026.csv
dotnet run --project tools/EkadashiEngine.Export -- --year 2026 --location Berlin --mode am --format test-vectors --output artifacts/exports/berlin-2026-vectors.json
```

`--mode technical` exportiert technische Jahresdaten ohne Fastenbehauptung.
`--dates yyyy-MM-dd,yyyy-MM-dd` begrenzt ausschließlich die Tagesvektoren eines
Testvektor-Exports; die Jahresregelfälle bleiben vollständig. Unbekannte Parameter
und ungültige Werte führen zu Exitcode 2. `--help` beschreibt eigene Locations.
Die CLI ersetzt die ausdrücklich angegebene Zieldatei; automatisch erzeugte
Beispiele gehören nach `artifacts/`, versionierte Vektoren nach `data/test-vectors`.

## Vaishnava-Erweiterung (Issue #9)

CLI-Modus `--mode vaishnava` und UI-Auswahl **Vaishnava / ISKCON** verwenden
dieselben Exportwege. JSON-Entscheidungen und Testfälle enthalten zusätzlich
`vaishnavaDays`: Sonnenaufgang, Arunodaya, Sonnenuntergang in UTC, deren Tithis
und Lahiri-Nakshatra (0–26). Das Feld ist eine additive Schema-1-Erweiterung und
bei alten AM-Vektoren nicht vorhanden. `missingRuleData` bezeichnet einen
ungeklärten Vaishnava-Fall. CSV ergänzt sieben entsprechende Beobachtungsspalten
(Arunodaya UTC/lokal/Tithi, Sonnenuntergang UTC/lokal/Tithi, Nakshatra).
Dateivorschläge enthalten die Tradition, damit AM- und Vaishnava-Exporte
unterscheidbar sind. Die zusätzlichen Vergleichsspalten der Oberfläche sind
kein zweiter Kalender im Export: beide Traditionen jeweils separat exportieren.
Siehe [Dart-Spezifikation](flutter-porting-spec.md).
