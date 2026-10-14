using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using EkadashiEngine.Desktop;
using EkadashiEngine.Desktop.ViewModels;
using EkadashiEngine.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace EkadashiEngine.Tests;

public sealed class LocationWorkflowTests
{
    [AvaloniaFact]
    public async Task CustomLocationCanBeEditedCalculatedAndAddedToComparison()
    {
        using var services = ServiceConfiguration.CreateServiceProvider();
        var viewModel = services.GetRequiredService<MainWindowViewModel>();
        var window = new MainWindow { DataContext = viewModel };
        try
        {
            window.Show();
            window.FindControl<ComboBox>("LocationSelector")!.SelectedIndex = 2;
            window.FindControl<Expander>("CustomLocationExpander")!.IsExpanded = true;
            Dispatcher.UIThread.RunJobs();
            var editor = window.FindControl<LocationEditorView>("CustomLocationEditor")!;
            editor.FindControl<TextBox>("CustomLocationName")!.Text = "Mumbai";
            editor.FindControl<NumericUpDown>("CustomLatitude")!.Value = 19.076m;
            editor.FindControl<NumericUpDown>("CustomLongitude")!.Value = 72.8777m;
            Dispatcher.UIThread.RunJobs();
            Assert.True(viewModel.UseCustomLocation);
            Assert.Equal(19.076, viewModel.EffectiveLocation!.Latitude);
            Assert.Equal("Asia/Kolkata", window.FindControl<TextBlock>("TimeZoneText")!.Text);
            await viewModel.CalculateCommand.ExecuteAsync(null);
            Assert.Equal("Mumbai", viewModel.Snapshot!.Astronomy.Location.DisplayName);
            Assert.Equal(365, viewModel.Results.Count);
            if (Environment.GetEnvironmentVariable("EKADASHI_CUSTOM_LOCATION_SCREENSHOT") is { Length: > 0 } screenshot)
            {
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                using var frame = window.CaptureRenderedFrame();
                Assert.NotNull(frame);
                frame.Save(screenshot, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
            }
            window.FindControl<Button>("CompareLocationsButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            var comparison = Assert.Single(window.OwnedWindows.OfType<CrossLocationWindow>());
            Assert.True(comparison.IsVisible);
            Assert.Contains(viewModel.CrossLocation.Locations, choice => choice.Name == "Mumbai" && choice.IsSelected);
            comparison.Close();
            editor.FindControl<AutoCompleteBox>("CustomTimeZone")!.Text = "Invalid/Zone";
            Dispatcher.UIThread.RunJobs();
            Assert.False(viewModel.CalculateCommand.CanExecute(null));
            Assert.False(viewModel.CanExport);
            Assert.Empty(viewModel.Results);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task NineLocationsCanBeComparedByLocalDateThroughBoundControls()
    {
        using var services = ServiceConfiguration.CreateServiceProvider();
        var viewModel = services.GetRequiredService<CrossLocationViewModel>();
        var window = new CrossLocationWindow { DataContext = viewModel };
        try
        {
            window.Show();
            viewModel.SelectAllCommand.Execute(null);
            window.FindControl<Button>("CompareButton")!.Command!.Execute(null);
            await viewModel.CalculateCommand.ExecutionTask!;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(9, viewModel.Summaries.Count);
            Assert.Equal(365, viewModel.Days.Count);
            Assert.Equal(9, viewModel.DayRows.Count);
            Assert.True(viewModel.SelectedDay!.Data.HasDifferences);
            Assert.Same(viewModel.DayRows, window.FindControl<DataGrid>("LocationDayTable")!.ItemsSource);
            Assert.Contains(viewModel.DayRows, row => row.Selection == "Ja");
            Assert.Contains(viewModel.DayRows, row => row.Selection == "Nein");
            if (Environment.GetEnvironmentVariable("EKADASHI_LOCATION_SCREENSHOT") is { Length: > 0 } screenshot)
            {
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                using var frame = window.CaptureRenderedFrame();
                Assert.NotNull(frame);
                frame.Save(screenshot, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
            }
            window.FindControl<CheckBox>("LocationDifferencesFilter")!.IsChecked = true;
            Dispatcher.UIThread.RunJobs();
            Assert.NotEmpty(viewModel.Days);
            Assert.True(viewModel.Days.Count < 365);
            Assert.All(viewModel.Days, day => Assert.True(day.Data.HasDifferences || day.Data.HasUnknown));
            viewModel.Year = 2027;
            Assert.Empty(viewModel.DayRows);
            Assert.Empty(viewModel.Summaries);
        }
        finally { window.Close(); }
    }
}
