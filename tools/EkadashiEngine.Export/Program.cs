using System.Globalization;
using EkadashiEngine.Astronomy.SwissEphemeris;
using EkadashiEngine.Core;
using EkadashiEngine.Core.Astronomy;
using EkadashiEngine.Core.Diagnostics;
using EkadashiEngine.Core.Export;
using EkadashiEngine.Core.ReferenceData;
using EkadashiEngine.Core.Rules;

if (args.Length == 0 || args.Contains("--help"))
{
    Console.WriteLine("""
        Export a complete calculation without starting the desktop UI.
        --year 2026 --location Berlin --mode am|vaishnava|technical --format json|csv|test-vectors --output path
        Optional: --dates yyyy-MM-dd,yyyy-MM-dd (test-vectors only)
        Custom location: --latitude number --longitude number --time-zone IANA --country code
        """);
    return 0;
}

try
{
    var options = new Dictionary<string, string>(StringComparer.Ordinal);
    string[] supported = ["--year", "--location", "--mode", "--format", "--output", "--dates", "--latitude", "--longitude", "--time-zone", "--country"];
    for (var i = 0; i < args.Length; i += 2)
    {
        if (i + 1 >= args.Length || !supported.Contains(args[i]) || !options.TryAdd(args[i], args[i + 1]))
            throw new ArgumentException($"Unknown, duplicate or incomplete option: {args[i]}");
    }
    string Required(string key) => options.TryGetValue(key, out var value) ? value : throw new ArgumentException($"Missing {key}");
    var year = int.Parse(Required("--year"), CultureInfo.InvariantCulture);
    var name = Required("--location");
    var custom = options.Keys.Any(key => key is "--latitude" or "--longitude" or "--time-zone" or "--country");
    var location = custom
        ? new Location(name, Required("--country"), double.Parse(Required("--latitude"), CultureInfo.InvariantCulture),
            double.Parse(Required("--longitude"), CultureInfo.InvariantCulture), Required("--time-zone"))
        : LocationCatalog.DefaultLocations.SingleOrDefault(item => item.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Unknown catalog location: {name}");
    var format = Required("--format") switch
    {
        "json" => ExportFormat.Json, "csv" => ExportFormat.Csv, "test-vectors" => ExportFormat.TestVectors,
        _ => throw new ArgumentException("Format must be json, csv or test-vectors.")
    };
    var mode = options.GetValueOrDefault("--mode", "am");
    if (mode is not ("am" or "vaishnava" or "technical")) throw new ArgumentException("Mode must be am, vaishnava or technical.");
    var output = Path.GetFullPath(Required("--output"));
    var sky = new SwissEphemerisAstronomyService();
    var tithi = new TithiCalculator();
    var astronomy = new YearAstronomyCalculator(sky, sky, tithi).Calculate(year, location);
    TraditionYearResult? calendar = null;
    CalendarComparisonResult? comparison = null;
    if (mode != "technical")
    {
        calendar = new FastingCalendarCalculator(new TraditionDataCalculator(sky, new TithiIntervalCalculator(sky, tithi)),
            new AnandaMargaRule()).Calculate(year, location, mode == "am" ? Tradition.AnandaMarga : Tradition.GaudiyaVaishnava);
        comparison = new CalendarComparisonService().Compare(calendar,
            new EmbeddedCalendarReferenceProvider().Find(year, location, calendar.Tradition));
    }
    var snapshot = new CalculationSnapshotFactory(new DayAstronomyCalculator(sky, sky, tithi),
        SwissEphemerisAstronomyService.Provenance).Create(astronomy, calendar, comparison);
    var exporter = new CalculationExporter();
    var payload = exporter.Create(snapshot, format);
    if (options.TryGetValue("--dates", out var dateText))
    {
        if (format != ExportFormat.TestVectors) throw new ArgumentException("--dates is only supported for test-vectors.");
        var dates = dateText.Split(',').Select(value => DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture)).ToHashSet();
        if (dates.Count == 0 || dates.Any(date => date.Year != year)) throw new ArgumentException("Vector dates must belong to the requested year.");
        var vectors = exporter.CreateTestVectors(snapshot);
        payload = payload with { Content = CalculationExporter.Json(vectors with { Vectors = vectors.Vectors.Where(vector => dates.Contains(vector.Date)).ToArray() }) };
    }
    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
    File.WriteAllBytes(output, payload.Content);
    Console.WriteLine($"Exported {payload.Content.Length} bytes to {output}");
    return 0;
}
catch (Exception exception) when (exception is ArgumentException or FormatException or OverflowException
    or TimeZoneNotFoundException or InvalidOperationException or IOException or AstronomyCalculationException)
{
    Console.Error.WriteLine(exception.Message);
    return 2;
}
