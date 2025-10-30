using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using EightBitten.Infrastructure.Logging;
using EightBitten.Infrastructure.Configuration;
using EightBitten.Core.Cartridge;
using EightBitten.Core.Emulator;
using EightBitten.Core.CPU;
using EightBitten.Core.PPU;
using EightBitten.Core.APU;
using EightBitten.Core.Memory;
using EightBitten.Core.Timing;
using System.IO;

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

            // Parse and validate ROM file argument
            var romFilePath = ParseROMFileArgument(args);
            if (romFilePath == null)
            {
                #pragma warning disable CA1303 // Do not pass literals as localized parameters
                System.Console.WriteLine("Error: ROM file path is required");
                System.Console.WriteLine("Use --help for usage information");
                #pragma warning restore CA1303
                return 1; // General error
            }

            // Parse frames argument
            var frameCount = ParseFramesArgument(args);

            // Validate ROM file
            var validationResult = ValidateROMFile(romFilePath);
            if (!validationResult.IsValid)
            {
                System.Console.WriteLine($"Error: {validationResult.ErrorMessage}");
                return validationResult.ExitCode;
            }

            var host = CreateHostBuilder(args).Build();

            using var scope = host.Services.CreateScope();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("8Bitten.Headless");

            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            logger.LogInformation("8Bitten Headless Console starting...");
            logger.LogInformation("ROM file: {ROMFile}", romFilePath);

            // Load and validate ROM
            var loadResult = await ROMLoader.LoadROMAsync(romFilePath).ConfigureAwait(false);

            if (!loadResult.IsSuccess)
            {
                logger.LogError("Failed to load ROM: {Error}", loadResult.ErrorMessage);
                System.Console.WriteLine($"Error: {loadResult.ErrorMessage}");
                return GetExitCodeFromLoadError(loadResult.ErrorCode);
            }

            logger.LogInformation("ROM loaded successfully");
            logger.LogInformation("Mapper: {Mapper}, PRG: {PRGSize}KB, CHR: {CHRSize}KB",
                loadResult.Cartridge!.Header.MapperNumber,
                loadResult.Cartridge.Header.PRGROMSize / 1024,
                loadResult.Cartridge.Header.CHRROMSize / 1024);

            // Create a simple headless emulation session to test timing
            logger.LogInformation("Starting headless emulation session...");

            try
            {
                // Create emulator components (similar to CLI but headless)
                var services = new ServiceCollection();
                services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

                // Add core emulator services
                services.AddSingleton<TimingCoordinator>();
                services.AddSingleton<CPU6502>();
                services.AddSingleton<Core.PPU.PictureProcessingUnit>(provider =>
                {
                    var ppuLogger = provider.GetRequiredService<ILogger<Core.PPU.PictureProcessingUnit>>();
                    var memoryMap = provider.GetRequiredService<IPPUMemoryMap>();
                    return new Core.PPU.PictureProcessingUnit(ppuLogger, memoryMap, headless: true); // Headless mode!
                });
                services.AddSingleton<Core.APU.AudioProcessingUnit>();
                services.AddSingleton<ICPUMemoryMap, CPUMemoryMap>();
                services.AddSingleton<IPPUMemoryMap, PPUMemoryMap>();
                services.AddSingleton<NESEmulator>(provider =>
                {
                    var emulatorLogger = provider.GetRequiredService<ILogger<NESEmulator>>();
                    var timing = provider.GetRequiredService<TimingCoordinator>();
                    var cpu = provider.GetRequiredService<CPU6502>();
                    var ppu = provider.GetRequiredService<Core.PPU.PictureProcessingUnit>();
                    var apu = provider.GetRequiredService<Core.APU.AudioProcessingUnit>();
                    var cpuMemoryMap = provider.GetRequiredService<ICPUMemoryMap>();
                    var ppuMemoryMap = provider.GetRequiredService<IPPUMemoryMap>();
                    return new NESEmulator(emulatorLogger, timing, cpu, ppu, apu, cpuMemoryMap, ppuMemoryMap, headless: true);
                });

                var serviceProvider = services.BuildServiceProvider();
                var emulator = serviceProvider.GetRequiredService<NESEmulator>();

                // Initialize and load ROM
                emulator.Initialize();

                // Load ROM data directly (convert cartridge back to ROM data)
                var romPath = args[0]; // First argument is the ROM file path
                emulator.LoadROM(romPath);

                emulator.Reset();

                #pragma warning disable CA1849 // Do not call synchronous methods in async context
                emulator.Start();
                #pragma warning restore CA1849

                logger.LogInformation("Emulator initialized successfully - running emulator to see PPU advancement...");

                // Run the emulator for the specified number of frames
                logger.LogInformation("Running emulator for {FrameCount} frames...", frameCount);
                for (int frame = 0; frame < frameCount; frame++)
                {
                    logger.LogInformation("Executing frame {Frame}...", frame);
                    emulator.ExecuteFrame();

                    logger.LogInformation("Frame {Frame} completed", frame);
                }

                logger.LogInformation("Multiple frame execution completed!");
                logger.LogInformation("Emulator state: Frame={Frame}, Cycles={Cycles}", emulator.FrameNumber, emulator.CycleCount);
            }
            #pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception emulatorEx)
            #pragma warning restore CA1031
            {
                logger.LogError(emulatorEx, "Error during headless emulation");
                return 97;
            }

            #pragma warning restore CA1848

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

    /// <summary>
    /// Parse ROM file path from command line arguments
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>ROM file path or null if not found</returns>
    private static string? ParseROMFileArgument(string[] args)
    {
        // Look for the first argument that doesn't start with -- and has .nes extension
        foreach (var arg in args)
        {
            if (!arg.StartsWith("--", StringComparison.Ordinal) && !arg.StartsWith('-'))
            {
                return arg;
            }
        }
        return null;
    }

    /// <summary>
    /// Parse frames argument from command line arguments
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Number of frames to execute (default: 1)</returns>
    private static int ParseFramesArgument(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--frames" && int.TryParse(args[i + 1], out int frameCount))
            {
                return Math.Max(1, frameCount); // Ensure at least 1 frame
            }
        }
        return 1; // Default to 1 frame
    }

    /// <summary>
    /// Validate ROM file exists and has correct extension
    /// </summary>
    /// <param name="romFilePath">ROM file path</param>
    /// <returns>Validation result</returns>
    private static ROMFileValidationResult ValidateROMFile(string romFilePath)
    {
        // Check if file exists
        if (!File.Exists(romFilePath))
        {
            return new ROMFileValidationResult(false, "File not found", 4); // I/O error
        }

        // Check file extension
        var extension = Path.GetExtension(romFilePath).ToUpperInvariant();
        if (extension != ".NES")
        {
            return new ROMFileValidationResult(false,
                $"Invalid file extension '{extension}' - only .nes files are supported", 2); // Invalid ROM
        }

        // Check if file is readable
        try
        {
            using var fileStream = File.OpenRead(romFilePath);
            // File is readable
        }
        catch (UnauthorizedAccessException)
        {
            return new ROMFileValidationResult(false, "Access denied - file is not readable", 4); // I/O error
        }
        catch (IOException ex)
        {
            return new ROMFileValidationResult(false, $"I/O error: {ex.Message}", 4); // I/O error
        }

        return new ROMFileValidationResult(true, null, 0);
    }

    /// <summary>
    /// Map ROM load error code to exit code
    /// </summary>
    /// <param name="loadErrorCode">ROM load error code</param>
    /// <returns>Exit code</returns>
    private static int GetExitCodeFromLoadError(ROMLoadErrorCode loadErrorCode)
    {
        return loadErrorCode switch
        {
            ROMLoadErrorCode.InvalidPath => 2,
            ROMLoadErrorCode.InvalidExtension => 2,
            ROMLoadErrorCode.InvalidHeader => 2,
            ROMLoadErrorCode.UnsupportedMapper => 3,
            ROMLoadErrorCode.FileNotFound => 4,
            ROMLoadErrorCode.IOError => 4,
            _ => 1 // General error
        };
    }

    /// <summary>
    /// ROM file validation result
    /// </summary>
    private sealed record ROMFileValidationResult(bool IsValid, string? ErrorMessage, int ExitCode);
}
