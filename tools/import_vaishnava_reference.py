"""Import fasting dates from a downloaded Berlin GCal ICS, without using engine results."""
import argparse
import hashlib
import json
import re
from datetime import date, datetime
from pathlib import Path

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--year', type=int, required=True)
    parser.add_argument('--retrieved-on', type=date.fromisoformat, required=True)
    args = parser.parse_args()
    raw = args.input.read_bytes()
    text = re.sub(r'\r?\n[ \t]', '', raw.decode('utf-8-sig'))
    dates = []
    for event in text.split('BEGIN:VEVENT')[1:]:
        if not re.search(r'^SUMMARY:Fasting for .*Ekadasi\s*$', event, re.M):
            continue
        match = re.search(r'^DTSTART;VALUE=DATE:(\d{8})\s*$', event, re.M)
        if not match:
            raise ValueError('Fasting event without an all-day DTSTART')
        dates.append(datetime.strptime(match[1], '%Y%m%d').date())
    if len(dates) not in (24, 25, 26) or len(set(dates)) != len(dates) or any(d.year != args.year for d in dates):
        raise ValueError('Expected a complete year of unique fasting dates; wrong year or incomplete source')
    document = dict(source=f'https://www.vaisnavacalendar.info/ICS/{args.year}/Berlin%20%5BGermany%5D-a{args.year}-ICS.ics',
        title=f'GCal 11 · Vaisnava Calendar · Berlin {args.year}',retrievedOn=args.retrieved_on.isoformat(),
        scope='Vollständige Ekadashi-Fastentage aus dem veröffentlichten GCal-11-Kalender; keine Parana-Zeiten. Anbieter unabhängig vom GBC Calendar Committee. Vergleich am Berliner Katalogpunkt.',
        year=args.year,tradition='GaudiyaVaishnava',comparisonLocation=dict(displayName='Berlin',country='DE',latitude=52.52,longitude=13.405,timeZoneId='Europe/Berlin'),
        fastingDates=[d.isoformat() for d in sorted(dates)],sourceSha256=hashlib.sha256(raw).hexdigest())
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(document,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

if __name__ == '__main__': main()
