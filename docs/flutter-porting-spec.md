# Fachlicher Vertrag für die Flutter-/Dart-Portierung

Stand: 14.10.2026. Die C#-Engine ist eine Referenzimplementierung. Die mobile
Implementierung darf andere Bibliotheken und eine andere Architektur verwenden,
muss aber die versionierten fachlichen Erwartungen reproduzieren.

## Eingaben

| Input | Vertrag |
| --- | --- |
| year | Ganzzahl 1900–2100, lokales Kalenderjahr |
| latitude / longitude | Endliche Gradwerte −90…90 / −180…180; Osten positiv |
| timeZoneId | Gültige IANA-ID aus versionierter TZDB, z. B. Europe/Berlin |
| tradition | AnandaMarga, GaudiyaVaishnava oder technischer Diagnosemodus |
| displayName / country | Beschriftung, kein astronomischer Input |
| versions | Regelversion, Ephemeridenmodell, TZDB und Konventionsversion |

Koordinaten und IANA-Zone sind beide erforderlich. Land, Geräte-UTC-Offset und
Sprache bestimmen keine Zeitzone. Die Auswahl Automatic/Home/Select city liefert
dasselbe Location-Objekt; ihr genauer Vertrag steht in [Automatic](automatic-location.md).
Berechnungs- und Cache-Identität verwenden volle Koordinatenpräzision, IANA-Zone,
Zeitraum und alle Modellversionen, keine gerundeten UI-Werte oder Stadtnamen.

## Astronomischer Ablauf

1. Für jedes lokale Datum den ersten gültigen Tagesbeginn in seiner IANA-Zone
   bestimmen; bei überlappendem Mitternachtszeitpunkt den früheren Instant,
   bei Mitternachtslücke den ersten gültigen Instant nehmen. Ein ganz
   übersprungenes Datum bleibt `SkippedLocalDate`.
2. Sonnenaufgang innerhalb dieses lokalen Datums berechnen. Die Suche darf
   keinen späteren Sonnenaufgang einem früheren Datum zuordnen. Oberer Sonnenrand,
   Refraktion, Meereshöhe, Druck 1013,25 hPa und Temperatur 15 °C. Kein Sonnenaufgang
   → `NoSunriseOnLocalDate`, alle davon abhängigen Werte `null`.
3. Sonne und Mond: geozentrische, scheinbare tropische ekliptikale Längen des Datums.
   Referenz: Swiss Ephemeris 2.10.03 / SwissEphSharp 2.10.3.1, explizit Moshier.
   Die Bibliothek übernimmt UTC → UT1/TT inklusive ihrer ΔT-/Schaltsekundenmodelle.
   UTC nicht versehentlich als TT interpretieren. Keine topozentrischen Längen
   und keine siderische Korrektur für Tithi.
4. `phase = normalize(moonLongitude - sunLongitude)` in `[0,360)`.
   `tithi = floor(phase / 12) + 1`. Modulo muss auch bei negativen Zahlen korrekt
   normalisieren und darf durch Rundung nie 360 liefern. 1–15 zunehmend, 16–30
   abnehmend; Ekadashi 11/26. Der technische Modus markiert nur diese Tithis am
   Sonnenaufgang, ohne traditionelle Entscheidung.
5. Alle vollständigen Tithi-Intervalle bestimmen. Referenz sucht in 6-Stunden-
   Schritten nach den nächsten 12°-Grenzen und halbiert bis unter 100 ms.
   Den 360°-Übergang entrollen. Intervalle sind halboffen `[start,end)`;
   eine Grenze gehört zum neuen Tithi. Dauer und Überlappungen immer in UTC.
6. Für AM vier lokale Randtage und zwei UTC-Randtage vorbereiten. Für Vaishnava
   zwölf lokale Randtage und vierzehn UTC-Randtage verwenden, damit insbesondere
   Paksha-vardhini über einen Jahreswechsel hinweg auswertbar bleibt.
7. Für Vaishnava zusätzlich Arunodaya = Sonnenaufgang minus 96 **verstrichene**
   Minuten, Sonnenuntergang, Tithi an diesen Instants und Lahiri-Nakshatra am
   Sonnenaufgang berechnen. Nakshatra-Index `floor(normalize(moon - ayanamsha) /
   (360/27))` liegt in 0–26. Swiss-Lahiri verwenden; keine Sternzeichenindices.

Andere Suchverfahren sind zulässig, wenn sie denselben Vertrag und die
Abnahmetoleranzen einhalten. Astronomische Fehler müssen als Fehler oder
ausdrücklich unbekannte Daten sichtbar bleiben, nie als Nullgrad oder 00:00 Uhr.

## Reine TraditionRule

Eine Strategie erhält vorbereitete Sonnen- und Tithi-Daten. Sie liest weder
Gerätezeit noch GPS, UI, Netzwerk, Datenbank oder Reminder-Einstellungen.

