using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using EightBitten.Infrastructure.Logging;
using EightBitten.Infrastructure.Configuration;

namespace EightBitten.Console.Headless;

/// <summary>
/// Entry point for 8Bitten headless console application
/// </summary>
internal static class Program
{
    /// <summary>
    /// Main entry point
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Exit code</returns>
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var host = CreateHostBuilder(args).Build();
            
            using var scope = host.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger>();
            
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            logger.LogInformation("8Bitten Headless Console starting...");

            // TODO: Implement headless emulation logic
            logger.LogInformation("Headless mode not yet implemented");
            #pragma warning restore CA1848

            await host.RunAsync().ConfigureAwait(false);
            return 0;
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
    /// Create and configure the host builder
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Configured host builder</returns>
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(context.Configuration);
                services.AddEmulatorConfiguration(context.Configuration);
                
                // TODO: Add emulator services
            });
}
