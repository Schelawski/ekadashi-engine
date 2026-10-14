using EkadashiEngine.Astronomy.SwissEphemeris;
using EkadashiEngine.Core.Astronomy;
using EkadashiEngine.Core.Rules;
using EkadashiEngine.Core.ReferenceData;
using EkadashiEngine.Core.Diagnostics;
using EkadashiEngine.Core.Export;
using EkadashiEngine.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EkadashiEngine.Desktop;

public static class ServiceConfiguration
{
    public static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging
            .SetMinimumLevel(LogLevel.Information)
            .AddDebug()
            .AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            }));
        services.AddSingleton<SwissEphemerisAstronomyService>();
        services.AddSingleton<IAstronomyService>(provider => provider.GetRequiredService<SwissEphemerisAstronomyService>());
        services.AddSingleton<ISunriseCalculator>(provider => provider.GetRequiredService<SwissEphemerisAstronomyService>());
        services.AddSingleton<ITithiCalculator, TithiCalculator>();
        services.AddTransient<AstronomyDiagnosticsService>();
        services.AddTransient<IYearAstronomyCalculator, YearAstronomyCalculator>();
        services.AddTransient<ITithiIntervalCalculator, TithiIntervalCalculator>();
        services.AddTransient<TraditionDataCalculator>();
        services.AddTransient<ITraditionRule, AnandaMargaRule>();
        services.AddTransient<FastingCalendarCalculator>();
        services.AddTransient<IFastingCalendarCalculator>(provider => provider.GetRequiredService<FastingCalendarCalculator>());
        services.AddSingleton<ICalendarReferenceProvider, EmbeddedCalendarReferenceProvider>();
        services.AddTransient<CalendarComparisonService>();
        services.AddTransient<IDayAstronomyCalculator, DayAstronomyCalculator>();
        services.AddSingleton(SwissEphemerisAstronomyService.Provenance);
        services.AddTransient<CalculationSnapshotFactory>();
        services.AddTransient<CalculationExporter>();
        services.AddTransient<MainWindowViewModel>();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }
}
