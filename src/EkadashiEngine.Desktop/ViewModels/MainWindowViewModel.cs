using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EkadashiEngine.Core;
using EkadashiEngine.Core.Astronomy;
using Microsoft.Extensions.Logging;

namespace EkadashiEngine.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IYearAstronomyCalculator _calculator;
    private readonly ILogger<MainWindowViewModel> _logger;
    private IReadOnlyList<ResultRow> _allResults = [];
    private int _inputVersion;

    public IReadOnlyList<Location> Locations => LocationCatalog.DefaultLocations;

    public IReadOnlyList<TraditionOption> Traditions { get; } =
    [
        new(Tradition.TechnicalSunriseCandidate, "Technisch: Tithi bei Sonnenaufgang")
    ];

    public int MinimumYear => YearAstronomyCalculator.MinimumYear;
    public int MaximumYear => YearAstronomyCalculator.MaximumYear;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CalculateCommand))]
    private Location? _selectedLocation;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CalculateCommand))]
    [NotifyPropertyChangedFor(nameof(YearValidationMessage))]
    [NotifyPropertyChangedFor(nameof(HasYearValidationError))]
    private decimal? _year = 2026;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CalculateCommand))]
    private TraditionOption? _selectedTradition;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CalculateCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private bool _showCandidatesOnly;

    [ObservableProperty]
    private IReadOnlyList<ResultRow> _results = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedResult))]
    private ResultRow? _selectedResult;

    [ObservableProperty]
    private string _statusMessage = "Ort und Jahr wählen, dann Berechnen starten.";

    [ObservableProperty]
    private string _resultSummary = "Noch keine Berechnung";

    public bool HasSelectedResult => SelectedResult is not null;
    public bool HasYearValidationError => !IsYearValid;
    public string YearValidationMessage => IsYearValid ? string.Empty : $"Bitte ein ganzes Jahr von {MinimumYear} bis {MaximumYear} eingeben.";
    private bool IsYearValid => Year is >= YearAstronomyCalculator.MinimumYear and <= YearAstronomyCalculator.MaximumYear
        && decimal.Truncate(Year.Value) == Year.Value;
    private bool CanCalculate => !IsBusy && IsYearValid && SelectedLocation is not null
        && SelectedTradition?.Tradition == Tradition.TechnicalSunriseCandidate;

    public MainWindowViewModel(IYearAstronomyCalculator calculator, ILogger<MainWindowViewModel> logger)
    {
        _calculator = calculator;
        _logger = logger;
        _selectedLocation = Locations[0];
        _selectedTradition = Traditions[0];
    }

    [RelayCommand(CanExecute = nameof(CanCalculate), IncludeCancelCommand = true)]
    private async Task CalculateAsync(CancellationToken cancellationToken)
    {
        if (!CanCalculate)
            return;

        var location = SelectedLocation!;
        var year = (int)Year!.Value;
        var inputVersion = _inputVersion;
        IsBusy = true;
        ClearResults();
        StatusMessage = $"Berechne {location.DisplayName} {year} …";
        try
        {
            _logger.LogInformation("Calculating technical candidates for {Location}, {Year}.", location.DisplayName, year);
            var calculation = await Task.Run(() => _calculator.Calculate(year, location, cancellationToken), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (inputVersion != _inputVersion)
                return;

            _allResults = calculation.Days.Select(day => new ResultRow(location, day, row => SelectedResult = row)).ToArray();
            UpdateVisibleRows();
            var candidates = calculation.Days.Count(day => day.IsEkadashiCandidate == true);
            var unavailable = calculation.Days.Count(day => day.IsEkadashiCandidate is null);
            ResultSummary = $"{location.DisplayName} · {year} · {calculation.Days.Count} Tage · {candidates} Kandidaten · {unavailable} nicht auswertbar";
            StatusMessage = "Berechnung abgeschlossen. Eine Zeile oder Details öffnet die astronomischen Werte.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (inputVersion == _inputVersion)
                StatusMessage = "Berechnung abgebrochen. Es wurde keine unvollständige Jahresliste übernommen.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Year calculation failed for {Location}, {Year}.", location.DisplayName, year);
            if (inputVersion == _inputVersion)
                StatusMessage = "Berechnung fehlgeschlagen. Bitte Eingaben prüfen und erneut versuchen. Technische Details stehen im Log.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CloseDetails() => SelectedResult = null;

    partial void OnSelectedLocationChanged(Location? value) => InputsChanged();
    partial void OnYearChanged(decimal? value) => InputsChanged();
    partial void OnSelectedTraditionChanged(TraditionOption? value) => InputsChanged();
    partial void OnShowCandidatesOnlyChanged(bool value) => UpdateVisibleRows();

    private void InputsChanged()
    {
        _inputVersion++;
        CalculateCommand.Cancel();
        ClearResults();
        StatusMessage = "Eingaben geändert. Bitte neu berechnen.";
    }

    private void ClearResults()
    {
        _allResults = [];
        Results = [];
        SelectedResult = null;
        ResultSummary = "Noch keine Berechnung";
    }

    private void UpdateVisibleRows()
    {
        SelectedResult = null;
        Results = ShowCandidatesOnly ? _allResults.Where(row => row.Day.IsEkadashiCandidate == true).ToArray() : _allResults;
    }
}
