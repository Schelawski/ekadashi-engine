using EkadashiEngine.Core;
using EkadashiEngine.Core.Comparison;
using EkadashiEngine.Desktop.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace EkadashiEngine.Tests;

public sealed class CrossLocationViewModelTests
{
    [Fact]
    public async Task CompleteYearCanBeFilteredWithoutLosingUnknownDaysOrYearStatistics()
    {
        var viewModel = Create((year, locations, tradition, _) => Result(year, locations, tradition));
        Assert.Equal(9, viewModel.Locations.Count);
        viewModel.SelectAllCommand.Execute(null);
        viewModel.Year = 2028;
        await viewModel.CalculateCommand.ExecuteAsync(null);
        Assert.Equal(366, viewModel.Days.Count);
        Assert.Equal(9, viewModel.Summaries.Count);
        Assert.Equal(9, viewModel.DayRows.Count);
        var summary = viewModel.Summary;
        viewModel.ShowDifferencesOnly = true;
        Assert.Equal(2, viewModel.Days.Count);
        Assert.Contains(viewModel.Days, day => day.Data.HasUnknown);
        Assert.Equal(summary, viewModel.Summary);
        viewModel.Year = 2026;
        Assert.Empty(viewModel.Days);
        Assert.Empty(viewModel.DayRows);
        Assert.Empty(viewModel.Summaries);
    }

    [Fact]
    public void CustomLocationsAreDistinguishedByCoordinatesRatherThanTimeZoneOrName()
    {
        var viewModel = TestCalculationServices.CrossViewModel;
        var munich = new Location("Munich", "DE", 48.1372, 11.5756, "Europe/Berlin");
        var hamburg = new Location("Hamburg", "DE", 53.5511, 9.9937, "Europe/Berlin");
        viewModel.AddLocation(munich); viewModel.AddLocation(hamburg);
        Assert.Equal(11, viewModel.Locations.Count);
        viewModel.AddLocation(new("Different label", "XX", 48.1372, 11.5756, "Europe/Berlin"));
        Assert.Equal(11, viewModel.Locations.Count);
        viewModel.LocationEditor.TimeZoneId = "Invalid/Zone";
        Assert.False(viewModel.AddLocationCommand.CanExecute(null));
    }

    [Theory]
    [InlineData("2026.5")]
    [InlineData("1899")]
    [InlineData(null)]
    public void InvalidYearOrSingleLocationDisablesCalculation(string? input)
    {
        var viewModel = TestCalculationServices.CrossViewModel;
        viewModel.Year = input is null ? null : decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture);
        Assert.False(viewModel.CalculateCommand.CanExecute(null));
        viewModel.Year = 2026;
        foreach (var choice in viewModel.Locations.Skip(1)) choice.IsSelected = false;
        Assert.False(viewModel.CalculateCommand.CanExecute(null));
        Assert.NotEmpty(viewModel.ValidationMessage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationAndInputChangesNeverPublishPartialLocations(bool changeInputs)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var viewModel = Create((_, _, _, token) =>
        {
            started.SetResult();
            if (!token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10))) throw new TimeoutException();
            token.ThrowIfCancellationRequested();
            throw new InvalidOperationException();
        });
        var running = viewModel.CalculateCommand.ExecuteAsync(null);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        if (changeInputs) viewModel.Locations[0].IsSelected = false;
        else viewModel.CalculateCancelCommand.Execute(null);
        await running;
        Assert.False(viewModel.IsBusy);
        Assert.Empty(viewModel.Days);
        Assert.Empty(viewModel.Summaries);
        Assert.Contains(changeInputs ? "Eingaben geändert" : "abgebrochen", viewModel.StatusMessage);
    }

    [Fact]
    public async Task ErrorClearsOldDataAndCanBeRetried()
    {
        var fail = false;
        var viewModel = Create((year, locations, tradition, _) => fail ? throw new InvalidOperationException() : Result(year, locations, tradition));
        await viewModel.CalculateCommand.ExecuteAsync(null);
        Assert.NotEmpty(viewModel.Days);
        fail = true;
        await viewModel.CalculateCommand.ExecuteAsync(null);
        Assert.Empty(viewModel.Days);
        Assert.Empty(viewModel.DayRows);
        Assert.Contains("fehlgeschlagen", viewModel.StatusMessage);
        fail = false;
        await viewModel.CalculateCommand.ExecuteAsync(null);
        Assert.NotEmpty(viewModel.Days);
    }

    private static CrossLocationViewModel Create(Func<int, IReadOnlyList<Location>, Tradition, CancellationToken, CrossLocationResult> calculate) =>
        new(new StubCalculator(calculate), NullLogger<CrossLocationViewModel>.Instance);

    private sealed class StubCalculator(Func<int, IReadOnlyList<Location>, Tradition, CancellationToken, CrossLocationResult> calculate) : ICrossLocationCalculator
    {
        public CrossLocationResult Calculate(int year, IReadOnlyList<Location> locations, Tradition tradition,
            CancellationToken cancellationToken = default, IProgress<CrossLocationProgress>? progress = null) => calculate(year, locations, tradition, cancellationToken);
    }

    private static CrossLocationResult Result(int year, IReadOnlyList<Location> locations, Tradition tradition)
    {
        var results = locations.Select(location => new LocationYearResult(TestCalculationServices.Year(year, location), null)).ToArray();
        var days = Enumerable.Range(0, DateTime.IsLeapYear(year) ? 366 : 365).Select(index => new CrossLocationDay(new DateOnly(year, 1, 1).AddDays(index),
            results.Select((result, column) => new LocationDayResult(result.Astronomy.Location, result.Astronomy.Days[index],
                index == 0 ? column == 0 : index == 1 && column == 0 ? null : false)).ToArray())).ToArray();
        return new(year, tradition, results, days);
    }
}
