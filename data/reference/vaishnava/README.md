# Vaishnava-Referenzen

`gcal-berlin-2026.calendar.json` enthält 24 unabhängig von dieser Engine
veröffentlichte GCal-Fastentage für Berlin. Quelle, Abrufdatum, Vergleichspunkt
und SHA-256 der heruntergeladenen ICS stehen im Datensatz.

Quelle: [Vaisnava Calendar, Berlin 2026 ICS](https://www.vaisnavacalendar.info/ICS/2026/Berlin%20%5BGermany%5D-a2026-ICS.ics).
Der Anbieter verwendet GCal 11, wird aber nicht vom GBC Calendar Committee
betrieben. Dieser Unterschied ist in der Quellenbeschreibung festgehalten.
Die fünf letzten Termine 2026 sind zusätzlich auf der Website des
[ISKCON-Tempels Berlin](https://www.krishnaberlin.com/en.html) veröffentlicht.

Der Importer `tools/import_vaishnava_reference.py` extrahiert ausschließlich
Fastenereignisse, keine Fastenbrechen- oder Festivalereignisse. Er verwendet
keine Engine-Ergebnisse und prüft Vollständigkeit, Jahr und Eindeutigkeit.
Die Herkunft der Eingabedatei muss als Berliner Kalender überprüft werden.
Eine Kalenderübereinstimmung am Katalogpunkt ist keine Kenntnis der intern
vom Referenzanbieter verwendeten exakten Koordinaten.

Die Referenz darf nur für GaudiyaVaishnava, Berlin-Koordinaten 52.52/13.405,
Europe/Berlin und 2026 verwendet werden. Andere Anfragen erhalten keine Referenz.
