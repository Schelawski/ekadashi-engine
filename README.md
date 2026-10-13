# Ekadashi Engine

.NET-/Avalonia-Referenzprojekt für die spätere internationale Ekadashi-Berechnung
in **Marga Smriti**. Fachliches Konzept: [Issue #1](https://github.com/Schelawski/ekadashi-engine/issues/1).

## Stand

Das Desktop-Grundgerüst aus [Issue #2](https://github.com/Schelawski/ekadashi-engine/issues/2)
enthält eine Hauptansicht mit Ort, Jahr, Tradition und Ergebnistabelle.
Berlin, 2026 und Ananda Marga sind vorausgewählt. Der Berechnen-Button ist
deaktiviert: Astronomie, Traditionsregeln und Kalendervergleich sind noch nicht
implementiert. Es werden ausdrücklich keine Beispieltermine als berechnete
Fastentage angezeigt. Auch die zweite Tradition ist derzeit nur ein Platzhalter.

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
dotnet test tests/EkadashiEngine.Tests/EkadashiEngine.Tests.csproj
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
  EkadashiEngine.Core/       UI-unabhängige Orts- und Traditionsmodelle
  EkadashiEngine.Desktop/    Avalonia, ViewModels, DI und Logging
tests/
  EkadashiEngine.Tests/      xUnit, Headless-Smoke-Test und Architekturtest
data/
  reference/                Spätere offizielle Kalender mit Quellenangaben
  locations/                Späterer Ortskatalog
  exports/                  Spätere lokale Exporte und Testvektoren
```

Die Desktop-Schicht referenziert Core. Core hat keine Paket- oder UI-Abhängigkeiten.
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
- Vaishnava-/ISKCON-Regeln sowie Swiss-Ephemeris-Anbindung und Lizenzprüfung
  sind spätere Arbeitsschritte.
- Für Flutter/Dart sind gemeinsame Spezifikation und Testvektoren vorgesehen.
  Day −1 (Vorabend), Day 0 (Fastentag) und Day +1 (Fastenbrechen, primär informativ)
  bleiben eine getrennte mobile UX-Schicht.

Programmcode, Bezeichner, Codekommentare und Testnamen sind Englisch;
fachliche Dokumentation und Issues sind Deutsch.

Technische Grundlagen: [Avalonia Dependency Injection](https://docs.avaloniaui.net/docs/app-development/dependency-injection)
und [Avalonia Headless mit xUnit](https://docs.avaloniaui.net/docs/testing/headless-xunit).