**Ananda Marga**, Version `am-max-overlap-v1`: Für jedes Ekadashi-Intervall den
Sonnenaufgangstag mit größter vollständiger UTC-Überlappung wählen. Tagesfenster
sind `[sunrise(D), sunrise(D+1))`. Die Fenster müssen das ganze Tithi abdecken;
sonst `MissingSunriseCoverage`. Differenz der beiden größten Überlappungen ≤1 s
→ `AmbiguousOverlap`. Keine undokumentierte Gleichstandsentscheidung. Details:
[AM-Regel](ananda-marga-rule.md).

**Gaudiya Vaishnava**, Version `vaishnava-gcal-v1`: Die Reihenfolge der
[Vaishnava-Regeltabelle](vaishnava-rule.md#regelreihenfolge) ist normativ. Zuerst
Mahadvadashi auf Dvadashi markieren, danach reine Ekadashi beurteilen. Es genügt
nicht, generell „Tithi an Sonnenaufgang“ oder „bei Viddha einen Tag später“ zu
verwenden. Unmilani, Trisprsha und Kombination, Vyanjuli, Paksha-vardhini sowie
Jaya, Jayanti, Papa-nasini und Vijaya werden separat geprüft. Fehlender relevanter
Kontext oder nicht eindeutige Auswahl → `MissingRuleData` und unbekanntes Datum.

Für beide Strategien erfolgt die Auswahl **vor** dem Filter nach lokalem Jahr.
Ein Gewinner am 31.12. wird bei einer Januarabfrage nicht auf den 01.01. verschoben.

## Ergebnisse und Serialisierung

Ein Jahresergebnis enthält Location, Jahr, Tradition, Regelversion/-quelle,
sortierte eindeutige `fastingDates` und die Entscheidungen pro Ekadashi-Intervall.
Eine Entscheidung enthält Tithi-Nummer, UTC-Grenzen, `fastingDate` als reines
lokales Datum `yyyy-MM-dd` oder `null`, Status, verwendete Tagesfenster und eine
Begründung. Vaishnava enthält zusätzlich `vaishnavaDays` mit lokalen Daten,
Sonnenaufgang/Arunodaya/Sonnenuntergang in UTC, deren Tithis und Nakshatra.

`isFasting(localDate)` ist dreistufig: `true` bei ausgewähltem Fastentag, `null`
bei fehlendem Sonnenaufgang bzw. einer dieses Datum betreffenden ungeklärten
Entscheidung, ansonsten `false`. Außerhalb des Ergebnisjahres gilt `null`.
Bei der aktuellen konservativen Vaishnava-Regel kann der gesamte gespeicherte
Lookahead-Kontext einer ungeklärten Entscheidung betroffen sein.

`fastingDate` ist **kein UTC-Mitternachtszeitpunkt**. ISO-Instants enthalten dagegen
Offset/Z; alle Winkel und Sekunden sind JSON-Zahlen mit invariantem Dezimalpunkt.
`null` bedeutet unbekannt/nicht verfügbar, nie false oder 0. `officialFasting`
ist ohne passende externe Referenz ebenfalls `null`. JSON-Enum-Namen werden
camelCase geschrieben. Ein AM-Referenzkalender ist kein Vaishnava-Testorakel.

Export-Vertrag: [Diagnose und Export](diagnostics-and-export.md). Schema 1 wurde
additiv um optionale `vaishnavaDays` in Entscheidungen/Regeltests erweitert;
fehlendes Feld ist bei älteren AM-Dateien erlaubt. Die Regelversion steuert,
welche Inputs erforderlich sind. `MissingRuleData` ist ein zusätzlicher Status.
CSV führt ergänzend Arunodaya und Sonnenuntergang lokal/UTC, Tithis und Nakshatra.
Exporte enthalten stets das vollständige Ergebnis unabhängig vom UI-Filter und
tragen die Tradition im vorgeschlagenen Dateinamen.

## Abnahme durch Testvektoren

Alle sieben versionierten Dateien in [data/test-vectors](../data/test-vectors/README.md)
sind verbindlich: insgesamt **30 Tageswerte und 94 reine Regelfälle**.
Die Herkunft ist pro Datei bezeichnet; synthetische Grenzfälle dürfen nicht
gegen eine reale Ephemeride geprüft werden. Die Dateien dürfen nicht im Testlauf
aus den aktuellen Ergebnissen regeneriert werden.

Zwei getrennte Teststufen implementieren:

1. **Regeltests ohne Astronomie:** `ruleCases` einlesen, SolarDays/Tithi und bei
   Vaishnava `vaishnavaDays` an die Strategie geben; Datum einschließlich `null`
   und Status müssen exakt `expectedFastingDate`/`expectedStatus` entsprechen.
   So wird die Fachlogik unabhängig vom gewählten Dart-Astronomieadapter geprüft.
2. **Astronomie und Integration:** Reale Tageswerte aus Location/Jahr berechnen.
   Längenabweichung höchstens 0,00001°, Sonnenaufgang/-untergang höchstens 2 s,
   Tithi-Grenzen höchstens 1 s; UTC-Offset, lokales Datum, Tithi, Kandidat und
   Fastentag müssen exakt übereinstimmen. Vaishnava-Beobachtungen einschließlich
   Nakshatra und Arunodaya ebenfalls nachrechnen. Die tolerierte Zeitabweichung
   erlaubt niemals eine Abweichung um einen Fastentag.

Zusätzlich müssen Berlin 2026 alle 24 unabhängigen Vaishnava-Referenzdaten und die
bekannten AM-Referenzabweichungen reproduziert werden. Eine Änderung der TZDB,
Ephemeride oder Regelversion erfordert einen dokumentierten Vergleich der
Erwartungen. Der C#-Test `PortableTestVectorTests` zeigt den sprachneutralen Ablauf.

## Grenzfälle und Genauigkeit

Zu prüfen sind Jahreswechsel, beide Hemisphären und DST-Wechsel, kurze/doppelte
Tithis, ausgelassene Dvadashi, Viddha, alle Mahadvadashi-Zweige, fehlende
Sonnenaufgänge/-untergänge, übersprungene lokale Daten (Apia 30.12.2011), AM-
Gleichstände und fehlende offizielle Referenzjahre. Fehlende Daten nicht durch
einen Referenzort oder eine feste Uhrzeit ersetzen.

Die Rechentoleranzen sind Portierungsziele und keine Aussage über die
Genauigkeit eines real beobachteten Sonnenaufgangs. Gelände, Höhe, Atmosphäre,
astronomische Konventionen und sekundengenaue Grenzlagen können Unterschiede
bewirken. Globale fachliche Freigabe bleibt offen: Berlin 2026 Vaishnava 24/24,
AM 12/24; weitere Orte werden technisch geprüft, aber nicht pauschal als
kalender-validiert bezeichnet. Seltene Nakshatra-Fälle sind bislang synthetisch
abgedeckt. Parana-Zeiten werden nicht berechnet.

Vorgesehener App-Hinweis:

> Die Ekadashi-Daten werden nach bestem Wissen anhand astronomischer Daten und
> der gewählten Tradition berechnet. In seltenen Grenzfällen können lokale oder
> traditionelle Kalender abweichen.

## Integration in Marga Smriti und Reminder

Die fachliche Engine endet bei `fastingDate`. Die mobile App verwaltet separat
Notification-Berechtigungen, Nutzerzeiten, Betriebssystemplanung und Ortswechsel.
„Heute ist Ekadashi“ prüft das aktuelle lokale Datum am aktiven Standort; die
Aufstehzeit des Nutzers verändert den Fastentag nicht. Die bisherigen statischen
Berliner Day-0-JSON-Daten werden durch das Ergebnis für Standort/Tradition ersetzt.
Day −1 existiert bereits; Day +1 ist eine spätere Erweiterung.

| Lokaler Tag relativ zu fastingDate | Bedeutung | Vorgesehene Vorgabe |
| --- | --- | --- |
| Day −1 | Tomorrow is Ekadashi | 19:00 Uhr oder Nutzerzeit |
| Day 0 | Today is Ekadashi | Optional 04:30 Uhr oder Nutzerzeit |
| Day +1 | Fast breaking | Informationsanzeige, **kein Standard-Push** |

04:30 Uhr und Pańcajanya um etwa 05:00 Uhr sind UX-Kontext, keine astronomischen
Parameter. Day +1 darf ohne zusätzliche Parana-Berechnung kein vermeintlich
präzises Fastenbrechen-Zeitfenster anzeigen. Für Vaishnava ist ein beliebiger
Morgenzeitpunkt kein Ersatz für traditionelle Parana-Regeln. Ein späteres
Day-+1-Push erfordert ausdrückliches Opt-in.

Reminderdaten durch **lokale Kalenderarithmetik** bilden: `fastingDate ± 1 Tag`,
dann Nutzeruhrzeit in der betreffenden IANA-Zone auflösen; nicht UTC ±24 Stunden.
Vorgeschlagene mobile Auflösung: bei DST-Lücke erster gültiger Zeitpunkt danach,
bei doppelter Uhrzeit die frühere Instanz; ein vollständig übersprungener
Remindertag wird ausgelassen. Diese UX-Entscheidung separat testen und versionieren.

Bei Automatic-Ortswechsel zukünftige Reminder für den neuen Standort neu
berechnen und über stabile Event-IDs deduplizieren; bereits vergangene Reminder
nicht nachsenden. Ortswechsel werden erst nach einem neuen Standortfix bekannt.
Engine-Cache und Reminder-Planung haben getrennte Lebenszyklen. Für die tatsächliche
Astronomiebibliothek der mobilen App ist außerdem die bereits dokumentierte
[Lizenzentscheidung](../THIRD-PARTY-NOTICES.md) erforderlich.
