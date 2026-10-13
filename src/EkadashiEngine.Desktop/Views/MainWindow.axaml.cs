using Avalonia.Controls;
using EkadashiEngine.Desktop.ViewModels;

namespace EkadashiEngine.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
            viewModel.CalculateCommand.Cancel();
        base.OnClosed(e);
    }
}
