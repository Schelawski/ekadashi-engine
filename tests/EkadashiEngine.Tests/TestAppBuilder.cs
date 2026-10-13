using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using EkadashiEngine.Desktop;

[assembly: AvaloniaTestApplication(typeof(EkadashiEngine.Tests.TestAppBuilder))]

namespace EkadashiEngine.Tests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .WithInterFont()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .UseSkia();
}
