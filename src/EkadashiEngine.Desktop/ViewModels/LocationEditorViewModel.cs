using CommunityToolkit.Mvvm.ComponentModel;
using EkadashiEngine.Core;
using NodaTime;

namespace EkadashiEngine.Desktop.ViewModels;

public sealed partial class LocationEditorViewModel : ObservableObject
{
    private bool _loading;
    public event Action? Changed;
    public IReadOnlyList<string> TimeZoneIds { get; } = DateTimeZoneProviders.Tzdb.Ids.Order().ToArray();
    [ObservableProperty] private string _displayName = "Berlin";
    [ObservableProperty] private string _country = "DE";
    [ObservableProperty] private decimal? _latitude = 52.52m;
    [ObservableProperty] private decimal? _longitude = 13.405m;
    [ObservableProperty] private string? _timeZoneId = "Europe/Berlin";

    public string ValidationMessage =>
        string.IsNullOrWhiteSpace(DisplayName) ? "Bitte einen Ortsnamen eingeben." :
        string.IsNullOrWhiteSpace(Country) ? "Bitte ein Länderkennzeichen eingeben." :
        Latitude is null or < -90 or > 90 ? "Breite muss zwischen −90 und +90 liegen." :
        Longitude is null or < -180 or > 180 ? "Länge muss zwischen −180 und +180 liegen." :
        string.IsNullOrWhiteSpace(TimeZoneId) || DateTimeZoneProviders.Tzdb.GetZoneOrNull(TimeZoneId.Trim()) is null
            ? "Bitte eine gültige IANA-Zeitzone wählen oder eingeben." : string.Empty;
    public bool IsValid => ValidationMessage.Length == 0;
    public Location? Location => IsValid
        ? new(DisplayName.Trim(), Country.Trim(), (double)Latitude!.Value, (double)Longitude!.Value, TimeZoneId!.Trim()) : null;

    public void Load(Location location)
    {
        _loading = true;
        try
        {
            DisplayName = location.DisplayName; Country = location.Country;
            Latitude = (decimal)location.Latitude; Longitude = (decimal)location.Longitude; TimeZoneId = location.TimeZoneId;
        }
        finally { _loading = false; }
        NotifyChanged();
    }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (!_loading && e.PropertyName is nameof(DisplayName) or nameof(Country) or nameof(Latitude) or nameof(Longitude) or nameof(TimeZoneId))
            NotifyChanged();
    }

    private void NotifyChanged()
    {
        OnPropertyChanged(nameof(ValidationMessage)); OnPropertyChanged(nameof(IsValid)); OnPropertyChanged(nameof(Location));
        Changed?.Invoke();
    }
}
