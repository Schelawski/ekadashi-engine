using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EkadashiEngine.Desktop.ViewModels;
using EkadashiEngine.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EkadashiEngine.Desktop;

public partial class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _services = ServiceConfiguration.CreateServiceProvider();
            desktop.Exit += OnDesktopExit;
            desktop.MainWindow = new MainWindow
            {
                DataContext = _services.GetRequiredService<MainWindowViewModel>()
            };
            _services.GetRequiredService<ILogger<App>>().LogInformation("Desktop application initialized.");
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs args)
    {
        _services?.GetRequiredService<ILogger<App>>().LogInformation("Desktop application shutting down.");
        _services?.Dispose();
        _services = null;
    }
}
