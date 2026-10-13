# Ekadashi Engine

.NET-/Avalonia-Referenzprojekt für die spätere internationale Ekadashi-Berechnung
in **Marga Smriti**. Fachliches Konzept: [Issue #1](https://github.com/Schelawski/ekadashi-engine/issues/1).

## Stand

Das Desktop-Grundgerüst aus [Issue #2](https://github.com/Schelawski/ekadashi-engine/issues/2)
enthält eine Hauptansicht mit Ort, Jahr, Tradition und Ergebnistabelle.
Berlin, 2026 und Ananda Marga sind vorausgewählt. Der Berechnen-Button ist
deaktiviert: Traditionsregeln und Kalendervergleich sind noch nicht
implementiert. Es werden ausdrücklich keine Beispieltermine als berechnete
Fastentage angezeigt. Auch die zweite Tradition ist derzeit nur ein Platzhalter.

Die astronomische Basis aus [Issue #3](https://github.com/Schelawski/ekadashi-engine/issues/3)
ist implementiert: Sonne/Mond, Winkeldifferenz, Tithi, lokaler Sonnenaufgang und
Diagnoseobjekte. SwissEphSharp ist in einem separaten Adapter gekapselt;
NodaTime liefert die IANA-Zeitzonendaten. Diese Dienste sind in der Desktop-DI
registriert, aber noch nicht mit einem Berechnungsworkflow in der UI verbunden.
API, Konventionen, Grenzen und Referenztests stehen in [Astronomie](docs/astronomy.md).

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
Auswahlfelder, bidirektionale Bindings sowie den ehrlichen Platzhalterzustand.
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
  reference/                Astronomische Referenzen; später offizielle Kalender
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
Start und Ende der App sowie die Initialisierung des ViewModels werden protokolliert.
Bei einem normalen Windows-Start als `WinExe` erscheint kein zusätzliches
Konsolenfenster; Logs sind im Debugger oder über umgeleitete Standardausgabe
verfügbar. Avalonia-Diagnostik verwendet zusätzlich `LogToTrace()`.

Paketversionen werden zentral in `Directory.Packages.props` gepflegt; die
`packages.lock.json`-Dateien fixieren die Abhängigkeiten. Bei beabsichtigten
Paketänderungen `dotnet restore EkadashiEngine.slnx --force-evaluate` ausführen
und aktualisierte Lockdateien prüfen.

## Fachliche Leitplanken für die nächsten Schritte

- Ein Ort besteht aus Koordinaten und IANA-Zeitzone, nicht allein aus einem Land.
- Die Auswahl 1–9999 im Grundgerüst ist kein zugesicherter astronomischer
  Berechnungszeitraum. Dessen Grenzen legt erst der Astronomieadapter fest.
- Der Fastentag ist ein lokales Kalenderdatum; astronomische Zeitpunkte können
  in UTC vorliegen. Die Erinnerungseinstellungen der App bestimmen keinen Fastentag.
- Die Ananda-Marga-Regel wird erst anhand offizieller Kalender validiert.
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
