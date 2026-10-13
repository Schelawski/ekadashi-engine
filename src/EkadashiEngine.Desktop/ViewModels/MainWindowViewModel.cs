using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EkadashiEngine.Core;
using Microsoft.Extensions.Logging;

namespace EkadashiEngine.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    public IReadOnlyList<Location> Locations { get; } =
    [
        new("Berlin", "DE", 52.5200, 13.4050, "Europe/Berlin")
    ];

    public IReadOnlyList<TraditionOption> Traditions { get; } =
    [
        new(Tradition.AnandaMarga, "Ananda Marga"),
        new(Tradition.GaudiyaVaishnava, "Gaudiya Vaishnava / ISKCON (später)")
    ];

    [ObservableProperty]
    private Location _selectedLocation;

    [ObservableProperty]
    private int _year = 2026;

    [ObservableProperty]
    private TraditionOption _selectedTradition;

    public ObservableCollection<ResultRow> Results { get; } = [];

    public MainWindowViewModel(ILogger<MainWindowViewModel> logger)
    {
        _selectedLocation = Locations[0];
        _selectedTradition = Traditions[0];
        logger.LogInformation("Main view prepared for {Location}, {Year}.", SelectedLocation.DisplayName, Year);
    }
}
