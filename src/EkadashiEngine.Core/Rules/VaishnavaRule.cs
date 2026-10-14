using EkadashiEngine.Core.Astronomy;

namespace EkadashiEngine.Core.Rules;

/// <summary>Gaurabda fasting-day rules; no festival fasts or parana times.
/// Rule ordering follows GCal's MahadvadasiCalc/EkadasiCalc (see third-party notice).</summary>
public sealed class VaishnavaRule : ITraditionRule
{
    public const string Version = "vaishnava-gcal-v1";
    public const string Source = "https://github.com/gopa810/gaurabda-calendar/blob/92c36b5948e9bcbfe991f19f511371aff1cc0fcb/gaurabda/TCalendar.py";
    public Tradition Tradition => Tradition.GaudiyaVaishnava;

    public TraditionYearResult Evaluate(PreparedTraditionYear data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        var days = (data.VaishnavaDays ?? []).OrderBy(day => day.LocalDate).ToArray();
        if (days.Select(day => day.LocalDate).Distinct().Count() != days.Length)
            throw new ArgumentException("Observation dates must be unique.", nameof(data));
        if (days.Any(day => day.SunriseTithi is < 1 or > 30 || day.ArunodayaTithi is < 1 or > 30
            || day.SunsetTithi is < 1 or > 30 || day.Nakshatra is < 0 or > 26))
            throw new ArgumentException("Invalid tithi or nakshatra.", nameof(data));
        var decisions = new List<RuleDecision>();
        foreach (var tithi in data.Tithis.Where(t => t.TithiNumber is 11 or 26))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (tithi.EndUtc <= tithi.StartUtc) throw new ArgumentException("Invalid tithi interval.", nameof(data));
            var startDate = DateOnly.FromDateTime(data.Location.GetLocalTime(tithi.StartUtc).DateTime).AddDays(-1);
            var endDate = DateOnly.FromDateTime(data.Location.GetLocalTime(tithi.EndUtc).DateTime).AddDays(9);
            // Keep the complete eight-sunrise full/new-moon lookahead in the portable decision input.
            var context = days.Where(d => d.LocalDate >= startDate && d.LocalDate <= endDate).ToArray();
            DateOnly? selected = null;
            var reason = "Missing observations for sunrise, arunodaya or Mahadvadashi lookahead.";
            var status = RuleDecisionStatus.MissingRuleData;
            var complete = context.Length == endDate.DayNumber - startDate.DayNumber + 1
                && context.All(d => d.SunriseUtc.HasValue && d.SunriseTithi.HasValue && d.ArunodayaTithi.HasValue);
            if (complete)
            {
                var mahadvadasi = new string?[context.Length];
                var unknown = false;
                for (var i = 1; i + 7 < context.Length; i++)
                {
                    var s = context[i - 1]; var t = context[i]; var u = context[i + 1];
                    if (Half(t.SunriseTithi) != 12 || Half(s.SunriseTithi) == 12) continue;
                    if (t.SunriseTithi == 12)
                    {
                        if (t.SunsetTithi is null || t.Nakshatra is null || u.Nakshatra is null) { unknown = true; continue; }
                        if (t.SunsetTithi == 12 && t.Nakshatra == u.Nakshatra)
                            mahadvadasi[i] = t.Nakshatra switch
                            { 6 => "Jaya Mahadvadashi", 3 => "Jayanti Mahadvadashi", 7 => "Papa-nasini Mahadvadashi", 21 => "Vijaya Mahadvadashi", _ => null };
                    }
                    if (mahadvadasi[i] is not null) continue;
                    if (Half(u.SunriseTithi) == 12 && Half(s.SunriseTithi) == 11 && Half(s.ArunodayaTithi) == 11)
                        mahadvadasi[i] = "Vyanjuli Mahadvadashi";
                    else if (Enumerable.Range(i + 1, 7).Any(j => context[j].SunriseTithi == context[j - 1].SunriseTithi && Half(context[j].SunriseTithi) == 15))
                        mahadvadasi[i] = "Paksha-vardhini Mahadvadashi";
                    else if (Half(s.ArunodayaTithi) < 11)
                        mahadvadasi[i] = "Viddha / omitted Ekadashi: fast on Dvadashi";
                }
                var choices = new List<(DateOnly Date, string Reason)>();
                for (var i = 1; i + 1 < context.Length; i++)
                {
                    var s = context[i - 1]; var t = context[i]; var u = context[i + 1];
                    if (t.SunriseUtc < tithi.StartUtc) continue;
                    if (t.SunriseTithi == tithi.TithiNumber + 1 && mahadvadasi[i] is { } mhd
                        && t.SunriseUtc < tithi.EndUtc.AddDays(2))
                        choices.Add((t.LocalDate, mhd));
                    if (t.SunriseTithi != tithi.TithiNumber || t.SunriseUtc >= tithi.EndUtc || Half(t.ArunodayaTithi) < 11) continue;
                    string? explanation;
                    if (Half(s.SunriseTithi) == 11 && Half(s.ArunodayaTithi) == 11)
                        explanation = Half(u.SunriseTithi) == 13 ? "Unmilani-Trisprsha Mahadvadashi" : "Unmilani Mahadvadashi";
                    else if (Half(u.SunriseTithi) == 13) explanation = "Trisprsha Mahadvadashi";
                    else if (Half(u.SunriseTithi) == 11 || mahadvadasi[i + 1] is not null) explanation = null;
                    else explanation = "Suddha Ekadashi: pure at arunodaya and sunrise";
                    if (explanation is not null) choices.Add((t.LocalDate, explanation));
                }
                if (!unknown && choices.Count == 1)
                {
                    selected = choices[0].Date; status = RuleDecisionStatus.Selected;
                    reason = choices[0].Reason + "; arunodaya = sunrise minus 96 elapsed minutes.";
                }
                else if (!unknown) reason = $"No unique Gaurabda decision ({choices.Count} candidates); no date guessed.";
            }
            // Selection happens before filtering to the requested local year.
            if (selected is { } date ? date.Year != data.Year :
                data.Location.GetLocalTime(tithi.StartUtc).Year != data.Year && data.Location.GetLocalTime(tithi.EndUtc).Year != data.Year) continue;
            var candidates = data.SolarDays.Where(d => d.LocalDate >= startDate && d.LocalDate <= endDate
                && d.SunriseUtc.HasValue && d.NextSunriseUtc.HasValue).Select(d =>
                {
                    var from = d.SunriseUtc!.Value > tithi.StartUtc ? d.SunriseUtc.Value : tithi.StartUtc;
                    var to = d.NextSunriseUtc!.Value < tithi.EndUtc ? d.NextSunriseUtc.Value : tithi.EndUtc;
                    return new DayOverlap(d.LocalDate, d.SunriseUtc.Value, d.NextSunriseUtc.Value, to > from ? to - from : TimeSpan.Zero);
                }).ToArray();
            decisions.Add(new(tithi, selected, status, candidates, reason, context));
        }
        cancellationToken.ThrowIfCancellationRequested();
        return new(data.Location, data.Year, Tradition, Version, Source, false,
            decisions.Where(d => d.FastingDate.HasValue).Select(d => d.FastingDate!.Value).Distinct().Order().ToArray(), decisions.AsReadOnly());
    }

    private static int? Half(int? tithi) => (tithi - 1) % 15 + 1;
}
