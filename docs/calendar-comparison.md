# Jahresvergleich mit offiziellen Kalendern

Umsetzung von [Issue #6](https://github.com/Schelawski/ekadashi-engine/issues/6).

## Bedienung

1. **Berlin**, **2026** und **Ananda Marga** auswählen.
2. **Berechnen** startet die vorhandene AM-Regelstrategie im Hintergrund.
3. Die Tabelle zeigt den berechneten lokalen Fastentag, das veröffentlichte
   Datum, Status und Differenz. `MATCH` ist grün, `MISMATCH` rot hinterlegt.
   Der Textstatus bleibt auch ohne Farberkennung eindeutig.
4. **Nur Referenzabweichungen** blendet Treffer aus. Die Statistik bezieht sich weiter
   auf das gesamte Jahr.
5. Eine Zeile öffnet die vollständige Diagnose mit Sonnenaufgang und Winkeln für
   beide Tage, Tithi-Grenzen lokal/UTC und angewandter Regel. **Schließen** führt
   zurück zur Tabelle. Siehe [Diagnose und Export](diagnostics-and-export.md).

Quellen-URL, Abrufdatum und Geltungsbereich stehen im Fenster. Bei Änderung von
Ort, Jahr oder Modus werden alte Vergleichsdaten und Details gelöscht. Abbruch
und Fehler übernehmen kein Teilergebnis; anschließend kann erneut gerechnet werden.

Der technische Modus mit Sonnenaufgangs-Tithis bleibt über die Modusauswahl
zugänglich. AM-Berechnung und Referenzvergleich verändern die Strategie nicht.

## Zuordnung und Statistik

`CalendarComparisonService` arbeitet unabhängig vom UI auf `TraditionYearResult`
und einer optionalen `CalendarReference`. Die Referenz muss zu Jahr, Tradition,
Koordinaten und IANA-Zeitzone der Berechnung passen.

Zunächst werden gleiche Kalenderdaten als `MATCH` reserviert. Übrige Referenztage
werden einem noch freien Rechendatum in höchstens zwei Tagen Abstand zugeordnet;
das nächste Datum gewinnt, bei gleicher Entfernung das frühere. Diese Zuordnung
ist ausschließlich eine Darstellungshilfe für Abweichungen, keine Fastenregel.
Fehlende Tage verschieben so nicht einfach alle nachfolgenden Zeilen.

| Status | Bedeutung |
| --- | --- |
| MATCH | Berechnetes und offizielles Datum stimmen exakt überein |
| MISMATCH | Zugeordnetes Rechendatum liegt ein oder zwei Tage daneben |
| FEHLT BERECHNET | Ein Referenzdatum hat kein zugehöriges Rechendatum |
| ZUSÄTZLICH BERECHNET | Ein Rechendatum hat kein zugehöriges Referenzdatum |
| KEINE REFERENZ | Für Ort, Jahr und Tradition fehlen offizielle Daten |

- **Referenztage:** Anzahl offizieller Ekadashi-Daten.
- **Matches:** Anzahl exakter Datumsübereinstimmungen.
- **Mismatches:** Referenztage minus Matches; umfasst auch fehlende Rechentage.
- **Zusätzlich berechnet:** Rechentage ohne zugehöriges offizielles Datum,
  separat ausgewiesen.
- **Trefferquote:** `100 × Matches / Referenztage`. Zusätzliche Rechentage sind
  deshalb separat zu beurteilen; eine hohe Quote allein genügt nicht zur Validierung.

Ohne Referenz sind diese Statistikwerte nicht verfügbar. Es wird weder 0 % noch
100 % suggeriert. Ungeklärte Regelentscheidungen erzeugen zusätzlich einen Hinweis
auf eine unvollständige berechnete Jahresliste.

## Aktueller Datenstand und Ergebnis

Für Berlin 2026: **24 Referenztage, 12 Matches, 12 Mismatches, keine zusätzlichen
Rechentage, 50 % Trefferquote**. Die Regel bleibt fachlich nicht validiert.
Die Abweichungen und verwendete Methode beschreibt
[Ananda-Marga-Regel](ananda-marga-rule.md).

Für Berlin Sector 2025 und 2027 wurden bislang keine belastbaren Quellen gefunden.
Die Jahre sind berechenbar; die UI zeigt dafür **KEINE REFERENZ**. Sie wurden
weder ausgerechnet noch aus Kalendern anderer Sektoren als offiziell ergänzt.
Recherche, JSON-Format und Import sind im
[Referenzverzeichnis](../data/reference/ananda-marga/README.md) dokumentiert.

## Prüfung

Tests prüfen Ressourcennutzung ohne Repo-Pfad, ungültige Referenzdaten,
Jahres-/Ortszuordnung, Datumsverschiebungen, fehlende/zusätzliche Tage,
Schutz exakter Treffer, Statistik und fehlende Referenzen. ViewModel-Tests
prüfen Fehler, Retry, Abbruch, Modusänderung und unvollständige Ergebnisse.
Der Avalonia-Test bedient die echten Auswahlfelder, berechnet Berlin 2026,
prüft rote/grüne Statusanzeigen, Filter und Diagnose sowie den Fall 2027
ohne Referenz. Die Ansicht wurde zusätzlich gerendert und visuell kontrolliert.
