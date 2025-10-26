using System;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EightBitten.Core.Emulator;
using EightBitten.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EightBitten.Console;

/// <summary>
/// Console application for headless NES emulation
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Exit code</returns>
    public static async Task<int> Main(string[] args)
    {
        // Create root command
        var rootCommand = new RootCommand("8Bitten - Cycle-accurate NES emulator for research and validation");

        // Add ROM file argument
        var romFileArgument = new Argument<FileInfo>(
            name: "rom-file",
            description: "Path to the NES ROM file (.nes)")
        {
            Arity = ArgumentArity.ExactlyOne
        };
        rootCommand.AddArgument(romFileArgument);

        // Add options
        var framesOption = new Option<int>(
            name: "--frames",
            description: "Number of frames to execute (default: 60 for 1 second)",
            getDefaultValue: () => 60);
        rootCommand.AddOption(framesOption);

        var verboseOption = new Option<bool>(
            name: "--verbose",
            description: "Enable verbose logging",
            getDefaultValue: () => false);
        rootCommand.AddOption(verboseOption);

        var validateOption = new Option<bool>(
            name: "--validate",
            description: "Enable validation mode with detailed output",
            getDefaultValue: () => false);
        rootCommand.AddOption(validateOption);

        var outputOption = new Option<FileInfo?>(
            name: "--output",
            description: "Output file for validation results");
        rootCommand.AddOption(outputOption);

        // Set handler
        rootCommand.SetHandler(async (romFile, frames, verbose, validate, output) =>
        {
            try
            {
                await RunEmulatorAsync(romFile, frames, verbose, validate, output);
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"Error: {ex.Message}");
                if (verbose)
                {
                    System.Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                Environment.Exit(1);
            }
        }, romFileArgument, framesOption, verboseOption, validateOption, outputOption);

        // Parse and invoke
        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Run the emulator with specified parameters
    /// </summary>
    private static async Task RunEmulatorAsync(FileInfo romFile, int frames, bool verbose, bool validate, FileInfo? output)
    {
        // Validate ROM file
        if (!romFile.Exists)
        {
            throw new FileNotFoundException($"ROM file not found: {romFile.FullName}");
        }

        // Setup logging
        var logLevel = verbose ? LogLevel.Debug : LogLevel.Information;
        
        // Create host builder
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(logLevel);
            })
            .ConfigureServices(services =>
            {
                // Register emulator services
                services.AddEmulatorServices(ExecutionMode.Headless);
            });

        // Build and run
        using var host = hostBuilder.Build();
        await host.StartAsync();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var emulator = host.Services.GetRequiredService<NESEmulator>();

        try
        {
            // Initialize emulator
            logger.LogInformation("Initializing 8Bitten NES emulator...");
            emulator.Initialize();

            // Load ROM
            logger.LogInformation("Loading ROM: {RomFile}", romFile.Name);
            emulator.LoadROM(romFile.FullName);

            // Setup validation if requested
            ValidationResults? validationResults = null;
            if (validate)
            {
                validationResults = new ValidationResults();
                SetupValidation(emulator, validationResults, logger);
            }

            // Start emulation
            logger.LogInformation("Starting headless emulation for {Frames} frames...", frames);
            emulator.Start();

            // Execute frames
            var startTime = DateTime.UtcNow;
            for (int frame = 0; frame < frames; frame++)
            {
                emulator.ExecuteFrame();

                // Progress reporting
                if (frame % 60 == 0 || frame == frames - 1)
                {
                    var progress = (double)(frame + 1) / frames * 100;
                    logger.LogInformation("Progress: {Progress:F1}% (Frame {Frame}/{TotalFrames})", 
                        progress, frame + 1, frames);
                }

                // Check for errors
                if (emulator.State == Core.Contracts.EmulatorState.Error)
                {
                    throw new InvalidOperationException("Emulator encountered an error during execution");
                }
            }

            // Stop emulation
            emulator.StopEmulation();
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            // Report results
            logger.LogInformation("Emulation completed successfully!");
            logger.LogInformation("Execution time: {Duration:F2} seconds", duration.TotalSeconds);
            logger.LogInformation("Total cycles: {Cycles:N0}", emulator.CycleCount);
            logger.LogInformation("Average FPS: {FPS:F2}", frames / duration.TotalSeconds);

            // Output validation results
            if (validate && validationResults != null)
            {
                await OutputValidationResultsAsync(validationResults, output, logger);
            }
        }
        finally
        {
            emulator.Dispose();
            await host.StopAsync();
        }
    }

    /// <summary>
    /// Setup validation monitoring
    /// </summary>
    private static void SetupValidation(NESEmulator emulator, ValidationResults results, ILogger logger)
    {
        logger.LogInformation("Validation mode enabled - collecting detailed execution data");

        // Monitor frame completion
        emulator.FrameCompleted += (sender, e) =>
        {
            results.FramesCompleted++;
            results.TotalCycles = emulator.CycleCount;
        };

        // Monitor state changes
        emulator.StateChanged += (sender, e) =>
        {
            results.StateChanges.Add(new StateChange
            {
                Timestamp = DateTime.UtcNow,
                PreviousState = e.PreviousState,
                NewState = e.NewState
            });
        };

        // Monitor errors
        emulator.EmulationError += (sender, e) =>
        {
            results.Errors.Add(new EmulationError
            {
                Timestamp = DateTime.UtcNow,
                Exception = e.Exception,
                Message = e.Exception.Message
            });
        };
    }

    /// <summary>
    /// Output validation results
    /// </summary>
    private static async Task OutputValidationResultsAsync(ValidationResults results, FileInfo? outputFile, ILogger logger)
    {
        var summary = $"""
            Validation Results Summary:
            ==========================
            Frames Completed: {results.FramesCompleted:N0}
            Total Cycles: {results.TotalCycles:N0}
            State Changes: {results.StateChanges.Count}
            Errors: {results.Errors.Count}
            
            """;

        // Output to console
        logger.LogInformation("Validation completed:");
        System.Console.WriteLine(summary);

        // Output to file if specified
        if (outputFile != null)
        {
            try
            {
                await File.WriteAllTextAsync(outputFile.FullName, summary);
                logger.LogInformation("Validation results written to: {OutputFile}", outputFile.FullName);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to write validation results to file: {OutputFile}", outputFile.FullName);
            }
        }

        // Report any errors
        if (results.Errors.Count > 0)
        {
            logger.LogWarning("Emulation completed with {ErrorCount} errors:", results.Errors.Count);
            foreach (var error in results.Errors)
            {
                logger.LogWarning("  {Timestamp}: {Message}", error.Timestamp, error.Message);
            }
        }
        else
        {
            logger.LogInformation("Emulation completed without errors - validation successful!");
        }
    }
}

/// <summary>
/// Validation results collection
/// </summary>
public class ValidationResults
{
    public int FramesCompleted { get; set; }
    public long TotalCycles { get; set; }
    public List<StateChange> StateChanges { get; } = new();
    public List<EmulationError> Errors { get; } = new();
}

/// <summary>
/// State change record
/// </summary>
public class StateChange
{
    public DateTime Timestamp { get; set; }
    public Core.Contracts.EmulatorState PreviousState { get; set; }
    public Core.Contracts.EmulatorState NewState { get; set; }
}

/// <summary>
/// Emulation error record
/// </summary>
public class EmulationError
{
    public DateTime Timestamp { get; set; }
    public Exception Exception { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
}
