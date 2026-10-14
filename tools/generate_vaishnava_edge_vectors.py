"""Author synthetic rule-only acceptance fixtures; never run automatically in tests."""
import copy
import json
from datetime import datetime, timedelta, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
document = json.loads((ROOT / 'data/test-vectors/vaishnava-berlin-2026-v1.json').read_text(encoding='utf-8'))
document.update(astronomy={'provider': 'Synthetic', 'version': '1', 'ephemeris': 'Rule-only fixtures'}, reference=None, vectors=[], ruleCases=[])
scenarios = [('pure',12),('viddha',13),('omitted',12),('unmilani',13),('trisprsha',12),('combined',13),
             ('vyanjuli',13),('paksha',13),('jaya',13),('jayanti',13),('papanasini',13),('vijaya',13),
             ('missing-rise',None),('missing-nakshatra',None),('missing-lookahead',None)]
def instant(day, hour=6): return datetime(2026,1,day,hour,tzinfo=timezone.utc)
def iso(value): return value.isoformat()
for scenario, expected in scenarios:
    days = []
    for day in range(10,27):
        rise = instant(day)
        days.append(dict(localDate=rise.date().isoformat(),sunriseUtc=iso(rise),arunodayaUtc=iso(rise-timedelta(minutes=96)),
                         sunsetUtc=iso(rise+timedelta(hours=12)),sunriseTithi=day-1,arunodayaTithi=day-1,sunsetTithi=day-1,nakshatra=day%27))
    def set_day(day, **values): days[day-10].update(values)
    start, end = instant(12,1), instant(13,1)
    if scenario == 'viddha': set_day(12,arunodayaTithi=10); start += timedelta(hours=4)
    if scenario == 'omitted':
        set_day(12,sunriseTithi=12,arunodayaTithi=12,sunsetTithi=12)
        start -= timedelta(hours=18); end -= timedelta(days=1)
    if scenario in ('unmilani','combined'):
        set_day(13,sunriseTithi=11,arunodayaTithi=11); end += timedelta(hours=6)
        if scenario == 'unmilani': set_day(14,sunriseTithi=12)
    if scenario == 'trisprsha': set_day(13,sunriseTithi=13)
    if scenario == 'vyanjuli': set_day(14,sunriseTithi=12)
    if scenario == 'paksha': set_day(17,sunriseTithi=15)
    if scenario in ('jaya','jayanti','papanasini','vijaya'):
        nak = dict(jaya=6,jayanti=3,papanasini=7,vijaya=21)[scenario]
        set_day(13,nakshatra=nak); set_day(14,nakshatra=nak)
    if scenario == 'missing-rise': set_day(12,sunriseUtc=None,sunriseTithi=None,arunodayaTithi=None)
    if scenario == 'missing-nakshatra': set_day(13,nakshatra=None)
    if scenario == 'missing-lookahead': days = days[:6]
    solar = []
    for day in days:
        if not day['sunriseUtc']: continue
        rise = datetime.fromisoformat(day['sunriseUtc']); next_rise = rise+timedelta(days=1)
        solar.append(dict(date=day['localDate'],sunriseUtc=iso(rise),nextSunriseUtc=iso(next_rise),
                          overlapSeconds=max(0,(min(end,next_rise)-max(start,rise)).total_seconds())))
    document['ruleCases'].append(dict(tithi=11,startUtc=iso(start),endUtc=iso(end),solarDays=solar,
         expectedFastingDate=f'2026-01-{expected:02d}' if expected else None,
         expectedStatus='selected' if expected else 'missingRuleData',vaishnavaDays=copy.deepcopy(days)))
(ROOT/'data/test-vectors/vaishnava-edges-2026-v1.json').write_text(json.dumps(document,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
