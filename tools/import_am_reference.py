"""Import Ekadashii dates from a saved official Berlin Sector HTML calendar."""

import argparse
import datetime as dt
from html.parser import HTMLParser
import json
from pathlib import Path
import re


class CalendarParser(HTMLParser):
    def __init__(self):
        super().__init__()
        self.headings = []
        self.rows = []
        self.heading = None
        self.row = None
        self.cell = None

    def handle_starttag(self, tag, attrs):
        if tag == "h2":
            self.heading = []
        elif tag == "tr":
            self.row = []
        elif tag in ("td", "th") and self.row is not None:
            self.cell = []

    def handle_data(self, data):
        if self.heading is not None:
            self.heading.append(data)
        if self.cell is not None:
            self.cell.append(data)

    def handle_endtag(self, tag):
        if tag == "h2" and self.heading is not None:
            self.headings.append(" ".join("".join(self.heading).split()))
            self.heading = None
        elif tag in ("td", "th") and self.cell is not None:
            self.row.append(" ".join("".join(self.cell).split()))
            self.cell = None
        elif tag == "tr" and self.row is not None:
            self.rows.append(self.row)
            self.row = None


def extract_dates(html, year):
    calendar = CalendarParser()
    calendar.feed(html)
    published_years = [int(match.group(1)) for heading in calendar.headings
                       if (match := re.fullmatch(r"Fasting Chart\s+(\d{4})", heading))]
    if published_years != [year]:
        raise ValueError(f"Requested {year}, but calendar heading reports {published_years}.")
    months = {name: number for number, name in enumerate(
        ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"], 1)}
    dates = []
    for row in calendar.rows:
        if len(row) < 3 or row[1].casefold() not in ("ekadashi", "ekadashii"):
            continue
        match = re.fullmatch(r"([A-Za-z]{3})\s+(\d{1,2})", row[2])
        if not match or match.group(1) not in months:
            raise ValueError(f"Unsupported date in an Ekadashii row: {row!r}")
        dates.append(dt.date(year, months[match.group(1)], int(match.group(2))).isoformat())
    if len(dates) not in (24, 25) or len(set(dates)) != len(dates):
        raise ValueError("Expected 24 or 25 unique Ekadashii dates for a complete year.")
    # Weekdays are presentation data; the published month/day is authoritative here.
    return sorted(dates)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--html", required=True, type=Path, help="Saved official calendar HTML")
    parser.add_argument("--year", required=True, type=int)
    parser.add_argument("--retrieved-on", required=True, type=dt.date.fromisoformat)
    parser.add_argument("--source", default="https://anandamarga.eu/fastingChart.php/1000")
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    try:
        dates = extract_dates(args.html.read_text(encoding="utf-8-sig"), args.year)
    except ValueError as error:
        parser.error(str(error))
    document = {
        "source": args.source,
        "title": f"AMPS Berlin Sector – Fasting Chart {args.year}",
        "retrievedOn": args.retrieved_on.isoformat(),
        "scope": "Berlin Sector; die Quelle nennt weder Ortskoordinaten noch eine Berechnungsmethode.",
        "year": args.year,
        "tradition": "AnandaMarga",
        "comparisonLocation": {
            "displayName": "Berlin", "country": "DE", "latitude": 52.52,
            "longitude": 13.405, "timeZoneId": "Europe/Berlin"
        },
        "fastingDates": dates
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(document, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Imported {len(dates)} dates for {args.year} into {args.output}")


if __name__ == "__main__":
    main()
