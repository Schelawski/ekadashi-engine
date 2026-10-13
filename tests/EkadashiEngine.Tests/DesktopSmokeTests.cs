using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using EkadashiEngine.Desktop;
using EkadashiEngine.Desktop.ViewModels;
using EkadashiEngine.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace EkadashiEngine.Tests;

public sealed class DesktopSmokeTests
{
    [AvaloniaFact]
    public void MainWindowOpensWithInjectedViewModelAndEmptyResults()
    {
        using var services = ServiceConfiguration.CreateServiceProvider();
        var viewModel = services.GetRequiredService<MainWindowViewModel>();
        var window = new MainWindow { DataContext = viewModel };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.True(window.IsVisible);
            Assert.Equal("Berlin", viewModel.SelectedLocation.DisplayName);
            Assert.Equal(2026m, window.FindControl<NumericUpDown>("YearInput")!.Value);
            Assert.Same(viewModel.SelectedLocation, window.FindControl<ComboBox>("LocationSelector")!.SelectedItem);
            Assert.Equal("Europe/Berlin", window.FindControl<TextBlock>("TimeZoneText")!.Text);
            Assert.Same(viewModel.SelectedTradition, window.FindControl<ComboBox>("TraditionSelector")!.SelectedItem);
            Assert.False(window.FindControl<Button>("CalculateButton")!.IsEnabled);
            Assert.Same(viewModel.Results, window.FindControl<DataGrid>("ResultsTable")!.ItemsSource);
            Assert.Empty(viewModel.Results);

            window.FindControl<NumericUpDown>("YearInput")!.Value = 2027;
            window.FindControl<ComboBox>("TraditionSelector")!.SelectedIndex = 1;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(2027, viewModel.Year);
            Assert.Same(viewModel.Traditions[1], viewModel.SelectedTradition);
        }
        finally
        {
            window.Close();
        }
    }
}
