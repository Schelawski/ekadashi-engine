# Internationale Locations und Jahresvergleich

Umsetzung von [Issue #8](https://github.com/Schelawski/ekadashi-engine/issues/8).

## Ortsmodell und Berechnungsidentität

`Location` enthält `DisplayName`, `Country`, `Latitude`, `Longitude` und
`TimeZoneId`. Breite und Länge werden als endliche Gradwerte in `[-90,90]` und
`[-180,180]` übergeben; Nord/Ost sind positiv. Die Zeitzone muss eine bekannte
IANA-ID sein, beispielsweise `Europe/Berlin`. Windows-Zeitzonennamen und reine
UTC-Offsets ersetzen eine IANA-Zeitzone nicht.

Koordinaten bestimmen den Sonnenaufgang, die Zeitzone die Zuordnung zu einem
lokalen Kalendertag und den jeweils gültigen UTC-Offset. Weder Land noch
Stadtname bestimmen astronomische Daten oder die Traditionsregel. Die sichtbare
Ortsbezeichnung und das Land sind Metadaten.

`LocationKey` bildet die Berechnungsidentität aus **Breite, Länge und IANA-ID**.
Zwei Punkte in `Europe/Berlin`, etwa Berlin und München, bleiben zwei Orte.
Identische Koordinaten mit derselben ID werden im Ortsvergleich unabhängig von
Namens-/Länderlabels als Duplikat behandelt. Identische Koordinaten mit einer
anderen Zeitzone werden ebenfalls nicht stillschweigend zusammengelegt.
Die Anzeige rundet Koordinaten; die Berechnung verwendet die gespeicherten Werte.

Das Desktop-Tool löst die Zeitzone nicht anhand geografischer Grenzdaten auf.
Eine formal gültige ID kann zu den eingegebenen Koordinaten unpassend sein;
für diesen manuellen Testfall verantwortet der Nutzer die Kombination. Die
Automatic-Integration muss diese Zuordnung später ausdrücklich übernehmen.

## Freie Eingabe im Hauptfenster

1. Einen der neun bekannten Orte als Vorlage auswählen.
2. **Eigene Koordinaten verwenden** aufklappen. Name, Länderlabel, Breite, Länge
   und IANA-Zeitzone bearbeiten. Die Zeitzonenauswahl bietet die eingebetteten
   TZDB-IDs als Suchvorschläge; eine gültige ID kann auch eingegeben werden.
3. **Berechnen** verwendet den vollständigen eigenen Standort in beiden Modi.
   Diagnose, Referenzsuche und JSON-/CSV-/Vektorexporte beziehen sich auf diesen Ort.
4. Durch Zuklappen wird wieder die Katalogauswahl verwendet. Eine neue
   Katalogauswahl füllt die Eingabefelder mit den Daten der gewählten Vorlage.

Ungültige/fehlende Eingaben sperren die Berechnung und zeigen eine konkrete
Meldung. Änderungen am aktiven Standort verwerfen Ergebnisse und Exportsnapshot;
ein Ergebnis kann nicht versehentlich unter neuen Koordinaten weiterverwendet
werden. Referenzen des Berlin Sector werden weiterhin nur beim dafür
festgelegten Vergleichspunkt Berlin und passender Zeitzone geladen.

## Cross-Location-Vergleich

**Orte vergleichen …** öffnet ein eigenes Fenster und übernimmt den aktuellen
Standort, ein gültiges Jahr und den Modus. Vier weit auseinanderliegende Orte
sind anfangs ausgewählt: Berlin, Delhi, New York und Sydney.

1. Mindestens zwei Orte markieren; **Alle Orte** wählt den gesamten Bestand.
2. Jahr und technischen oder AM-Modus wählen und **Vergleichen** starten.
3. Die Jahrestabelle zeigt Koordinaten, Zeitzone, Anzahl ausgewählter Tage und
   Anzahl ungeklärter Tagesentscheidungen je Ort.
4. Ein lokales Datum auswählen. Die untere Tabelle zeigt für alle Orte
   Sonnenaufgang lokal/UTC, Tithi und Ja/Nein/Ungeklärt.
5. **Nur unterschiedliche / ungeklärte Tage** reduziert die Datumsauswahl,
   ohne die Jahresstatistik zu verändern.

Unter **Eigenen Ort hinzufügen** können weitere freie Standorte ergänzt werden.
Ihre Auswahl lässt sich anschließend wie bei den Katalogorten über das
Kontrollkästchen ändern. Der aktuelle Standort aus dem Hauptfenster kann auch
einem bereits geöffneten Vergleichsfenster hinzugefügt werden.

Bekannte Testorte sind Berlin, Moscow, Delhi, London, New York, Los Angeles,
Tokyo, Sydney und São Paulo. Das Jahr umfasst für jeden Ort alle 365/366
**lokalen Kalenderdaten**. Gleiche Datumswerte sind keine identischen
UTC-Zeitintervalle: Der Sonnenaufgang am 13.01. in Tokyo kann beispielsweise
auf den 12.01. UTC fallen.

Im technischen Modus bezeichnet Ja einen Sonnenaufgangskandidaten (Tithi 11/26).
Im AM-Modus bezeichnet Ja das Ergebnis von `am-max-overlap-v1`, dessen bekannte
Abweichungen von offiziellen Kalendern fortbestehen. Es werden die Ergebnisse
der Orte untereinander verglichen, keine neue offizielle Referenz erzeugt.

Im Vaishnava-Modus bezeichnet Ja das Ergebnis von `vaishnava-gcal-v1`, einschließlich
Arunodaya, Viddha und Mahadvadashi. Alle neun Katalogorte sind technisch geprüft;
der unabhängige Kalenderabgleich gilt bislang für Berlin 2026. Fehlender relevanter
Regelkontext ergibt „Ungeklärt“. Die [Regeldokumentation](vaishnava-rule.md) erklärt
den Validierungsumfang und die konservative Behandlung polarer Randfälle.

Ein Datum hat unterschiedliche Entscheidungen, wenn mindestens ein auswertbarer
Ort Ja und ein anderer Nein ergibt. Ungeklärte Orte werden nicht als Nein oder
Übereinstimmung gezählt. Der Filter nimmt sie ausdrücklich mit auf. Abweichende
Sonnenaufgangszeiten allein werden nicht als abweichende Fastenentscheidung
gezählt; die Zeiten bleiben in der Datumstabelle einsehbar.

`ICrossLocationCalculator` ist UI-unabhängig. Es prüft die gemeinsame vollständige
Jahresperiode, erhält die Eingabereihenfolge und berechnet jeden Punkt mit
seinen eigenen Koordinaten. Fortschritt wird je abgeschlossenem Ort gemeldet.
Abbruch, Eingabeänderung, Fensterschließen oder ein Fehler übernehmen keine
unvollständige Vergleichsliste. Erneutes Berechnen bleibt möglich.

## Geprüfte Fälle

- Alle neun Katalogorte im gemeinsamen Jahr 2026; beim technischen Kriterium
  unterscheiden sich Entscheidungen an **35 von 365 lokalen Daten**.
- Berlin/München haben dieselbe IANA-Zeitzone und unterschiedliche Sonnenaufgänge.
- Berlin/Tokyo im AM-Modus; ein AM-Fastentag muss kein Sonnenaufgangskandidat sein.
- Schaltjahr, unbekannte Tageswerte, Duplikate und ungültige Jahre/Traditionen.
- Eigene Eingabe (Mumbai), Weitergabe an die Jahresberechnung, Exportinvalidierung
  und Übergabe an das Vergleichsfenster.
- ViewModel-Abbruch, Eingabewechsel, Fehler und Retry; echte XAML-Bindings,
  Datumsauswahl und Filter im Headless-Workflow. Beide Ansichten wurden gerendert.

Die spätere mobile Ortswahl ist in
[Automatic für Marga Smriti](automatic-location.md) spezifiziert. GPS, mobile
Permissions und ein Laufzeitcache sind in diesem Desktop-Testprojekt nicht aktiv.
