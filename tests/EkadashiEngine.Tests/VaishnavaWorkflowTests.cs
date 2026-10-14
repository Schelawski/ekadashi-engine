using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using EkadashiEngine.Core;
using EkadashiEngine.Core.Export;
using EkadashiEngine.Desktop;
using EkadashiEngine.Desktop.ViewModels;
using EkadashiEngine.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace EkadashiEngine.Tests;

public sealed class VaishnavaWorkflowTests
{
    [AvaloniaFact]
    public async Task VaishnavaReferenceAndCrossTraditionDatesAreVisibleAndExportable()
    {
        using var services = ServiceConfiguration.CreateServiceProvider();
        var vm = services.GetRequiredService<MainWindowViewModel>();
        var window = new MainWindow { DataContext = vm };
        try
        {
            window.Show();
            window.FindControl<ComboBox>("TraditionSelector")!.SelectedIndex = 2;
            Dispatcher.UIThread.RunJobs();
            await vm.CalculateCommand.ExecuteAsync(null);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(24, vm.ComparisonRows.Count);
            Assert.Contains("24 Matches", vm.ComparisonSummary);
            var shifted = vm.ComparisonRows.Single(row => row.VaishnavaDate == "2026-03-29");
            Assert.Equal("2026-03-28", shifted.AnandaMargaDate);
            Assert.Equal("1 Tag(e)", shifted.TraditionDifference);
            Assert.Contains("Arunodaya", shifted.Diagnosis);
            using var json = JsonDocument.Parse(vm.CreateExport(ExportFormat.TestVectors).Content);
            Assert.Equal("gaudiyaVaishnava", json.RootElement.GetProperty("rule").GetProperty("tradition").GetString());
            Assert.NotEmpty(json.RootElement.GetProperty("ruleCases")[0].GetProperty("vaishnavaDays").EnumerateArray());
            Capture(window, "EKADASHI_VAISHNAVA_SCREENSHOT");
            window.Width = 960; window.Height = 740;
            window.UpdateLayout();
            Assert.True(window.FindControl<DataGrid>("ComparisonTable")!.Bounds.Height >= 110);
            Capture(window, "EKADASHI_VAISHNAVA_MIN_SCREENSHOT");
            window.Width = 1120; window.Height = 880;
            vm.SelectedComparisonRow = shifted;
            Dispatcher.UIThread.RunJobs();
            Assert.True(window.FindControl<Border>("ComparisonDetailsPanel")!.IsVisible);
            Capture(window, "EKADASHI_VAISHNAVA_DETAILS_SCREENSHOT");
            vm.PrepareCrossLocationComparison();
            Assert.Equal(Tradition.GaudiyaVaishnava, vm.CrossLocation.SelectedTradition!.Tradition);
            vm.Year = 2027;
            Assert.Null(vm.Snapshot);
            Assert.Empty(vm.ComparisonRows);
            vm.Year = null;
            Assert.False(vm.CanCompareLocations);
        }
        finally { window.Close(); }
    }

    private static void Capture(Window window, string variable)
    {
        if (Environment.GetEnvironmentVariable(variable) is not { Length: > 0 } path) return;
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        using var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        frame.Save(path, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }
}
