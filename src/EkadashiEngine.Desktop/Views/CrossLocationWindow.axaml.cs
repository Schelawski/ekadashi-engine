using Avalonia.Controls;
using EkadashiEngine.Desktop.ViewModels;

namespace EkadashiEngine.Desktop.Views;

public partial class CrossLocationWindow : Window
{
    public CrossLocationWindow() => InitializeComponent();
    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is CrossLocationViewModel viewModel) viewModel.CalculateCommand.Cancel();
        base.OnClosed(e);
    }
}
