# Ekadashi Engine

.NET-/Avalonia-Referenzprojekt für die spätere internationale Ekadashi-Berechnung
in **Marga Smriti**. Fachliches Konzept: [Issue #1](https://github.com/Schelawski/ekadashi-engine/issues/1).

## Stand

Der Jahresworkflow aus [Issue #4](https://github.com/Schelawski/ekadashi-engine/issues/4)
ist nutzbar. Berlin, 2026 und der neutrale technische Modus sind vorausgewählt.
Neun Orte stehen zur Auswahl. Die Tabelle zeigt alle lokalen Kalendertage mit
Sonnenaufgang, Tithi am Sonnenaufgang, Kandidatenkennzeichnung und Tagesdetails.
Die angezeigten technischen Kandidaten sind noch keine validierten Fastentage.

Die Engine enthält jetzt die austauschbare Ananda-Marga-Strategie aus
[Issue #5](https://github.com/Schelawski/ekadashi-engine/issues/5). Sie verwendet
die dokumentierte Methode der größten Überschneidung zwischen Ekadashi und
Sonnenaufgangstag und liefert lokale `FastingDates` mit Entscheidungsdiagnosen.
Der erste Vergleich für Berlin 2026 ergibt **12 von 24 Übereinstimmungen** mit
dem offiziellen Sektorkalender; die Regel ist daher nicht als kalender-validiert
gekennzeichnet. Methode, API und Abweichungen stehen in
[Ananda-Marga-Regel](docs/ananda-marga-rule.md).
Die UI-Vergleichsansicht folgt in Issue #6; ISKCON-Regeln folgen separat.

Die astronomische Basis aus [Issue #3](https://github.com/Schelawski/ekadashi-engine/issues/3)
ist implementiert: Sonne/Mond, Winkeldifferenz, Tithi, lokaler Sonnenaufgang und
Diagnoseobjekte. SwissEphSharp ist in einem separaten Adapter gekapselt;
NodaTime liefert die IANA-Zeitzonendaten. Die Dienste sind über die Desktop-DI
mit dem Jahresworkflow verbunden.
API, Konventionen, Grenzen und Referenztests stehen in [Astronomie](docs/astronomy.md).

## Jahresübersicht bedienen

1. Ort und ein ganzes Jahr von **1900 bis 2100** auswählen. Dieser Bereich begrenzt
   den ersten UI-Workflow; er ist keine Zusicherung für jede Ephemeridenepoche.
2. **Berechnen** startet die Jahresberechnung im Hintergrund. Die Eingaben sind
   währenddessen gesperrt; **Abbrechen** stoppt den Lauf ohne unvollständige Ergebnisse.
3. **Nur Kandidaten** zeigt ausschließlich Tage mit Tithi **11 oder 26** beim
   astronomischen Sonnenaufgang. Ohne Filter sind alle 365 bzw. 366 Tage sichtbar.
4. Eine Zeile oder **Details** öffnet lokales Datum, Koordinaten, IANA-Zeitzone,
   Sonnenaufgang lokal mit UTC-Offset, Sonnenaufgang UTC, Sonne/Mond,
   Winkeldifferenz und Tithi am Sonnenaufgang.

Fehlender Sonnenaufgang oder ein übersprungener Kalendertag werden als
**nicht auswertbar** behandelt. Der Kandidatenfilter blendet solche Tage aus;
ihre Anzahl bleibt in der Zusammenfassung sichtbar. Bei Änderung von Ort, Jahr
oder Modus wird das bisherige Ergebnis gelöscht, damit es nicht versehentlich
der neuen Auswahl zugeordnet wird.

Für Berlin 2026 liefert der technische Modus aktuell **24 Kandidaten** bei 365
auswertbaren Tagen. Dies beschreibt das Rechenergebnis, nicht die Übereinstimmung
mit einem offiziellen Kalender. Auch ausgelassene oder doppelte Tithis am
Sonnenaufgang erhalten in dieser Phase noch keine traditionelle Sonderregel.

## Voraussetzungen und Start

- .NET 10 SDK; `global.json` erlaubt neuere stabile Feature-Bänder innerhalb von .NET 10.
- Desktop-System mit grafischer Sitzung (Windows, macOS oder Linux).
- Zugriff auf NuGet beim ersten Restore.

Im Repository-Stamm:

```powershell
dotnet restore EkadashiEngine.slnx --locked-mode
dotnet build EkadashiEngine.slnx --no-restore
dotnet run --project src/EkadashiEngine.Desktop
```

Tests unabhängig vom manuellen App-Start ausführen:

```powershell
dotnet test EkadashiEngine.slnx
```

Nur die Fachlogik und den Astronomieadapter prüfen, ohne Desktop-/Avalonia-Projekt:

```powershell
dotnet test tests/EkadashiEngine.Astronomy.Tests/EkadashiEngine.Astronomy.Tests.csproj
```

Der Avalonia-Headless-Smoke-Test lädt die echten XAML-Ansichten und Styles,
öffnet das Hauptfenster mit dem über DI aufgelösten ViewModel und prüft
Auswahlfelder, bidirektionale Bindings, die vollständige Berlin-Berechnung,
Kandidatenfilter und Tagesdetails. ViewModel-Tests prüfen Fehler, Abbruch und
Eingabevalidierung; die Fachtests berechnen 2026 für alle neun Orte und prüfen
unter anderem Schaltjahre und fehlende Sonnenaufgänge.
Er benötigt keinen sichtbaren Desktop. Ein Architekturtest sichert ab, dass Core
keine Referenz auf Avalonia oder Desktop hat. Das ersetzt keine plattformspezifische
Prüfung des nativen Fensterstarts.

## Struktur

```text
src/
  EkadashiEngine.Core/                      Modelle, Tithi, Diagnose, Schnittstellen
  EkadashiEngine.Astronomy.SwissEphemeris/   Austauschbarer Astronomieadapter
  EkadashiEngine.Desktop/                   Avalonia, ViewModels, DI und Logging
tests/
  EkadashiEngine.Astronomy.Tests/           UI-unabhängige Fach- und Referenztests
  EkadashiEngine.Tests/                     Desktop-Smoke-Test und Architekturtest
data/
  reference/                Astronomische Referenzen und AM Berlin Sector 2026
  locations/                Späterer Ortskatalog
  exports/                  Spätere lokale Exporte und Testvektoren
```

Die Desktop-Schicht referenziert Core und den Astronomieadapter. Core nutzt
NodaTime, hat aber keine Abhängigkeit von UI oder astronomischer Bibliothek.
Weitere fachliche Dienste und austauschbare Traditionsregeln gehören in Core;
die Verdrahtung erfolgt in `ServiceConfiguration` im Desktop-Projekt.
`Microsoft.Extensions.DependencyInjection` löst ViewModels und Logger auf.
Der ServiceProvider wird beim Beenden der App freigegeben.

Logging ist über `ILogger<T>` mit Debug- und Console-Provider vorbereitet.
Start und Ende der App sowie Jahresberechnungen und Fehler werden protokolliert.
Bei einem normalen Windows-Start als `WinExe` erscheint kein zusätzliches
Konsolenfenster; Logs sind im Debugger oder über umgeleitete Standardausgabe
verfügbar. Avalonia-Diagnostik verwendet zusätzlich `LogToTrace()`.

Paketversionen werden zentral in `Directory.Packages.props` gepflegt; die
`packages.lock.json`-Dateien fixieren die Abhängigkeiten. Bei beabsichtigten
Paketänderungen `dotnet restore EkadashiEngine.slnx --force-evaluate` ausführen
und aktualisierte Lockdateien prüfen.

## Fachliche Leitplanken für die nächsten Schritte

- Ein Ort besteht aus Koordinaten und IANA-Zeitzone, nicht allein aus einem Land.
- Der Jahresworkflow ist auf 1900–2100 begrenzt; der Astronomieadapter bleibt
  unabhängig davon für Einzelzeitpunkte nutzbar (siehe dokumentierte Grenzen).
- Der Fastentag ist ein lokales Kalenderdatum; astronomische Zeitpunkte können
  in UTC vorliegen. Die Erinnerungseinstellungen der App bestimmen keinen Fastentag.
- Die dokumentierte Ananda-Marga-Regel ist implementiert; die Abweichungen von
  offiziellen Kalenderdaten müssen fachlich weiter geklärt werden.
- Vaishnava-/ISKCON-Regeln und die Lizenzentscheidung für eine spätere direkte
  Produktintegration sind weitere Arbeitsschritte. Die Abhängigkeit dieses
  Referenzprojekts und ihre Original-Lizenztexte sind in
  [Drittanbieterhinweise](THIRD-PARTY-NOTICES.md) dokumentiert.
- Für Flutter/Dart sind gemeinsame Spezifikation und Testvektoren vorgesehen.
  Day −1 (Vorabend), Day 0 (Fastentag) und Day +1 (Fastenbrechen, primär informativ)
  bleiben eine getrennte mobile UX-Schicht.

Programmcode, Bezeichner, Codekommentare und Testnamen sind Englisch;
fachliche Dokumentation und Issues sind Deutsch.

Technische Grundlagen: [Avalonia Dependency Injection](https://docs.avaloniaui.net/docs/app-development/dependency-injection)
und [Avalonia Headless mit xUnit](https://docs.avaloniaui.net/docs/testing/headless-xunit).
