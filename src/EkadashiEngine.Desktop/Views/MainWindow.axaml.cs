using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using EkadashiEngine.Core.Export;
using EkadashiEngine.Desktop.ViewModels;

namespace EkadashiEngine.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly CancellationTokenSource _exportCancellation = new();
    private CrossLocationWindow? _crossLocationWindow;
    public MainWindow() => InitializeComponent();

    private void CompareLocationsClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || !viewModel.CanCompareLocations) return;
        viewModel.PrepareCrossLocationComparison();
        if (_crossLocationWindow is not null) { _crossLocationWindow.Activate(); return; }
        _crossLocationWindow = new CrossLocationWindow { DataContext = viewModel.CrossLocation };
        _crossLocationWindow.Closed += (_, _) => _crossLocationWindow = null;
        _crossLocationWindow.Show(this);
    }

    private async void ExportJsonClicked(object? sender, RoutedEventArgs e) => await ExportAsync(ExportFormat.Json);
    private async void ExportCsvClicked(object? sender, RoutedEventArgs e) => await ExportAsync(ExportFormat.Csv);
    private async void ExportVectorsClicked(object? sender, RoutedEventArgs e) => await ExportAsync(ExportFormat.TestVectors);

    public async Task ExportAsync(ExportFormat format, Func<ExportPayload, Task<bool>>? save = null)
    {
        if (DataContext is not MainWindowViewModel viewModel || !viewModel.CanExport) return;
        var snapshot = viewModel.Snapshot;
        try
        {
            var payload = viewModel.CreateExport(format);
            viewModel.IsExporting = true;
            var saved = await (save ?? SavePayloadAsync)(payload);
            if (ReferenceEquals(snapshot, viewModel.Snapshot))
                viewModel.StatusMessage = saved ? "Export gespeichert." : "Export abgebrochen.";
        }
        catch (OperationCanceledException) when (_exportCancellation.IsCancellationRequested) { }
        catch (Exception exception) { viewModel.ReportExportFailure(exception, ReferenceEquals(snapshot, viewModel.Snapshot)); }
        finally { viewModel.IsExporting = false; }
    }

    private async Task<bool> SavePayloadAsync(ExportPayload payload)
    {
        if (!StorageProvider.CanSave) throw new NotSupportedException("File saving is unavailable on this platform.");
        using var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Berechnung exportieren",
            SuggestedFileName = payload.SuggestedFileName,
            DefaultExtension = payload.Extension,
            FileTypeChoices = [new FilePickerFileType(payload.Extension.ToUpperInvariant()) { Patterns = [$"*.{payload.Extension}"] }],
            ShowOverwritePrompt = true
        });
        if (file is null) return false;
        _exportCancellation.Token.ThrowIfCancellationRequested();
        await using var stream = await file.OpenWriteAsync();
        if (stream.CanSeek) stream.SetLength(0);
        await stream.WriteAsync(payload.Content, _exportCancellation.Token);
        await stream.FlushAsync(_exportCancellation.Token);
        return true;
    }

    protected override void OnClosed(EventArgs e)
    {
        _exportCancellation.Cancel();
        _crossLocationWindow?.Close();
        if (DataContext is MainWindowViewModel viewModel)
            viewModel.CalculateCommand.Cancel();
        base.OnClosed(e);
    }
}
