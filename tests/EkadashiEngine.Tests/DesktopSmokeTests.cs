using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EkadashiEngine.Desktop;
using EkadashiEngine.Desktop.ViewModels;
using EkadashiEngine.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using EkadashiEngine.Core.Export;
using System.Text.Json;

namespace EkadashiEngine.Tests;

public sealed class DesktopSmokeTests
{
    [AvaloniaFact]
    public async Task PendingExportDisablesActionsAndDoesNotReplaceStatusAfterInputChanges()
    {
        using var services = ServiceConfiguration.CreateServiceProvider();
        var viewModel = services.GetRequiredService<MainWindowViewModel>();
        var window = new MainWindow { DataContext = viewModel };
        try
        {
            window.Show();
            Assert.False(viewModel.CanExport);
            await viewModel.CalculateCommand.ExecuteAsync(null);
            var destination = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var exporting = window.ExportAsync(ExportFormat.Json, _ => destination.Task);
            Dispatcher.UIThread.RunJobs();
            Assert.True(viewModel.IsExporting);
            Assert.False(window.FindControl<Button>("ExportJsonButton")!.IsEnabled);
            Assert.False(viewModel.CalculateCommand.CanExecute(null));
            viewModel.Year = 2027;
            destination.SetResult(true);
            await exporting;
            Assert.False(viewModel.IsExporting);
            Assert.False(viewModel.CanExport);
            Assert.Contains("Eingaben geändert", viewModel.StatusMessage);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task OfficialCalendarComparisonCanBeCalculatedFilteredAndInspected()
    {
        using var services = ServiceConfiguration.CreateServiceProvider();
        var viewModel = services.GetRequiredService<MainWindowViewModel>();
        var window = new MainWindow { DataContext = viewModel };
        try
        {
            window.Show();
            window.FindControl<ComboBox>("TraditionSelector")!.SelectedIndex = 1;
            Dispatcher.UIThread.RunJobs();
            Assert.True(viewModel.IsAnandaMargaMode);
            Assert.True(window.FindControl<Grid>("ComparisonPanel")!.IsVisible);
            window.FindControl<Button>("CalculateButton")!.Command!.Execute(null);
            await viewModel.CalculateCommand.ExecutionTask!;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(24, viewModel.ComparisonRows.Count);
            Assert.Contains("12 Matches", window.FindControl<TextBlock>("ComparisonSummaryText")!.Text);
            Assert.Contains("12 Mismatches", window.FindControl<TextBlock>("ComparisonSummaryText")!.Text);
            Assert.Contains("50", window.FindControl<TextBlock>("ComparisonSummaryText")!.Text);
            Assert.Contains("anandamarga.eu", window.FindControl<SelectableTextBlock>("ReferenceSourceText")!.Text);
            var table = window.FindControl<DataGrid>("ComparisonTable")!;
            Assert.Same(viewModel.ComparisonRows, table.ItemsSource);
            table.UpdateLayout();
            var statusText = table.GetVisualDescendants().OfType<TextBlock>().First(control => control.Text == "MISMATCH");
            Assert.Equal(Avalonia.Media.Color.Parse("#A52B26"), ((Avalonia.Media.ISolidColorBrush)statusText.Foreground!).Color);
            var matchText = table.GetVisualDescendants().OfType<TextBlock>().First(control => control.Text == "MATCH");
            Assert.NotEqual(statusText.Foreground, matchText.Foreground);
            table.SelectedItem = viewModel.ComparisonRows[0];
            Dispatcher.UIThread.RunJobs();
            Assert.True(window.FindControl<Border>("ComparisonDetailsPanel")!.IsVisible);
            Assert.Contains("Tithi 26", viewModel.SelectedComparisonRow!.Diagnosis);
            var diagnosis = window.FindControl<SelectableTextBlock>("ComparisonDiagnosisText")!.Text!;
            foreach (var required in new[] { "2026-01-13", "2026-01-14", "Sonnenaufgang lokal", "UTC", "Tithi bei Sonnenaufgang", "Ekadashi-Start lokal", "Ekadashi-Ende lokal", "Sonnenlänge", "Mondlänge", "Winkeldifferenz", "am-max-overlap-v1" })
                Assert.Contains(required, diagnosis);
            Assert.True(window.FindControl<Button>("ExportJsonButton")!.IsEnabled);
            Assert.True(window.FindControl<Button>("ExportCsvButton")!.IsEnabled);
            Assert.True(window.FindControl<Button>("ExportVectorsButton")!.IsEnabled);

            if (Environment.GetEnvironmentVariable("EKADASHI_COMPARISON_SCREENSHOT") is { Length: > 0 } screenshot)
            {
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                using var frame = window.CaptureRenderedFrame();
                Assert.NotNull(frame);
                frame.Save(screenshot, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
            }

            viewModel.CloseComparisonDetailsCommand.Execute(null);
            window.FindControl<CheckBox>("MismatchFilter")!.IsChecked = true;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(12, viewModel.ComparisonRows.Count);
            Assert.All(viewModel.ComparisonRows, row => Assert.True(row.IsMismatch));
            Assert.Null(viewModel.SelectedComparisonRow);
            await window.ExportAsync(ExportFormat.Json, payload =>
            {
                using var export = JsonDocument.Parse(payload.Content);
                Assert.Equal(365, export.RootElement.GetProperty("days").GetArrayLength());
                Assert.Equal(24, export.RootElement.GetProperty("comparisons").GetArrayLength());
                return Task.FromResult(true);
            });
            Assert.Contains("gespeichert", viewModel.StatusMessage);
            await window.ExportAsync(ExportFormat.Csv, _ => Task.FromResult(false));
            Assert.Contains("abgebrochen", viewModel.StatusMessage);
            Assert.True(viewModel.CanExport);
            await window.ExportAsync(ExportFormat.TestVectors, _ => throw new IOException("Test write failure"));
            Assert.Contains("fehlgeschlagen", viewModel.StatusMessage);
            Assert.True(viewModel.CanExport);
            window.FindControl<NumericUpDown>("YearInput")!.Value = 2027;
            Dispatcher.UIThread.RunJobs();
            Assert.Empty(viewModel.ComparisonRows);
            Assert.False(viewModel.CanExport);
            Assert.False(window.FindControl<Button>("ExportJsonButton")!.IsEnabled);
            Assert.Equal(string.Empty, window.FindControl<SelectableTextBlock>("ReferenceSourceText")!.Text);
            window.FindControl<CheckBox>("MismatchFilter")!.IsChecked = false;
            window.FindControl<Button>("CalculateButton")!.Command!.Execute(null);
            await viewModel.CalculateCommand.ExecutionTask!;
            Dispatcher.UIThread.RunJobs();
            Assert.NotEmpty(viewModel.ComparisonRows);
            Assert.All(viewModel.ComparisonRows, row => Assert.Equal("KEINE REFERENZ", row.Status));
            Assert.Contains("2027", window.FindControl<TextBlock>("ReferenceDescriptionText")!.Text);
        }
        finally { window.Close(); }
    }

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
            Assert.True(viewModel.CanExport);
            Assert.False(viewModel.CanExportCsv);
            Assert.False(viewModel.IsBusy);
            var table = window.FindControl<DataGrid>("ResultsTable")!;
            Assert.Same(viewModel.Results, table.ItemsSource);
            Assert.Contains("Berlin · 2026 · 365 Tage", window.FindControl<TextBlock>("ResultSummaryText")!.Text);

            window.FindControl<CheckBox>("CandidateFilter")!.IsChecked = true;
            Dispatcher.UIThread.RunJobs();
            Assert.NotEmpty(viewModel.Results);
            Assert.All(viewModel.Results, row => Assert.True(row.Day.IsEkadashiCandidate));
            Assert.True(viewModel.Results.Count < 365);
            await window.ExportAsync(ExportFormat.TestVectors, payload =>
            {
                using var export = JsonDocument.Parse(payload.Content);
                Assert.Equal(365, export.RootElement.GetProperty("vectors").GetArrayLength());
                Assert.Equal(JsonValueKind.Null, export.RootElement.GetProperty("vectors")[0].GetProperty("fasting").ValueKind);
                return Task.FromResult(true);
            });
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
            Assert.False(viewModel.CanExport);
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
