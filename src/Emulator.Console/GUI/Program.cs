using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using EightBitten.Infrastructure.Logging;
using EightBitten.Infrastructure.Configuration;

namespace EightBitten.Console.GUI;

/// <summary>
/// Entry point for 8Bitten GUI console application
/// </summary>
internal static class Program
{
    /// <summary>
    /// Main entry point
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Exit code</returns>
    public static int Main(string[] args)
    {
        try
        {
            return BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (OperationCanceledException)
        {
            // Expected when application is cancelled
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            System.Console.WriteLine($"Configuration error: {ex.Message}");
            return 1;
        }
        catch (ArgumentException ex)
        {
            System.Console.WriteLine($"Invalid argument: {ex.Message}");
            return 1;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            System.Console.WriteLine($"Fatal error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Avalonia configuration, don't remove; also used by visual designer.
    /// </summary>
    /// <returns>Configured Avalonia app builder</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

/// <summary>
/// Avalonia application class
/// </summary>
internal sealed class App : Application
{
    private IHost? _host;

    /// <summary>
    /// Initialize the application
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Called when the application framework initialization is completed
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        // Create and start the host
        _host = CreateHostBuilder().Build();
        _host.StartAsync();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // TODO: Create main window
            // desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Create and configure the host builder
    /// </summary>
    /// <returns>Configured host builder</returns>
    private static IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(context.Configuration);
                services.AddEmulatorConfiguration(context.Configuration);
                
                // TODO: Add emulator services
                // TODO: Add Avalonia services
            });


}
