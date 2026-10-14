# Abschlussreview zu Issue #9

Geprüft wurden Astronomieadapter, reine Traditionsregeln, Jahres-/Ortsvergleich,
Zeitzonen und Datumsgrenzen, Referenzzuordnung, Export, ViewModels, XAML-Ansichten,
Testvektoren und Übergabedokumentation. Der Core bleibt ohne Avalonia- oder
Swiss-Ephemeris-Abhängigkeit; die astronomische Bibliothek ist im Adapter gekapselt.

## Bereinigte Befunde

- Traditionen und Beschreibungstexte waren zwischen Hauptfenster und Ortsvergleich
  dupliziert. Beide verwenden jetzt dieselbe `TraditionOptions`-Quelle.
- Der neue Traditionsvergleich machte die Spalte „Gewählte Tradition“ redundant.
  Die Tabelle zeigt nun AM und Vaishnava genau einmal, dazu den Datumsabstand und
  die ausdrücklich bezeichnete Referenz der aktuellen Auswahl.
- Die eigentliche Vaishnava-Regelbegründung war hinter vielen astronomischen
  Kontextzeilen verborgen. Entscheidung und Arunodaya am Fastentag/Vortag stehen
  jetzt am Anfang der scrollbaren Details.
- Bei minimaler Fenstergröße verdrängten Eingaben und Hinweise fast die gesamte
  Tabelle. Kompaktere Abstände und ein begrenzter, scrollbarerer Eingabebereich
  halten die Ergebnistabelle sichtbar; die Mindesthöhe wird im UI-Test geprüft.
- Die vorgeschlagenen Exportdateinamen unterschieden Traditionen nicht.
  Die Tradition ist nun Bestandteil des Namens. CSV enthält auch die neuen
  Arunodaya-, Sonnenuntergangs- und Nakshatra-Beobachtungen.
- Bei ungültigem Jahr konnte der Ortsvergleich das vorherige Vergleichsjahr
  übernehmen. Aktion und Handler prüfen jetzt ein gültiges Jahr.
- Ein später Fehler beim Export einer alten Berechnung konnte den Status nach
  einer Eingabeänderung überschreiben. Er wird weiterhin protokolliert, verändert
  aber den Status einer inzwischen verworfenen Berechnung nicht mehr.

## Verifikation

Release-Build: keine Warnungen oder Fehler. Gesamtsuite: **206 Tests erfolgreich**
(165 Core/Astronomie, 41 Desktop). Keine Tests übersprungen.

Die Tests prüfen bestehende astronomische Referenzen, AM-Mismatches, beide
Traditionsstrategien, alle Mahadvadashi-Zweige und deren Priorität, vollständigen
GCal-Abgleich Berlin 2026, alle neun internationalen Orte, 1900/2100, Polarfälle,
Apias Datumswechsel, deterministische Exporte, Abbruch/Fehler/Wiederholung und
veraltete Ergebnisse nach Eingabeänderungen. Alle sieben portablen Datensätze
werden aus unveränderlichen Fixtures eingelesen und nachgerechnet.

Headless-UI-Tests laden die echten Avalonia-Ansichten und Bindings, wählen die
Traditionen, berechnen, filtern, öffnen Details und prüfen den Exportinhalt.
Die Hauptübersicht und Vaishnava-Details wurden zusätzlich als PNG gerendert und
visuell geprüft. Der Test deckt nicht jedes native Betriebssystem-Dateidialog-
Verhalten oder jede Bildschirm-/DPI-Konfiguration ab.

## Verbleibende fachliche Grenzen

- Vaishnava: Berlin 2026 trifft 24/24 externe Referenztage. Die neun Orte sind
  technisch geprüft; seltene Nakshatra-Mahadvadashi-Fälle haben synthetische
  Abnahmevektoren, noch keine breite unabhängige Kalenderbestätigung.
- AM: dokumentierte Methode bleibt wie vom Nutzer festgelegt. Berlin 2026 trifft
  12/24 offizielle AM-Daten; keine stillen Anpassungen an den Sektorkalender.
- `IsCalendarValidated` bedeutet globale Freigabe und bleibt false. Konkrete
  Vergleichsstatistiken sind separat sichtbar. Grenzlagen und fehlende Astronomie
  werden nicht als sicherer Fastentag ausgegeben.
- GPS, geografische Zeitzonensuche, Benachrichtigungen und Parana-Zeitfenster sind
  nicht Teil dieser Desktop-Referenz. Die spätere App-Integration ist spezifiziert;
  Day +1 bleibt eine Informationsanzeige ohne Standard-Push.

Es wurden keine Abhängigkeiten aktualisiert und keine kosmetischen Großumbauten
an der funktionierenden Astronomie vorgenommen.
