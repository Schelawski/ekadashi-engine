# Vaishnava / ISKCON

`VaishnavaRule` ist eine eigenständige, reine Strategie für den lokalen Fastentag.
Sie verwendet die Entscheidungsreihenfolge von Gaurabda/GCal, einschließlich
Arunodaya, Viddha und der acht Mahadvadashi-Arten. Festivals, Fastenstärken und
Parana-Zeitfenster gehören nicht zu dieser Implementierung.

## Quellen und Konventionen

- Regelreferenz: [Gaurabda TCalendar.py, Commit 92c36b5](https://github.com/gopa810/gaurabda-calendar/blob/92c36b5948e9bcbfe991f19f511371aff1cc0fcb/gaurabda/TCalendar.py),
  insbesondere `MahadvadasiCalc`, `EkadasiCalc`, `IsMhd58`, `NextNewFullIsVriddhi`.
  Die MIT-Lizenz liegt in `third-party/Gaurabda/LICENSE` und wird mit ausgeliefert.
- Unabhängige Datumsreferenz: [Berlin 2026, GCal-ICS](https://www.vaisnavacalendar.info/ICS/2026/Berlin%20%5BGermany%5D-a2026-ICS.ics),
  abgerufen am 14.10.2026. Der Anbieter verwendet nach eigener Beschreibung
  GCal 11; die Website wird nicht vom GBC Calendar Committee betrieben.
- Zusätzliche Stichprobe: [ISKCON Berlin](https://www.krishnaberlin.com/en.html)
  veröffentlicht die letzten fünf Fastentage 2026 (22.10., 05.11., 20.11.,
  04.12., 20.12.); diese stimmen ebenfalls überein.

Eigene Astronomie bleibt Swiss Ephemeris/Moshier, oberer Sonnenrand mit Refraktion,
Meereshöhe, 1013,25 hPa und 15 °C. Arunodaya ist **96 verstrichene Minuten** vor
dem lokalen Sonnenaufgang. Nakshatra: geozentrischer Mond abzüglich Swiss-Lahiri-
Ayanamsha, normiert auf 0–360°, dann `floor(longitude / (360/27))`, Index 0–26.
GCal verwendet eine eigene astronomische Näherung. Ein identisches Regelwerk
garantiert daher keine weltweite Identität in astronomischen Grenzfällen.

## Regelreihenfolge

Tithis in dieser Engine: 1–15 zunehmend, 16–30 abnehmend. Für Prüfungen innerhalb
einer Hälfte gilt `half(t) = ((t-1) mod 15)+1`. Ekadashi ist 11/26, Dvadashi
12/27, Trayodashi 13/28. Die GCal-Nummerierung wird nicht übernommen.

Zuerst werden mögliche Mahadvadashi-Tage am ersten Dvadashi-Sonnenaufgang markiert:

1. Zunehmende Dvadashi besteht bis Sonnenuntergang, und derselbe Nakshatra liegt
   am heutigen und morgigen Sonnenaufgang vor: Punarvasu (6) → Jaya,
   Rohini (3) → Jayanti, Pushya (7) → Papa-nasini, Shravana (21) → Vijaya.
   Die Sonnenuntergangsbedingung folgt dem äußeren Aufrufer in der Referenz.
2. Dvadashi an zwei Sonnenaufgängen und gestern reine Ekadashi auch an Arunodaya
   → Vyanjuli.
3. Voll- oder Neumond-Tithi wiederholt sich an aufeinanderfolgenden Sonnenaufgängen
   innerhalb der acht betrachteten Daten ab Dvadashi → Paksha-vardhini.
4. Gestern an Arunodaya noch vor Ekadashi → Fasten an Dvadashi (Viddha bzw.
   ausgelassene Ekadashi am Sonnenaufgang).

Anschließend werden Ekadashi-Sonnenaufgänge geprüft:

1. An Arunodaya noch vor Ekadashi → heute nicht fasten.
2. Gestern Ekadashi sowohl an Arunodaya als auch am Sonnenaufgang → heute
   Unmilani, kombiniert mit Trisprsha, wenn morgen Trayodashi ist.
3. Morgen Trayodashi → heute Trisprsha.
4. Morgen nochmals Ekadashi oder markierte Mahadvadashi → morgen vorbehalten.
5. Sonst heute Suddha Ekadashi.

Pro Ekadashi-Intervall muss genau eine Entscheidung entstehen. Ein ausgewähltes
Datum wird erst danach auf das angeforderte lokale Jahr gefiltert. Fehlende Daten
ergeben `MissingRuleData` und `fastingDate = null`, keine Ersatzzeit und kein
angenommenes Nein. Die Vorbereitung enthält zwölf zusätzliche Sonnenaufgangstage
an beiden Jahresrändern und vierzehn UTC-Tage für die Tithi-Suche.

Die erste Version verlangt konservativ einen vollständigen Sonnenaufgangs- und
Arunodaya-Kontext vom lokalen Intervallbeginn minus einem Tag bis Intervallende
plus neun Tagen. Bei Polarbedingungen oder übersprungenen Daten kann dadurch
auch eine benachbarte Entscheidung ungeklärt bleiben. Fehlende Sonnenuntergangs-
oder Nakshatra-Daten an relevanter zunehmender Dvadashi bleiben ebenfalls offen.

## Validierung

Berlin 2026: **24 von 24** Fastentagen stimmen mit der unabhängigen GCal-ICS überein.
Die Berechnung enthält unter anderem Trisprsha (29.01., 09.08.), Viddha (29.03.,
11.07.) und Paksha-vardhini (24.08., 05.11.). Die übrigen seltenen Mahadvadashi-
Zweige sind durch ausdrücklich synthetische Regeltests und portable Vektoren
abgedeckt; dies ist keine unabhängige Kalenderbestätigung dieser seltenen Fälle.

Die berechneten AM- und Vaishnava-Kalender unterscheiden sich 2026 in Berlin bei
**17 von 24** Intervallen um einen Tag. Beispiele:

| Intervall | AM | Vaishnava/GCal | Grund bei Vaishnava |
| --- | --- | --- | --- |
| März, zunehmend | 28.03.2026 | 29.03.2026 | Viddha an Arunodaya |
| Juli, abnehmend | 10.07.2026 | 11.07.2026 | Viddha an Arunodaya |
| August, zunehmend | 23.08.2026 | 24.08.2026 | Paksha-vardhini |

Die dokumentierte AM-Methode bleibt unverändert; ihre bekannten Abweichungen
zum offiziellen AM-Sektorkalender sind kein Anlass, die Vaishnava-Regel auf AM
anzuwenden. `IsCalendarValidated` bleibt ein globaler Freigabeindikator und
weiterhin `false`; der konkrete Referenzabgleich liefert seine eigene Statistik.

## Bedienung und Nachrechnung

Im Hauptfenster **Vaishnava / ISKCON** wählen. Die Tabelle zeigt AM, Vaishnava,
den Abstand zwischen beiden sowie die Referenz **der gewählten Tradition**.
Der Status und „Nur Referenzabweichungen“ beziehen sich ausschließlich auf diese
Referenz. Eine Zeile öffnet Entscheidung, Arunodaya und den vollständigen Kontext.
Auch der Ortsvergleich unterstützt alle drei Modi. JSON/CSV/Testvektoren beziehen
sich auf die gewählte Tradition; für beide Traditionen getrennt exportieren.

```powershell
dotnet run --project tools/EkadashiEngine.Export -- --year 2026 --location Berlin --mode vaishnava --format json --output artifacts/vaishnava-berlin.json
python tools/import_vaishnava_reference.py --input berlin-2026.ics --output artifacts/reference.json --year 2026 --retrieved-on 2026-10-14
```

Der Import liest ausschließlich `SUMMARY:Fasting for … Ekadasi` und die
zugehörigen ganztägigen `DTSTART`-Daten. Er prüft Jahr, Duplikate und Jahresumfang;
der SHA-256 der Quelldatei ist im Referenz-JSON festgehalten. Die Herkunft als
Berlin-Datei muss beim Download geprüft werden, da die ICS selbst keine
zuverlässigen Ortskoordinaten enthält. Der Berliner Katalogpunkt wird ausdrücklich
als Vergleichspunkt dokumentiert; Label- oder Länderänderungen ändern die
Zuordnung nicht, andere Koordinaten oder IANA-Zonen dagegen schon.
