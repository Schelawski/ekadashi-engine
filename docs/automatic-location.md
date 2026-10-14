# Automatic: fachliche Spezifikation für Marga Smriti

Status: Integrationsspezifikation zu [Issue #8](https://github.com/Schelawski/ekadashi-engine/issues/8)
und [Konzept #1](https://github.com/Schelawski/ekadashi-engine/issues/1).
Die folgende Produktlogik ist für Flutter/Dart vorgesehen. Der Desktop-Prototyp
implementiert manuelle Standorte und den Ortsvergleich; er fragt keine
Geräteposition ab und führt keinen Standortcache.

## Auswahl und Verantwortlichkeiten

```text
Ekadashi location

● Automatic
○ Home location
○ Select city...
```

| Option | Quelle des effektiven Standorts | Verhalten auf Reisen |
| --- | --- | --- |
| Automatic | Aktuell akzeptierte Gerätekoordinaten mit dazu aufgelöster IANA-Zeitzone | Bei einem akzeptierten Ortswechsel neu auswerten |
| Home location | Explizit gespeicherte Heimkoordinaten und IANA-Zeitzone | Bleibt am Heimatort |
| Select city... | Explizit ausgewählter Stadtpunkt mit Koordinaten und IANA-Zeitzone | Bleibt am gewählten Stadtpunkt |

Die gewählte Tradition bleibt eine separate Nutzereinstellung. Weder Land,
administrative Region noch ein Wechsel des Standortmodus wählen automatisch
eine andere Tradition. Alle drei Modi liefern denselben Core-Eingabetyp:

```text
Location(DisplayName, Country, Latitude, Longitude, TimeZoneId)
    + lokales Jahr + Tradition
    → lokale fastingDate-Werte und Diagnose-/Ungeklärt-Status
```

Ein Automatic-Standort verwendet die tatsächlichen akzeptierten Koordinaten,
keinen stillschweigend ersetzten Stadtmittelpunkt. `DisplayName` und `Country`
sind Beschreibungen. Ist kein Länderlabel verfügbar, darf ein sichtbarer
Platzhalter wie „Unbekannt“ verwendet werden; dies beeinflusst die Rechnung nicht.

## Auflösung einer Automatic-Position

1. Beim bewussten Aktivieren von Automatic die erforderliche Vordergrund-
   Standortfreigabe über die mobile Plattform anfordern.
2. Eine Positionsaufnahme mit Breite, Länge, Erfassungszeit in UTC und
   Genauigkeitsangabe anfordern. Dies erfordert keine dauerhafte GPS-Verfolgung.
3. Endliche Koordinaten und ihre Wertebereiche prüfen. Erfassungszeit,
   Genauigkeit und Herkunft bleiben Metadaten der mobilen Ortsschicht.
4. Die passende IANA-Zeitzone für **diese Koordinaten** auflösen. Der Adapter
   benötigt dafür einen geeigneten geografischen Zeitzonendienst bzw. eine
   Grenzdatenbank mit dokumentierter Version. Auswahl und plattformspezifische
   Anbindung dieses Dienstes sind spätere Integrationsarbeit.
5. Vollständige Location, Tradition und Jahr an die Engine übergeben. Die Engine
   erhält weder GPS-Handles noch mobile Berechtigungen oder Reminder-Zeiten.
6. Das Resultat mit dem effektiven Ort kennzeichnen. Ortserfassung, Zeitzonen-
   auflösung oder Neuberechnung dürfen ein altes Ergebnis nicht als neues ausgeben.

Ein aktueller UTC-Offset genügt nicht, weil er spätere Sommerzeitwechsel nicht
beschreibt. Die Betriebssystem-Zeitzone darf als Hinweis dienen, ist aber kein
geografischer Nachweis für die Geräteposition. Insbesondere dürfen manuell
verstellte Systemzeitzonen oder das Land keine Koordinaten ersetzen.

An Zeitzonengrenzen und bei groben Positionsangaben kann die richtige Zone
mehrdeutig sein. In diesem Fall gezielt eine bessere Aufnahme bzw. eine explizite
manuelle Orts-/Zeitzonenwahl verlangen; keine Genauigkeit erfinden. Die Desktop-
Möglichkeit, eine beliebige gültige ID einzugeben, ersetzt diesen Auflösungsschritt
für Automatic nicht.

## Aktualisierung und relevanter Ortswechsel

Vorgesehene Auslöser sind das Aktivieren von Automatic, Rückkehr in die App,
ein neuer lokaler Tag, ein explizites Aktualisieren sowie ein verfügbarer
Hinweis der Plattform auf eine relevante Ortsänderung. Ein Hinweis auf eine
geänderte Systemzeitzone stößt eine Standortprüfung an und überschreibt nicht
allein die Location. Im Hintergrund ist keine permanente GPS-Abfrage vorgesehen.

Die mobile Ortsschicht unterscheidet Sensorrauschen von einer akzeptierten neuen
Position anhand von Erfassungszeit und Unsicherheitsbereich. Für jede akzeptierte
Position gehen die vollständigen Koordinaten in die Berechnung ein. Eine Änderung
der aufgelösten IANA-Zeitzone erfordert ebenfalls eine neue Auswertung.

Eine feste Kilometergrenze kann nicht garantieren, dass der Fastentag gleich
bleibt: Schon ein kleiner Sonnenaufgangsunterschied kann nahe einer Tithi-Grenze
relevant sein. Deshalb gibt es keinen fachlichen „innerhalb von X km immer gleiche
Fastentage“-Schwellwert und keinen Cache allein nach Zeitzone/Stadt/Land.

Vor Produktfreigabe festzulegen und am Gerät zu prüfen sind die zulässige
Positionsalterung, eine angemessene Genauigkeit, die Trigger-Drosselung sowie der
Umgang mit der erfassten Unsicherheit. Diese Betriebsparameter sind keine
astronomischen Regeln; das Referenzprojekt erfindet dafür keine bereits
validierten Zahlenwerte. Bei unzureichender Position wird der Status explizit
degradiert und manuelle Auswahl angeboten.

## Zustände und Fallback

| Zustand | Anzeige und nächste Aktion |
| --- | --- |
| Noch keine Position / wird aufgelöst | „Standort wird ermittelt“; vorhandene Resultate behalten ihre bisherige Ortskennzeichnung |
| Position und Zeitzone gültig | Effektiver Ort sichtbar; berechnen oder exakt passenden Cache verwenden |
| Permission verweigert | Home location oder Select city... ausdrücklich anbieten |
| Position nicht verfügbar / zu alt / ungenau | Aktualisieren anbieten; letzten bekannten Ort mit Zeitbezug markieren, nicht als aktuelle Position ausgeben |
| Zeitzone nicht eindeutig auflösbar | Bessere Position bzw. manuelle Ortswahl anbieten |
| Keine auswertbare Morgenentscheidung | „Ungeklärt“ aus der Engine weiterreichen; keine Ersatz-Sonnenaufgangszeit erfinden |

Ein gespeicherter Heimat- oder Stadtort kann nach ausdrücklicher Nutzerwahl als
Fallback verwendet werden. Die App darf nicht unbemerkt von Automatic zu Home
wechseln und die Heimatergebnisse als aktuell ortsbezogen anzeigen. Eine alte
Location darf nur unter einer dokumentierten Aktualitäts-/Qualitätsregel wieder
als effektiver Automatic-Standort akzeptiert werden.

Ein laufender Rechenauftrag ist an Standort, Tradition und Jahr gebunden. Ändern
sich diese während des Laufs, wird er abgebrochen bzw. sein Resultat verworfen.
Zwischenergebnisse dürfen eine aktuelle Liste nicht teilweise ersetzen.

## Cache-Vertrag

Der in Core eingeführte `LocationKey` enthält **nur den Standortanteil** einer
späteren Cache-Identität: Latitude, Longitude und TimeZoneId. Ein vollständiger
Berechnungsschlüssel muss zusätzlich mindestens enthalten:

- lokales Jahr bzw. explizite lokale Datumsperiode;
- Tradition und Regelversion;
- Astronomiemodell/Backend und Version, Sonnenaufgangskonvention und alle dazu
  verwendeten Parameter;
- Version der IANA-/TZDB-Zeitregeln und Format-/Engine-Spezifikation.

Geografische Zeitzonen-Grenzdaten werden separat versioniert, damit eine frühere
Koordinate→Zeitzone-Auflösung nach Aktualisierung überprüft werden kann.
Schlüsselzahlen werden kulturunabhängig und ohne Anzeige-Rundung gespeichert.
Für die Darstellung auf fünf Nachkommastellen gerundete Koordinaten dürfen nicht
unbemerkt zu den eigentlichen Berechnungskoordinaten werden.

Der reine Wechsel zwischen Automatic/Home/City darf dasselbe Rechenergebnis
wiederverwenden, falls alle fachlichen Schlüsselwerte identisch sind. Herkunft,
Positionszeit und Anzeige-Label werden separat geführt. Ein anderer Standort in
derselben Zone ist dagegen ein anderer Schlüssel.

Ein Cache speichert auch ungeklärte Entscheidungen, statt sie in `false`
umzuwandeln. Versionenwechsel invalidieren betroffene Einträge. Ein Jahreswechsel
braucht die neue lokale Periode einschließlich der Nachbartage für Regelentscheidungen.
Standortdaten sind ein aktueller Nutzungskontext, kein Anlass, eine Bewegungshistorie
zu sammeln oder standardmäßig an einen Server zu übertragen.

## Fastentag und mobile Erinnerungen

Die Engine liefert ein lokales Kalenderdatum. Nutzerzeiten wie 04:30 oder 05:00
sind keine Recheneingaben und ändern `fastingDate` nicht. „Heute Ekadashi“ bezieht
sich auf das aktuelle lokale Datum des effektiven Ekadashi-Orts.

Die mobile UX ordnet Erinnerungen getrennt zu:

- Day −1: konfigurierbarer Vorabend, im Konzept als Standard etwa 19:00.
- Day 0: optional früh morgens, z. B. 04:30 oder eine gewählte Nutzerzeit.
- Day +1: Fastenbrechen primär informativ; kein zusätzlicher Push als Standard,
  eine etwaige Erinnerung nur nach ausdrücklichem Opt-in.

Nach einem akzeptierten Orts- oder Zeitzonenwechsel müssen geplante Erinnerungen
zur neuen Location/Periode neu zugeordnet und gegebenenfalls ersetzt werden.
Das alleinige Addieren von 24 Stunden zu einem UTC-Zeitpunkt ersetzt weder einen
lokalen Folgetag noch eine kalendergerechte Erinnerung bei Sommerzeitwechseln.
Ohne Standortaufnahme während einer Reise kann die App den Ortswechsel erst beim
nächsten verfügbaren Auslöser erkennen; diese Grenze gehört zur mobilen UX.

## Abnahmeszenarien für die spätere Flutter-Integration

1. Berlin→München: gleiche Zeitzone, neue Koordinaten, neuer Cache-Schlüssel und
   eigener Sonnenaufgang. Ein unverändertes Fastendatum bleibt ein mögliches Resultat.
2. Berlin→Tokyo: neue Koordinaten und Zeitzone, lokale Jahres-/Tagesgrenze und
   Reminder werden mit dem neuen Ort beurteilt.
3. New York bzw. Sydney über den Sommerzeitwechsel: IANA-Zeitregeln bestimmen
   den Offset des betreffenden Tages, nicht der beim Caching aktuelle Offset.
4. Apia am übersprungenen Kalendertag und ein Polarort: `null`/Ungeklärt erhalten,
   keine erfundenen astronomischen Werte oder automatische Kalenderautorität.
5. Verweigerte Permission, alter Fix oder gescheiterte Zonenauflösung: sichtbarer
   Zustand, expliziter manueller Fallback, keine falsch beschriftete Ergebnisliste.
6. Schneller Standort-/Traditionswechsel während eines Rechenlaufs: nur das
   Ergebnis des zuletzt akzeptierten Eingabestands veröffentlichen.
7. Die versionierten .NET-Testvektoren nach [Schema v1](diagnostics-and-export.md)
   wiedergeben. Das portiert den aktuellen Algorithmus; die dokumentierten
   Abweichungen der AM-Methode von offiziellen Kalendern bleiben fachlich offen.
