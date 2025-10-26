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
            // Check for help flags first
            if (args.Contains("--help") || args.Contains("-h"))
            {
                ShowHelp();
                return 0;
            }

            var host = CreateHostBuilder(args).Build();
            
            using var scope = host.Services.CreateScope();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("8Bitten.Headless");
            
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

    /// <summary>
    /// Display help information for the 8Bitten NES Emulator Headless mode
    /// </summary>
    #pragma warning disable CA1303 // Do not pass literals as localized parameters - Help text is intentionally in English
    private static void ShowHelp()
    {
        System.Console.WriteLine("8Bitten NES Emulator - Headless Mode");
        System.Console.WriteLine("=====================================");
        System.Console.WriteLine();

        System.Console.WriteLine("OVERVIEW:");
        System.Console.WriteLine("  8Bitten is a cycle-accurate Nintendo Entertainment System (NES) emulator");
        System.Console.WriteLine("  designed for research-grade accuracy and professional game development.");
        System.Console.WriteLine("  Headless mode runs without UI for automated testing and background processing.");
        System.Console.WriteLine();

        System.Console.WriteLine("USAGE:");
        System.Console.WriteLine("  8Bitten.Console.Headless.exe <rom-file> [options]");
        System.Console.WriteLine("  dotnet run --project src/Emulator.Console/Headless <rom-file> [options]");
        System.Console.WriteLine();

        System.Console.WriteLine("PARAMETERS:");
        System.Console.WriteLine("  <rom-file>        Path to NES ROM file (.nes format) [REQUIRED]");
        System.Console.WriteLine("  --help, -h        Display this help information");
        System.Console.WriteLine("  --config <file>   Path to configuration file (JSON format)");
        System.Console.WriteLine("  --log-level <lvl> Logging level (Debug, Info, Warning, Error)");
        System.Console.WriteLine("  --save-state <f>  Path to save state file");
        System.Console.WriteLine("  --frames <count>  Number of frames to emulate (for testing)");
        System.Console.WriteLine("  --output <dir>    Output directory for test results");
        System.Console.WriteLine();

        System.Console.WriteLine("EXAMPLES:");
        System.Console.WriteLine("  # Basic ROM testing");
        System.Console.WriteLine("  8Bitten.Console.Headless.exe \"test-rom.nes\"");
        System.Console.WriteLine();
        System.Console.WriteLine("  # Automated testing with frame limit");
        System.Console.WriteLine("  8Bitten.Console.Headless.exe game.nes --frames 1000 --log-level Debug");
        System.Console.WriteLine();
        System.Console.WriteLine("  # CI/CD integration");
        System.Console.WriteLine("  8Bitten.Console.Headless.exe rom.nes --output ./test-results");
        System.Console.WriteLine();

        System.Console.WriteLine("HEADLESS MODE FEATURES:");
        System.Console.WriteLine("  ✓ No UI dependencies - perfect for servers");
        System.Console.WriteLine("  ✓ Minimal resource usage");
        System.Console.WriteLine("  ✓ Automated testing capabilities");
        System.Console.WriteLine("  ✓ CI/CD pipeline integration");
        System.Console.WriteLine("  ✓ Background service operation");
        System.Console.WriteLine("  ✓ Comprehensive logging output");
        System.Console.WriteLine();

        System.Console.WriteLine("REQUIREMENTS:");
        System.Console.WriteLine("  • .NET 9.0 Runtime or later");
        System.Console.WriteLine("  • Windows 10/11, Linux, or macOS");
        System.Console.WriteLine("  • 50MB available RAM");
        System.Console.WriteLine("  • Legal NES ROM file (.nes format, NROM mapper)");
        System.Console.WriteLine();

        System.Console.WriteLine("QUICK START:");
        System.Console.WriteLine("  1. Obtain a legal NES ROM file");
        System.Console.WriteLine("  2. Run: 8Bitten.Console.Headless.exe your-game.nes");
        System.Console.WriteLine("  3. Monitor output via logging");
        System.Console.WriteLine("  4. Use Ctrl+C to stop emulation");
        System.Console.WriteLine();

        System.Console.WriteLine("Note: ROM loading and emulation logic will be implemented in the next phase.");
        System.Console.WriteLine();
        System.Console.WriteLine("8Bitten NES Emulator v1.0.0 - Research-grade cycle-accurate emulation");
        System.Console.WriteLine("For more information, visit: https://github.com/ggcoleman/8bitten");
    }
    #pragma warning restore CA1303
}
