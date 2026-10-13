# Ortsdaten

Vorgesehener Ablageort für einen späteren extern ladbaren Ortskatalog.
Der erste feste Katalog liegt zentral in
[`LocationCatalog`](../../src/EkadashiEngine.Core/LocationCatalog.cs) und enthält
Berlin, Moscow, Delhi, London, New York, Los Angeles, Tokyo, Sydney und São Paulo
mit Anzeigename, ISO-Ländercode, Breitengrad, Längengrad und IANA-Zeitzone.

Die Koordinaten sind Stadt-Referenzpunkte. Berlin (52.5200, 13.4050,
`Europe/Berlin`) ist vorausgewählt. Die Jahresintegration wird für alle neun
Orte getestet; dies ersetzt nicht die spätere Kalender-Validierung pro Tradition.
