using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EkadashiEngine.Core;
using EkadashiEngine.Core.Astronomy;
using EkadashiEngine.Core.ReferenceData;
using EkadashiEngine.Core.Rules;
using EkadashiEngine.Core.Diagnostics;
using EkadashiEngine.Core.Export;
using Microsoft.Extensions.Logging;

namespace EkadashiEngine.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IYearAstronomyCalculator _calculator;
    private readonly IFastingCalendarCalculator _fastingCalculator;
    private readonly ICalendarReferenceProvider _references;
    private readonly CalendarComparisonService _comparisonService;
    private readonly CalculationSnapshotFactory _snapshotFactory;
    private readonly CalculationExporter _exporter;
    private readonly ILogger<MainWindowViewModel> _logger;
    private IReadOnlyList<ResultRow> _allResults = [];
    private IReadOnlyList<ComparisonRow> _allComparisonRows = [];
    private int _inputVersion;

    public IReadOnlyList<Location> Locations => LocationCatalog.DefaultLocations;
    public LocationEditorViewModel LocationEditor { get; } = new();
    public CrossLocationViewModel CrossLocation { get; }
    public Location? EffectiveLocation => UseCustomLocation ? LocationEditor.Location : SelectedLocation;
    public bool CanCompareLocations => !IsBusy && !IsExporting && IsYearValid && EffectiveLocation is not null
        && SelectedTradition?.Tradition is Tradition.TechnicalSunriseCandidate or Tradition.AnandaMarga or Tradition.GaudiyaVaishnava;
    public string EffectiveCoordinates => EffectiveLocation is { } location
        ? FormattableString.Invariant($"{location.Latitude:F5}°, {location.Longitude:F5}°") : "Ungültige Standortangaben";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CalculateCommand))]
    private bool _useCustomLocation;

    public IReadOnlyList<TraditionOption> Traditions => TraditionOptions.All;

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
    [NotifyPropertyChangedFor(nameof(CanExport))]
    [NotifyPropertyChangedFor(nameof(CanExportCsv))]
    [NotifyPropertyChangedFor(nameof(CanCompareLocations))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CalculateCommand))]
    [NotifyPropertyChangedFor(nameof(CanExport))]
    [NotifyPropertyChangedFor(nameof(CanExportCsv))]
    [NotifyPropertyChangedFor(nameof(CanCompareLocations))]
    private bool _isExporting;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanExport))]
    [NotifyPropertyChangedFor(nameof(CanExportCsv))]
    private CalculationSnapshot? _snapshot;

    [ObservableProperty]
    private bool _showCandidatesOnly;

    [ObservableProperty]
    private bool _showMismatchesOnly;

    [ObservableProperty]
    private IReadOnlyList<ComparisonRow> _comparisonRows = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedComparisonRow))]
    [NotifyPropertyChangedFor(nameof(ShowComparisonOverview))]
    private ComparisonRow? _selectedComparisonRow;

    [ObservableProperty]
    private string _comparisonSummary = "Noch kein Kalendervergleich";

    [ObservableProperty]
    private string _referenceDescription = "Referenzen für Berlin 2026: AM Berlin Sector und Vaishnava GCal 11.";

    [ObservableProperty]
    private string _referenceSource = string.Empty;

    [ObservableProperty]
    private string _comparisonWarning = string.Empty;

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
    public bool CanExport => Snapshot is not null && !IsBusy && !IsExporting;
    public bool CanExportCsv => CanExport && Snapshot!.Comparison is not null;
    public bool HasSelectedComparisonRow => SelectedComparisonRow is not null;
    public bool ShowComparisonOverview => IsTraditionalMode && !HasSelectedComparisonRow;
    public bool IsTraditionalMode => SelectedTradition?.Tradition is Tradition.AnandaMarga or Tradition.GaudiyaVaishnava;
    public string ModeDescription => TraditionOptions.Description(SelectedTradition?.Tradition);
    public bool HasYearValidationError => !IsYearValid;
    public string YearValidationMessage => IsYearValid ? string.Empty : $"Bitte ein ganzes Jahr von {MinimumYear} bis {MaximumYear} eingeben.";
    private bool IsYearValid => Year is >= YearAstronomyCalculator.MinimumYear and <= YearAstronomyCalculator.MaximumYear
        && decimal.Truncate(Year.Value) == Year.Value;
    private bool CanCalculate => !IsBusy && !IsExporting && IsYearValid && EffectiveLocation is not null
        && SelectedTradition?.Tradition is Tradition.TechnicalSunriseCandidate or Tradition.AnandaMarga or Tradition.GaudiyaVaishnava;

    public MainWindowViewModel(IYearAstronomyCalculator calculator, IFastingCalendarCalculator fastingCalculator,
        ICalendarReferenceProvider references, CalendarComparisonService comparisonService, CalculationSnapshotFactory snapshotFactory,
        CalculationExporter exporter, CrossLocationViewModel crossLocation, ILogger<MainWindowViewModel> logger)
    {
        _calculator = calculator;
        _fastingCalculator = fastingCalculator;
        _references = references;
        _comparisonService = comparisonService;
        _snapshotFactory = snapshotFactory;
        _exporter = exporter;
        CrossLocation = crossLocation;
        _logger = logger;
        _selectedLocation = Locations[0];
        _selectedTradition = Traditions[0];
        LocationEditor.Load(_selectedLocation);
        LocationEditor.Changed += () => { if (UseCustomLocation) { InputsChanged(); NotifyLocation(); } };
    }

    [RelayCommand(CanExecute = nameof(CanCalculate), IncludeCancelCommand = true)]
    private async Task CalculateAsync(CancellationToken cancellationToken)
    {
        if (!CanCalculate)
            return;

        var location = EffectiveLocation!;
        var year = (int)Year!.Value;
        var tradition = SelectedTradition!.Tradition;
        var inputVersion = _inputVersion;
        IsBusy = true;
        ClearResults();
        StatusMessage = $"Berechne {location.DisplayName} {year} …";
        try
        {
            _logger.LogInformation("Calculating {Tradition} for {Location}, {Year}.", tradition, location.DisplayName, year);
            if (tradition != Tradition.TechnicalSunriseCandidate)
            {
                var calculated = await Task.Run(() =>
                {
                    var calendar = _fastingCalculator.Calculate(year, location, tradition, cancellationToken);
                    var other = _fastingCalculator.Calculate(year, location, tradition == Tradition.AnandaMarga ? Tradition.GaudiyaVaishnava : Tradition.AnandaMarga, cancellationToken);
                    var astronomy = _calculator.Calculate(year, location, cancellationToken);
                    var reference = _references.Find(year, location, calendar.Tradition);
                    var comparison = _comparisonService.Compare(calendar, reference);
                    return (Snapshot: _snapshotFactory.Create(astronomy, calendar, comparison, cancellationToken), Other: other);
                }, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (inputVersion != _inputVersion) return;
                PublishComparison(calculated.Snapshot, calculated.Other);
                Snapshot = calculated.Snapshot;
                StatusMessage = "Kalendervergleich abgeschlossen. Eine Zeile öffnet die Regelentscheidung.";
                return;
            }
            var calculation = await Task.Run(() => _calculator.Calculate(year, location, cancellationToken), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (inputVersion != _inputVersion)
                return;

            var technicalSnapshot = _snapshotFactory.Create(calculation, cancellationToken: cancellationToken);
            _allResults = calculation.Days.Select(day => new ResultRow(location, day, row => SelectedResult = row)).ToArray();
            Snapshot = technicalSnapshot;
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

    [RelayCommand]
    private void CloseComparisonDetails() => SelectedComparisonRow = null;

    partial void OnSelectedLocationChanged(Location? value)
    {
        if (value is not null) LocationEditor.Load(value);
        InputsChanged(); NotifyLocation();
    }
    partial void OnUseCustomLocationChanged(bool value) { InputsChanged(); NotifyLocation(); }
    partial void OnYearChanged(decimal? value) { InputsChanged(); OnPropertyChanged(nameof(CanCompareLocations)); }
    partial void OnSelectedTraditionChanged(TraditionOption? value)
    {
        InputsChanged();
        OnPropertyChanged(nameof(IsTraditionalMode));
        OnPropertyChanged(nameof(ShowComparisonOverview));
        OnPropertyChanged(nameof(ModeDescription));
        OnPropertyChanged(nameof(CanCompareLocations));
    }
    partial void OnShowCandidatesOnlyChanged(bool value) => UpdateVisibleRows();
    partial void OnShowMismatchesOnlyChanged(bool value) => UpdateComparisonRows();

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
        Snapshot = null;
        Results = [];
        SelectedResult = null;
        ResultSummary = "Noch keine Berechnung";
        _allComparisonRows = [];
        ComparisonRows = [];
        SelectedComparisonRow = null;
        ComparisonSummary = "Noch kein Kalendervergleich";
        ReferenceDescription = "Referenzen für Berlin 2026: AM Berlin Sector und Vaishnava GCal 11.";
        ReferenceSource = string.Empty;
        ComparisonWarning = string.Empty;
    }

    private void UpdateVisibleRows()
    {
        SelectedResult = null;
        Results = ShowCandidatesOnly ? _allResults.Where(row => row.Day.IsEkadashiCandidate == true).ToArray() : _allResults;
    }

    private void PublishComparison(CalculationSnapshot snapshot, TraditionYearResult other)
    {
        var calendar = snapshot.Calendar!;
        var comparison = snapshot.Comparison!;
        var reference = comparison.Reference;
        _allComparisonRows = snapshot.Diagnostics.Select(diagnostic => new ComparisonRow(diagnostic, calendar, other)).ToArray();
        UpdateComparisonRows();
        ResultSummary = $"{calendar.Location.DisplayName} · {calendar.Year} · {calendar.FastingDates.Count} berechnete Fastentage";
        ComparisonSummary = reference is null
            ? $"{calendar.FastingDates.Count} berechnete Tage · Referenztage, Matches, Mismatches und Trefferquote: nicht verfügbar"
            : $"{comparison.ReferenceCount} Referenztage · {comparison.Matches} Matches · {comparison.Mismatches} Mismatches · {comparison.UnexpectedCalculated} zusätzlich berechnet · Trefferquote {comparison.MatchRate:F1} %";
        ReferenceDescription = reference is null
            ? $"Keine passenden Referenzdaten für {calendar.Location.DisplayName} {calendar.Year} vorhanden. Verfügbar: Berlin 2026 je Tradition."
            : $"{reference.Title} · Vergleichsort {reference.ComparisonLocation.DisplayName} · abgerufen {reference.RetrievedOn:yyyy-MM-dd}. {reference.Scope}";
        ReferenceSource = reference?.Source ?? string.Empty;
        ComparisonWarning = (calendar.Tradition == Tradition.AnandaMarga ? "Die dokumentierte AM-Methode weicht vom offiziellen Kalender ab." : "Validierungsumfang: Berlin 2026. Andere Orte und Jahre sind nicht allgemein freigegeben.")
            + (calendar.HasUnresolvedDecisions ? $" {calendar.Decisions.Count(decision => decision.Status != RuleDecisionStatus.Selected)} Regelentscheidungen sind ungeklärt; die berechnete Jahresliste ist unvollständig." : string.Empty);
    }

    private void UpdateComparisonRows()
    {
        SelectedComparisonRow = null;
        ComparisonRows = ShowMismatchesOnly ? _allComparisonRows.Where(row => row.IsMismatch).ToArray() : _allComparisonRows;
    }

    public ExportPayload CreateExport(ExportFormat format)
    {
        if (!CanExport || (format == ExportFormat.Csv && !CanExportCsv))
            throw new InvalidOperationException("No completed calculation is available for this export.");
        return _exporter.Create(Snapshot!, format);
    }

    public void ReportExportFailure(Exception exception, bool updateStatus = true)
    {
        _logger.LogError(exception, "Export failed.");
        if (updateStatus) StatusMessage = "Export fehlgeschlagen. Bitte Ziel und Schreibrechte prüfen und erneut versuchen.";
    }

    private void NotifyLocation()
    {
        OnPropertyChanged(nameof(EffectiveLocation)); OnPropertyChanged(nameof(EffectiveCoordinates));
        OnPropertyChanged(nameof(CanCompareLocations)); CalculateCommand.NotifyCanExecuteChanged();
    }

    public void PrepareCrossLocationComparison()
    {
        if (!CanCompareLocations) return;
        var location = EffectiveLocation!;
        if (IsYearValid) CrossLocation.Year = Year;
        CrossLocation.SelectedTradition = CrossLocation.Traditions.First(option => option.Tradition == SelectedTradition!.Tradition);
        CrossLocation.AddLocation(location);
    }
}
