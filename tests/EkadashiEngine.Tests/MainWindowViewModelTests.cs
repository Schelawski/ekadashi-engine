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
            new CalendarComparisonService(), TestCalculationServices.Factory, new CalculationExporter(), NullLogger<MainWindowViewModel>.Instance);

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
