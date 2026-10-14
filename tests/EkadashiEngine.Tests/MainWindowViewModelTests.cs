using EkadashiEngine.Core;
using EkadashiEngine.Core.Astronomy;
using EkadashiEngine.Core.ReferenceData;
using EkadashiEngine.Core.Rules;
using EkadashiEngine.Core.Export;
using EkadashiEngine.Desktop.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace EkadashiEngine.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task CustomCoordinatesReachTheCalculatorAndEditsInvalidateExports()
    {
        Location? received = null;
        var viewModel = CreateViewModel(new StubCalculator((year, location, _) => { received = location; return CreateResult(year, location); }));
        viewModel.UseCustomLocation = true;
        viewModel.LocationEditor.DisplayName = "Munich";
        viewModel.LocationEditor.Latitude = 48.1372m;
        viewModel.LocationEditor.Longitude = 11.5756m;
        await viewModel.CalculateCommand.ExecuteAsync(null);
        Assert.NotNull(received);
        Assert.Equal(48.1372, received.Latitude);
        Assert.Equal(11.5756, received.Longitude);
        Assert.Equal("Europe/Berlin", received.TimeZoneId);
        Assert.True(viewModel.CanExport);
        viewModel.LocationEditor.TimeZoneId = "Invalid/Zone";
        Assert.Empty(viewModel.Results);
        Assert.Null(viewModel.Snapshot);
        Assert.False(viewModel.CalculateCommand.CanExecute(null));
        Assert.False(viewModel.CanExport);
        viewModel.UseCustomLocation = false;
        Assert.True(viewModel.CalculateCommand.CanExecute(null));
        Assert.Equal(LocationCatalog.DefaultLocations[0], viewModel.EffectiveLocation);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("2026.5")]
    [InlineData("1899")]
    [InlineData("2101")]
    public void InvalidYearDisablesCalculation(string? input)
    {
        var viewModel = CreateViewModel(new StubCalculator((_, _, _) => throw new InvalidOperationException()));
        viewModel.Year = input is null ? null : decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture);
        Assert.False(viewModel.CalculateCommand.CanExecute(null));
        Assert.True(viewModel.HasYearValidationError);
    }

    [Fact]
    public async Task FailureDoesNotLeaveOldResultsAndCanBeRetried()
    {
        var fail = false;
        var viewModel = CreateViewModel(new StubCalculator((year, location, _) =>
            fail ? throw new AstronomyCalculationException("Test failure") : CreateResult(year, location)));
        await viewModel.CalculateCommand.ExecuteAsync(null);
        Assert.Single(viewModel.Results);
        fail = true;
        await viewModel.CalculateCommand.ExecuteAsync(null);
        Assert.Empty(viewModel.Results);
        Assert.Contains("fehlgeschlagen", viewModel.StatusMessage);
        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.CalculateCommand.CanExecute(null));
        fail = false;
        await viewModel.CalculateCommand.ExecuteAsync(null);
        Assert.Single(viewModel.Results);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationOrInputChangeDoesNotPublishPartialResults(bool changeInputs)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var viewModel = CreateViewModel(new StubCalculator((_, _, token) =>
        {
            started.SetResult();
            if (!token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Cancellation was not requested.");
            token.ThrowIfCancellationRequested();
            throw new InvalidOperationException();
        }));

        var running = viewModel.CalculateCommand.ExecuteAsync(null);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(viewModel.IsBusy);
        Assert.False(viewModel.CalculateCommand.CanExecute(null));
        if (changeInputs)
            viewModel.SelectedLocation = viewModel.Locations[2];
        else
            viewModel.CalculateCancelCommand.Execute(null);
        await running;

        Assert.False(viewModel.IsBusy);
        Assert.Empty(viewModel.Results);
        Assert.Null(viewModel.SelectedResult);
        Assert.Contains(changeInputs ? "Eingaben geändert" : "abgebrochen", viewModel.StatusMessage);
        Assert.True(viewModel.CalculateCommand.CanExecute(null));
    }

    private static MainWindowViewModel CreateViewModel(IYearAstronomyCalculator calculator) =>
        new(calculator, new UnusedFastingCalculator(), new EmbeddedCalendarReferenceProvider(),
            new CalendarComparisonService(), TestCalculationServices.Factory, new CalculationExporter(), TestCalculationServices.CrossViewModel, NullLogger<MainWindowViewModel>.Instance);

    private sealed class UnusedFastingCalculator : IFastingCalendarCalculator
    {
        public TraditionYearResult Calculate(int year, Location location, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Technical mode must not invoke traditional calculations.");
    }

    private static YearAstronomyResult CreateResult(int year, Location location)
    {
        var date = new DateOnly(year, 1, 1);
        var utc = new DateTimeOffset(year, 1, 1, 6, 0, 0, TimeSpan.Zero);
        var day = new DailyAstronomyResult(date,
            new(date, SunriseDefinition.UpperLimbWithRefraction, SunriseStatus.Available, utc, location.GetLocalTime(utc)),
            new(350, 110), new(120, 11));
        return new(location, year, [day]);
    }

    private sealed class StubCalculator(Func<int, Location, CancellationToken, YearAstronomyResult> calculate) : IYearAstronomyCalculator
    {
        public YearAstronomyResult Calculate(int year, Location location, CancellationToken cancellationToken = default) =>
            calculate(year, location, cancellationToken);
    }
}
