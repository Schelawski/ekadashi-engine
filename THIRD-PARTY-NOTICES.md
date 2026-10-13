# Drittanbieterhinweise

## SwissEphSharp 2.10.3.1

Das Referenzprojekt verwendet [SwissEphSharp](https://github.com/Tim81/SwissEphNet),
den von Timothy van der Ham gepflegten C#-Port auf Basis von Yan Greniers Arbeit.
Die zugrunde liegende Swiss Ephemeris stammt von Dieter Koch und Alois Treindl,
Astrodienst AG. Dies ist eine Quellenangabe, keine Empfehlung durch diese Autoren.

Das NuGet-Paket nennt AGPL-3.0 oder eine kommerzielle Swiss Ephemeris Professional
License als Lizenzalternativen. Für die hier verwendete freie Referenzimplementierung
wird die AGPL-Variante verwendet. Die bestehende GPL-3.0-Lizenz des eigenen
Repository-Codes wird nicht ersetzt; bei Kombination mit AGPL-Code ist auch
Abschnitt 13 der GPL-3.0 zu beachten. Eine spätere direkte Integration in Marga
Smriti benötigt eine eigene Lizenzentscheidung.

Die unverändert aus dem Paket übernommenen Texte liegen unter
[`third-party/SwissEphSharp`](third-party/SwissEphSharp) und werden in die Build-
und Publish-Ausgabe kopiert:

- `LICENSE`: Lizenzbedingungen der Swiss Ephemeris
- `NOTICE`: Urheber- und Portierungshinweise
- `agpl-3.0.txt`: vollständiger AGPL-3.0-Text

Originalquellen: [SwissEphSharp-Paket](https://www.nuget.org/packages/SwissEphSharp/2.10.3.1),
[Swiss-Ephemeris-Lizenz](https://github.com/aloistr/swisseph/blob/master/LICENSE).

## NodaTime 3.3.4

[NodaTime](https://nodatime.org/) von den Noda Time Authors wird unter
[Apache-2.0](https://github.com/nodatime/nodatime/blob/main/LICENSE.txt) angeboten.
Die eingebettete IANA-Zeitzonendatenbank wird mit der Paketversion festgelegt.

Weitere NuGet-Abhängigkeiten des Desktop- und Testgrundgerüsts sind über
`Directory.Packages.props` und die jeweiligen `packages.lock.json` nachvollziehbar.
