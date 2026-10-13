using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EkadashiEngine.Desktop;
using EkadashiEngine.Desktop.ViewModels;
using EkadashiEngine.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace EkadashiEngine.Tests;

public sealed class DesktopSmokeTests
{
    [AvaloniaFact]
    public async Task BerlinYearCanBeCalculatedFilteredAndInspectedThroughBoundControls()
    {
        using var services = ServiceConfiguration.CreateServiceProvider();
        var viewModel = services.GetRequiredService<MainWindowViewModel>();
        var window = new MainWindow { DataContext = viewModel };
        try
        {
            window.Show();
            var button = window.FindControl<Button>("CalculateButton")!;
            Assert.Same(viewModel.CalculateCommand, button.Command);
            button.Command!.Execute(null);
            await viewModel.CalculateCommand.ExecutionTask!;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(365, viewModel.Results.Count);
            Assert.False(viewModel.IsBusy);
            var table = window.FindControl<DataGrid>("ResultsTable")!;
            Assert.Same(viewModel.Results, table.ItemsSource);
            Assert.Contains("Berlin · 2026 · 365 Tage", window.FindControl<TextBlock>("ResultSummaryText")!.Text);

            window.FindControl<CheckBox>("CandidateFilter")!.IsChecked = true;
            Dispatcher.UIThread.RunJobs();
            Assert.NotEmpty(viewModel.Results);
            Assert.All(viewModel.Results, row => Assert.True(row.Day.IsEkadashiCandidate));
            Assert.True(viewModel.Results.Count < 365);
            table.UpdateLayout();
            var detailsButton = table.GetVisualDescendants().OfType<Button>().First(control => Equals(control.Content, "Details"));
            detailsButton.Command!.Execute(null);
            Dispatcher.UIThread.RunJobs();
            Assert.NotNull(viewModel.SelectedResult);
            Assert.True(window.FindControl<Border>("DetailsPanel")!.IsVisible);
            var text = window.GetVisualDescendants().OfType<TextBlock>().Select(control => control.Text).ToArray();
            Assert.Contains(viewModel.SelectedResult.SunriseUtc, text);
            Assert.Contains(viewModel.SelectedResult.SunLongitude, text);
            Assert.Contains(viewModel.SelectedResult.SunriseLocal, text);

            if (Environment.GetEnvironmentVariable("EKADASHI_TEST_SCREENSHOT") is { Length: > 0 } screenshot)
            {
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                using var frame = window.CaptureRenderedFrame();
                Assert.NotNull(frame);
                frame.Save(screenshot, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
            }

            viewModel.CloseDetailsCommand.Execute(null);
            Assert.Null(viewModel.SelectedResult);
            window.FindControl<ComboBox>("LocationSelector")!.SelectedIndex = 2;
            Dispatcher.UIThread.RunJobs();
            Assert.Empty(viewModel.Results);
            Assert.False(window.FindControl<Border>("DetailsPanel")!.IsVisible);

            window.FindControl<NumericUpDown>("YearInput")!.Value = null;
            Dispatcher.UIThread.RunJobs();
            Assert.Null(viewModel.Year);
            Assert.False(viewModel.CalculateCommand.CanExecute(null));
            Assert.False(button.IsEffectivelyEnabled);
            Assert.True(window.FindControl<TextBlock>("YearValidationText")!.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

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
            Assert.Equal("Berlin", viewModel.SelectedLocation!.DisplayName);
            Assert.Equal(9, viewModel.Locations.Count);
            Assert.Equal(2026m, window.FindControl<NumericUpDown>("YearInput")!.Value);
            Assert.Same(viewModel.SelectedLocation, window.FindControl<ComboBox>("LocationSelector")!.SelectedItem);
            Assert.Equal("Europe/Berlin", window.FindControl<TextBlock>("TimeZoneText")!.Text);
            Assert.Same(viewModel.SelectedTradition, window.FindControl<ComboBox>("TraditionSelector")!.SelectedItem);
            Assert.True(window.FindControl<Button>("CalculateButton")!.IsEnabled);
            Assert.Same(viewModel.Results, window.FindControl<DataGrid>("ResultsTable")!.ItemsSource);
            Assert.Empty(viewModel.Results);

            window.FindControl<NumericUpDown>("YearInput")!.Value = 2027;
            window.FindControl<ComboBox>("LocationSelector")!.SelectedIndex = 2;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(2027m, viewModel.Year);
            Assert.Same(viewModel.Locations[2], viewModel.SelectedLocation);
            Assert.Equal("Asia/Kolkata", window.FindControl<TextBlock>("TimeZoneText")!.Text);
        }
        finally
        {
            window.Close();
        }
    }
}
