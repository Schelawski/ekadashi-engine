namespace EkadashiEngine.Core.Astronomy;

public sealed class YearAstronomyCalculator(
    IAstronomyService astronomy,
    ISunriseCalculator sunrise,
    ITithiCalculator tithi) : IYearAstronomyCalculator
{
    // Deliberately restrict the first UI workflow to modern calendar years.
    public const int MinimumYear = 1900;
    public const int MaximumYear = 2100;

    public YearAstronomyResult Calculate(int year, Location location, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);
        if (year is < MinimumYear or > MaximumYear)
            throw new ArgumentOutOfRangeException(nameof(year), $"Year must be between {MinimumYear} and {MaximumYear}.");

        var firstDay = new DateOnly(year, 1, 1);
        var dayCount = DateTime.IsLeapYear(year) ? 366 : 365;
        var days = new List<DailyAstronomyResult>(dayCount);
        var dayCalculator = new DayAstronomyCalculator(astronomy, sunrise, tithi);
        for (var index = 0; index < dayCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var date = firstDay.AddDays(index);
            days.Add(dayCalculator.Calculate(date, location));
        }
        cancellationToken.ThrowIfCancellationRequested();
        return new(location, year, days.AsReadOnly());
    }
}
