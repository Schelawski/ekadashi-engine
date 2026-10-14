using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EkadashiEngine.Core;
using EkadashiEngine.Core.Astronomy;
using EkadashiEngine.Core.Comparison;
using Microsoft.Extensions.Logging;

namespace EkadashiEngine.Desktop.ViewModels;

public sealed partial class LocationChoice(Location location, bool isSelected) : ObservableObject
{
    public Location Location { get; } = location;
    public string Name => Location.DisplayName;
    [ObservableProperty] private bool _isSelected = isSelected;
}

public sealed record LocationSummaryRow(string Name, string Coordinates, string TimeZone, int SelectedDays, int UnknownDays);
public sealed record LocationDayRow(string Name, string Coordinates, string TimeZone, string SunriseLocal, string SunriseUtc, string Tithi, string Selection);
public sealed record LocationComparisonDay(CrossLocationDay Data)
{
    public string Label => Data.LocalDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        + (Data.HasDifferences ? " · unterschiedlich" : string.Empty) + (Data.HasUnknown ? " · ungeklärt" : string.Empty);
}

public sealed partial class CrossLocationViewModel : ObservableObject
{
    private readonly ICrossLocationCalculator _calculator;
    private readonly ILogger<CrossLocationViewModel> _logger;
    private int _inputVersion;
    private IReadOnlyList<LocationComparisonDay> _allDays = [];
    public ObservableCollection<LocationChoice> Locations { get; } = [];
    public LocationEditorViewModel LocationEditor { get; } = new();
    public IReadOnlyList<TraditionOption> Traditions { get; } =
    [new(Tradition.TechnicalSunriseCandidate, "Technisch: Sonnenaufgangskandidaten"), new(Tradition.AnandaMarga, "Ananda Marga: berechnete Fastentage")];

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(CalculateCommand))] [NotifyPropertyChangedFor(nameof(ValidationMessage))]
    private decimal? _year = 2026;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(CalculateCommand))] private TraditionOption? _selectedTradition;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(CalculateCommand))] [NotifyCanExecuteChangedFor(nameof(AddLocationCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectAllCommand))] private bool _isBusy;
    [ObservableProperty] private bool _showDifferencesOnly;
    [ObservableProperty] private IReadOnlyList<LocationSummaryRow> _summaries = [];
    [ObservableProperty] private IReadOnlyList<LocationComparisonDay> _days = [];
    [ObservableProperty] private LocationComparisonDay? _selectedDay;
    [ObservableProperty] private IReadOnlyList<LocationDayRow> _dayRows = [];
    [ObservableProperty] private string _statusMessage = "Mindestens zwei Orte auswählen und dasselbe Jahr vergleichen.";
    [ObservableProperty] private string _summary = "Noch kein Ortsvergleich";

    public string ValidationMessage => Year is < 1900 or > 2100 or null || decimal.Truncate(Year.Value) != Year.Value
        ? "Bitte ein ganzes Jahr von 1900 bis 2100 eingeben." : Locations.Count(choice => choice.IsSelected) < 2
        ? "Mindestens zwei Orte auswählen." : string.Empty;
    public string ModeDescription => SelectedTradition?.Tradition == Tradition.AnandaMarga
        ? "Ja/Nein bezieht sich auf die dokumentierte AM-Regel; diese ist mit offiziellen Kalendern noch nicht validiert."
        : "Ja/Nein bezeichnet Tithi 11 oder 26 am örtlichen Sonnenaufgang; das sind technische Kandidaten.";
    private bool CanCalculate => !IsBusy && ValidationMessage.Length == 0 && SelectedTradition is not null;
    private bool CanAddLocation => !IsBusy && LocationEditor.IsValid;
    private bool CanSelectAll => !IsBusy;

    public CrossLocationViewModel(ICrossLocationCalculator calculator, ILogger<CrossLocationViewModel> logger)
    {
        _calculator = calculator; _logger = logger;
        _selectedTradition = Traditions[0];
        foreach (var location in LocationCatalog.DefaultLocations)
            AddChoice(location, location.DisplayName is "Berlin" or "Delhi" or "New York" or "Sydney");
        LocationEditor.Changed += () => AddLocationCommand.NotifyCanExecuteChanged();
    }

    public void AddLocation(Location location)
    {
        var existing = Locations.FirstOrDefault(choice => LocationKey.From(choice.Location) == LocationKey.From(location));
        if (existing is not null)
        {
            existing.IsSelected = true;
            StatusMessage = "Ein Ort mit diesen Koordinaten und dieser Zeitzone ist bereits vorhanden und ausgewählt.";
            return;
        }
        AddChoice(location, true);
        InputsChanged();
    }

    [RelayCommand(CanExecute = nameof(CanAddLocation))]
    private void AddLocation() => AddLocation(LocationEditor.Location!);

    [RelayCommand(CanExecute = nameof(CanSelectAll))]
    private void SelectAll()
    {
        foreach (var choice in Locations) choice.IsSelected = true;
    }

    private void AddChoice(Location location, bool selected)
    {
        var choice = new LocationChoice(location, selected);
        choice.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(LocationChoice.IsSelected)) InputsChanged(); };
        Locations.Add(choice);
    }

    [RelayCommand(CanExecute = nameof(CanCalculate), IncludeCancelCommand = true)]
    private async Task CalculateAsync(CancellationToken cancellationToken)
    {
        if (!CanCalculate) return;
        var version = _inputVersion;
        var locations = Locations.Where(choice => choice.IsSelected).Select(choice => choice.Location).ToArray();
        var year = (int)Year!.Value;
        var tradition = SelectedTradition!.Tradition;
        IsBusy = true;
        ClearResults();
        StatusMessage = $"Vergleiche {locations.Length} Orte für {year} …";
        var progress = new Progress<CrossLocationProgress>(value =>
        {
            if (version == _inputVersion && IsBusy) StatusMessage = $"{value.Completed}/{value.Total} Orte berechnet · {value.LocationName}";
        });
        try
        {
            var result = await Task.Run(() => _calculator.Calculate(year, locations, tradition, cancellationToken, progress), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (version != _inputVersion) return;
            _allDays = result.Days.Select(day => new LocationComparisonDay(day)).ToArray();
            Summaries = result.Locations.Select((location, i) => new LocationSummaryRow(location.Astronomy.Location.DisplayName,
                Coordinates(location.Astronomy.Location), location.Astronomy.Location.TimeZoneId,
                result.Days.Count(day => day.Locations[i].Selected == true), result.Days.Count(day => day.Locations[i].Selected is null))).ToArray();
            UpdateDays();
            Summary = $"{year} · {locations.Length} Orte · {result.Days.Count} lokale Daten · {result.Days.Count(day => day.HasDifferences)} Tage mit unterschiedlichen Entscheidungen · {result.Days.Count(day => day.HasUnknown)} ungeklärte Tage";
            StatusMessage = "Vergleich abgeschlossen. Ein lokales Datum auswählen, um die Werte aller Orte zu sehen.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (version == _inputVersion) StatusMessage = "Vergleich abgebrochen. Kein Teilergebnis übernommen.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Cross-location comparison failed for {Year}.", year);
            if (version == _inputVersion) StatusMessage = "Vergleich fehlgeschlagen. Eingaben prüfen und erneut versuchen.";
        }
        finally { IsBusy = false; }
    }

    partial void OnYearChanged(decimal? value) => InputsChanged();
    partial void OnSelectedTraditionChanged(TraditionOption? value) { InputsChanged(); OnPropertyChanged(nameof(ModeDescription)); }
    partial void OnShowDifferencesOnlyChanged(bool value) => UpdateDays();
    partial void OnSelectedDayChanged(LocationComparisonDay? value) => DayRows = value?.Data.Locations.Select(day => new LocationDayRow(
        day.Location.DisplayName, Coordinates(day.Location), day.Location.TimeZoneId,
        day.Astronomy.Sunrise.SunriseLocal?.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture) ?? day.Astronomy.Sunrise.Status.ToString(),
        day.Astronomy.Sunrise.SunriseUtc?.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture) ?? "—",
        day.Astronomy.Tithi?.TithiNumber.ToString(CultureInfo.InvariantCulture) ?? "—",
        day.Selected switch { true => "Ja", false => "Nein", null => "Ungeklärt" })).ToArray() ?? [];

    private void InputsChanged()
    {
        _inputVersion++;
        CalculateCommand.Cancel();
        ClearResults();
        OnPropertyChanged(nameof(ValidationMessage)); CalculateCommand.NotifyCanExecuteChanged();
        StatusMessage = "Eingaben geändert. Bitte neu vergleichen.";
    }
    private void ClearResults() { _allDays = []; Days = []; Summaries = []; SelectedDay = null; DayRows = []; Summary = "Noch kein Ortsvergleich"; }
    private void UpdateDays()
    {
        var previous = SelectedDay?.Data.LocalDate;
        Days = ShowDifferencesOnly ? _allDays.Where(day => day.Data.HasDifferences || day.Data.HasUnknown).ToArray() : _allDays;
        SelectedDay = Days.FirstOrDefault(day => day.Data.LocalDate == previous) ?? Days.FirstOrDefault(day => day.Data.HasDifferences) ?? Days.FirstOrDefault();
    }
    private static string Coordinates(Location location) => FormattableString.Invariant($"{location.Latitude:F5}, {location.Longitude:F5}");
}
