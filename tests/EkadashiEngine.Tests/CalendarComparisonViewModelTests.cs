using EkadashiEngine.Core;
using EkadashiEngine.Core.Astronomy;
using EkadashiEngine.Core.ReferenceData;
using EkadashiEngine.Core.Rules;
using EkadashiEngine.Core.Export;
using EkadashiEngine.Desktop.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace EkadashiEngine.Tests;

public sealed class CalendarComparisonViewModelTests
{
    [Fact]
    public async Task TraditionalModePublishesComparisonAndFilterPreservesStatistics()
    {
        var viewModel = Create((year, location, _) => Result(year, location));
        await viewModel.CalculateCommand.ExecuteAsync(null);
        Assert.Equal(24, viewModel.ComparisonRows.Count);
        Assert.Contains("12 Matches", viewModel.ComparisonSummary);
        Assert.Contains("12 Mismatches", viewModel.ComparisonSummary);
        Assert.Contains("50", viewModel.ComparisonSummary);
        var summary = viewModel.ComparisonSummary;
        viewModel.ShowMismatchesOnly = true;
        Assert.Equal(12, viewModel.ComparisonRows.Count);
        Assert.All(viewModel.ComparisonRows, row => Assert.True(row.IsMismatch));
        Assert.Equal(summary, viewModel.ComparisonSummary);
        viewModel.SelectedComparisonRow = viewModel.ComparisonRows[0];
        viewModel.Year = 2027;
        Assert.Empty(viewModel.ComparisonRows);
        Assert.Null(viewModel.SelectedComparisonRow);
        Assert.Empty(viewModel.ReferenceSource);
        Assert.DoesNotContain("12 Matches", viewModel.ComparisonSummary);
    }

    [Theory]
    [InlineData(2025, 0)]
    [InlineData(2027, 0)]
    [InlineData(2026, 2)]
    public async Task UnavailableReferenceIsVisibleWithoutInventingStatistics(int year, int locationIndex)
    {
        var viewModel = Create((requestedYear, location, _) => Result(requestedYear, location));
        viewModel.Year = year;
        viewModel.SelectedLocation = viewModel.Locations[locationIndex];
        await viewModel.CalculateCommand.ExecuteAsync(null);
        Assert.NotEmpty(viewModel.ComparisonRows);
        Assert.All(viewModel.ComparisonRows, row => Assert.Equal("KEINE REFERENZ", row.Status));
        Assert.Contains("Keine passenden Referenzdaten", viewModel.ReferenceDescription);
        Assert.Contains("nicht verfügbar", viewModel.ComparisonSummary);
        Assert.DoesNotContain("%", viewModel.ComparisonSummary);
    }

    [Fact]
    public async Task UnresolvedRuleDecisionsWarnAboutIncompleteList()
    {
        var viewModel = Create((year, location, _) => Result(year, location) with
        {
            Decisions = [new(new(11, new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero)), null,
                RuleDecisionStatus.MissingSunriseCoverage, [], "Unavailable")]
        });
        await viewModel.CalculateCommand.ExecuteAsync(null);
        Assert.Contains("1 Regelentscheidungen sind ungeklärt", viewModel.ComparisonWarning);
        Assert.Contains("unvollständig", viewModel.ComparisonWarning);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationOrModeChangeDoesNotPublishTraditionalResults(bool changeMode)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var viewModel = Create((_, _, token) =>
        {
            started.SetResult();
            if (!token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10))) throw new TimeoutException();
            token.ThrowIfCancellationRequested();
            throw new InvalidOperationException();
        });
        var running = viewModel.CalculateCommand.ExecuteAsync(null);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        if (changeMode) viewModel.SelectedTradition = viewModel.Traditions[0];
        else viewModel.CalculateCancelCommand.Execute(null);
        await running;
        Assert.Empty(viewModel.ComparisonRows);
        Assert.False(viewModel.IsBusy);
        Assert.Contains(changeMode ? "Eingaben geändert" : "abgebrochen", viewModel.StatusMessage);
    }

    [Fact]
    public async Task FailureClearsOldComparisonAndAllowsRetry()
    {
        var fail = false;
        var viewModel = Create((year, location, _) => fail ? throw new InvalidOperationException() : Result(year, location));
        await viewModel.CalculateCommand.ExecuteAsync(null);
        Assert.NotEmpty(viewModel.ComparisonRows);
        fail = true;
        await viewModel.CalculateCommand.ExecuteAsync(null);
        Assert.Empty(viewModel.ComparisonRows);
        Assert.Empty(viewModel.ReferenceSource);
        Assert.Contains("fehlgeschlagen", viewModel.StatusMessage);
        fail = false;
        await viewModel.CalculateCommand.ExecuteAsync(null);
        Assert.NotEmpty(viewModel.ComparisonRows);
    }

    private static MainWindowViewModel Create(Func<int, Location, CancellationToken, TraditionYearResult> calculate)
    {
        var viewModel = new MainWindowViewModel(new SyntheticYearCalculator(), new StubFastingCalculator(calculate),
            new EmbeddedCalendarReferenceProvider(), new CalendarComparisonService(), TestCalculationServices.Factory,
            new CalculationExporter(), TestCalculationServices.CrossViewModel, NullLogger<MainWindowViewModel>.Instance);
        viewModel.SelectedTradition = viewModel.Traditions[1];
        return viewModel;
    }

    private static TraditionYearResult Result(int year, Location location)
    {
        var reference = new EmbeddedCalendarReferenceProvider().Find(2026, LocationCatalog.DefaultLocations[0], Tradition.AnandaMarga)!;
        var dates = reference.FastingDates.Select((date, index) => new DateOnly(year, date.Month, date.Day).AddDays(index % 2 == 0 ? -1 : 0)).ToArray();
        return new(location, year, Tradition.AnandaMarga, AnandaMargaRule.Version, AnandaMargaRule.Source, false, dates, []);
    }

    private sealed class StubFastingCalculator(Func<int, Location, CancellationToken, TraditionYearResult> calculate) : IFastingCalendarCalculator
    {
        public TraditionYearResult Calculate(int year, Location location, CancellationToken cancellationToken = default) => calculate(year, location, cancellationToken);
        public TraditionYearResult Calculate(int year, Location location, Tradition tradition, CancellationToken cancellationToken = default) => calculate(year, location, cancellationToken) with { Tradition = tradition };
    }

    private sealed class SyntheticYearCalculator : IYearAstronomyCalculator
    {
        public YearAstronomyResult Calculate(int year, Location location, CancellationToken cancellationToken = default) =>
            TestCalculationServices.Year(year, location);
    }
}
