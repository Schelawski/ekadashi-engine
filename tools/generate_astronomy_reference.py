"""Generate adapter regression fixtures with Astrodienst's native swetest 2.10.03."""

import argparse
import hashlib
import json
import math
from datetime import datetime, timezone
from pathlib import Path
import re
import subprocess
from zoneinfo import ZoneInfo


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("swetest", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    executable = args.swetest.resolve()
    cases = []
    cities = [
        ("Berlin", "DE", 52.52, 13.405, "Europe/Berlin", ["2026-01-15", "2026-06-21", "2026-03-29", "2026-10-25"]),
        ("Moscow", "RU", 55.7558, 37.6173, "Europe/Moscow", ["2026-01-15", "2026-06-21"]),
        ("Delhi", "IN", 28.6139, 77.2090, "Asia/Kolkata", ["2026-01-15", "2026-06-21"]),
        ("Kiritimati", "KI", 1.8721, -157.4278, "Pacific/Kiritimati", ["2026-01-15"]),
        ("Los Angeles", "US", 34.0522, -118.2437, "America/Los_Angeles", ["2026-01-15"]),
    ]

    def run(options):
        output = subprocess.check_output([str(executable), *options], text=True)
        if "error" in output.lower():
            raise RuntimeError(output)
        return output

    for name, country, latitude, longitude, zone, dates in cities:
        for date in dates:
            instant = datetime.fromisoformat(date).replace(tzinfo=timezone.utc)
            position_args = [f"-b{instant:%d.%m.%Y}", "-utc00:00:00", "-p01", "-emos", "-fPl", "-g,", "-head"]
            position_output = run(position_args)
            sun, moon = [float(line.split(",")[1]) for line in position_output.splitlines() if "," in line]
            local_midnight = datetime.fromisoformat(date).replace(tzinfo=ZoneInfo(zone))
            start = local_midnight.astimezone(timezone.utc)
            sunrise_values = {}
            sunrise_commands = {}
            for definition, flags in [("UpperLimbWithRefraction", []), ("HinduGeometricCenter", ["-hindu"])]:
                command = [f"-b{start:%d.%m.%Y}", f"-utc{start:%H:%M:%S}", "-p0", "-emos", "-rise",
                           f"-geopos{longitude},{latitude},0", "-at1013.25,15", "-n1", "-ep", *flags]
                output = run(command)
                match = re.search(r"rise\s+(\d{2}\.\d{2}\.\d{4})\s+(\d{2}:\d{2}:\d{2}\.\d+)", output)
                if not match:
                    raise RuntimeError(output)
                # swetest reports rise times in UT1, not UTC. Leave the value unzoned.
                sunrise_values[definition] = datetime.strptime(" ".join(match.groups()), "%d.%m.%Y %H:%M:%S.%f").isoformat()
                sunrise_commands[definition] = command
            difference = (moon - sun) % 360
            cases.append({
                "location": {"displayName": name, "country": country, "latitude": latitude,
                             "longitude": longitude, "timeZoneId": zone},
                "localDate": date,
                "instantUtc": instant.isoformat(),
                "sunLongitude": sun,
                "moonLongitude": moon,
                "angularDifference": difference,
                "tithiNumber": math.floor(difference / 12) + 1,
                "sunriseUt1": sunrise_values,
                "commands": {"positions": position_args, "sunrise": sunrise_commands},
            })

    document = {
        "source": "Astrodienst swetest64 2.10.03 (native C), Moshier, apparent geocentric ecliptic longitudes of date",
        "sourceUrl": "https://github.com/aloistr/swisseph/blob/aacf962d19d79f8bc921dbcdaacf306d85be1917/windows/programs/swetest64.exe",
        "sha256": hashlib.sha256(executable.read_bytes()).hexdigest(),
        "longitudeToleranceDegrees": 0.00001,
        "sunriseToleranceSeconds": 2,
        "sunriseTimeScale": "UT1; compared to UTC with a 2-second tolerance for these contemporary fixtures",
        "cases": cases,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8")
    print(f"Wrote {len(cases)} reference cases to {args.output}")


if __name__ == "__main__":
    main()
