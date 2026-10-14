using EkadashiEngine.Core;

namespace EkadashiEngine.Desktop.ViewModels;

public static class TraditionOptions
{
    public static IReadOnlyList<TraditionOption> All { get; } = Array.AsReadOnly<TraditionOption>([
        new(Tradition.TechnicalSunriseCandidate, "Technisch · Sonnenaufgang"),
        new(Tradition.AnandaMarga, "Ananda Marga"),
        new(Tradition.GaudiyaVaishnava, "Vaishnava / ISKCON")]);

    public static string Description(Tradition? tradition) => tradition switch
    {
        Tradition.AnandaMarga => "Ananda Marga: größte Ekadashi-Überschneidung mit dem Sonnenaufgangstag. Bekannte Abweichungen zum offiziellen AM-Kalender.",
        Tradition.GaudiyaVaishnava => "Vaishnava: Arunodaya (96 Minuten vor Sonnenaufgang), Viddha und Mahadvadashi. Berlin 2026: 24/24 GCal-Referenztage; keine weltweite Validierung.",
        _ => "Technisch: Tithi 11 oder 26 am örtlichen Sonnenaufgang. Kandidaten ohne traditionelle Fastenregel."
    };
}
