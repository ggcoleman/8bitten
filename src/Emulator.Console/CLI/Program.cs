using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using EightBitten.Infrastructure.Logging;
using EightBitten.Infrastructure.Configuration;
using EightBitten.Core.Emulator;
using EightBitten.Core.PPU;
using EightBitten.Core.APU;
using EightBitten.Infrastructure.Platform.Graphics;
using EightBitten.Infrastructure.Platform.Audio;
using EightBitten.Infrastructure.Platform.Input;
using EightBitten.Infrastructure.Metrics;
using EightBitten.Emulator.Console.CLI;
using Spectre.Console;

namespace EightBitten.Console.CLI;

/// <summary>
/// Entry point for 8Bitten CLI console application
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
            if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
            {
                ShowHelp();
                return 0;
            }

            AnsiConsole.Write(
                new FigletText("8Bitten")
                    .LeftJustified()
                    .Color(Color.Green));

            AnsiConsole.WriteLine("Cycle-Accurate NES Emulator - CLI Gaming Mode");
            AnsiConsole.WriteLine();

            // Parse ROM file argument
            if (args.Length == 0)
            {
                AnsiConsole.MarkupLine("[red]Error: ROM file required[/]");
                AnsiConsole.MarkupLine("Usage: 8Bitten.Console.CLI.exe <rom-file> [options]");
                return 1;
            }

            var romPath = args[0];
            if (!File.Exists(romPath))
            {
                AnsiConsole.MarkupLine($"[red]Error: ROM file not found: {romPath}[/]");
                return 4;
            }

            var host = CreateHostBuilder(args).Build();

            using var scope = host.Services.CreateScope();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("8Bitten.CLI");

            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            logger.LogInformation("8Bitten CLI Gaming starting with ROM: {RomPath}", romPath);
            #pragma warning restore CA1848

            // Run CLI gaming mode
            var exitCode = await RunCLIGamingAsync(scope.ServiceProvider, romPath, args, logger);
            return exitCode;
        }
        catch (OperationCanceledException)
        {
            // Expected when application is cancelled
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Configuration error: {ex.Message}[/]");
            return 1;
        }
        catch (ArgumentException ex)
        {
            AnsiConsole.MarkupLine($"[red]Invalid argument: {ex.Message}[/]");
            return 1;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AnsiConsole.WriteException(ex);
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

                // Add emulator services
                services.AddSingleton<NESEmulator>();
                services.AddSingleton<Renderer>();
                services.AddSingleton<AudioGenerator>();

                // Add platform services
                services.AddSingleton<MonoGameRenderer>();
                services.AddSingleton<NAudioRenderer>();
                services.AddSingleton<InputManager>();
                services.AddSingleton<WindowManager>();
                services.AddSingleton<PerformanceMonitor>();

                // Add CLI gaming services
                services.AddSingleton<GameWindow>();
                services.AddSingleton<GameLoop>();
            });

    /// <summary>
    /// Runs the CLI gaming mode with graphics and audio
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency injection</param>
    /// <param name="romPath">Path to the ROM file</param>
    /// <param name="args">Command line arguments</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>Exit code</returns>
    private static async Task<int> RunCLIGamingAsync(IServiceProvider serviceProvider, string romPath, string[] args, ILogger logger)
    {
        try
        {
            AnsiConsole.MarkupLine($"[green]Loading ROM:[/] {Path.GetFileName(romPath)}");

            // Get services
            var emulator = serviceProvider.GetRequiredService<NESEmulator>();
            var renderer = serviceProvider.GetRequiredService<Renderer>();
            var audioGenerator = serviceProvider.GetRequiredService<AudioGenerator>();
            var windowManager = serviceProvider.GetRequiredService<WindowManager>();
            var performanceMonitor = serviceProvider.GetRequiredService<PerformanceMonitor>();

            // Parse CLI options
            var settings = ParseCLIOptions(args);

            // Load ROM
            try
            {
                emulator.LoadROM(romPath);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: Failed to load ROM file: {ex.Message}[/]");
                return 2;
            }

            AnsiConsole.MarkupLine("[green]ROM loaded successfully![/]");

            // Create game window
            var gameWindow = new GameWindow(
                serviceProvider.GetRequiredService<MonoGameRenderer>(),
                serviceProvider.GetRequiredService<NAudioRenderer>(),
                serviceProvider.GetRequiredService<InputManager>(),
                settings,
                serviceProvider.GetRequiredService<ILogger<GameWindow>>());

            // Register window
            if (!windowManager.CreateWindow("main", gameWindow))
            {
                AnsiConsole.MarkupLine("[red]Error: Failed to create game window[/]");
                return 3;
            }

            // Create game loop
            var gameLoop = new GameLoop(
                emulator,
                gameWindow,
                renderer,
                audioGenerator,
                new GameLoopSettings { AudioEnabled = settings.AudioEnabled },
                serviceProvider.GetRequiredService<ILogger<GameLoop>>());

            AnsiConsole.MarkupLine("[green]Starting emulation...[/]");
            AnsiConsole.MarkupLine("[dim]Press ESC to exit[/]");

            // Start performance monitoring
            if (settings.PerformanceMonitoring)
            {
                performanceMonitor.Start();
            }

            // Start game loop
            if (!gameLoop.Start())
            {
                AnsiConsole.MarkupLine("[red]Error: Failed to start game loop[/]");
                return 5;
            }

            // Run the game window
            gameWindow.Run();

            // Stop game loop
            await gameLoop.StopAsync();

            // Stop performance monitoring and show stats
            if (settings.PerformanceMonitoring)
            {
                performanceMonitor.Stop();
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]Performance Statistics:[/]");
                AnsiConsole.WriteLine(performanceMonitor.GenerateReport());
            }

            AnsiConsole.MarkupLine("[green]Emulation completed successfully![/]");
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in CLI gaming mode");
            AnsiConsole.WriteException(ex);
            return 99;
        }
    }

    /// <summary>
    /// Parses CLI-specific options from command line arguments
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Game window settings</returns>
    private static GameWindowSettings ParseCLIOptions(string[] args)
    {
        var settings = new GameWindowSettings();

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--fullscreen":
                    settings.IsFullScreen = true;
                    break;
                case "--windowed":
                    settings.IsFullScreen = false;
                    break;
                case "--scale":
                    if (i + 1 < args.Length && float.TryParse(args[i + 1], out var scale))
                    {
                        settings.RenderScale = scale;
                        i++; // Skip next argument
                    }
                    break;
                case "--no-audio":
                    settings.AudioEnabled = false;
                    break;
                case "--vsync":
                    settings.VSync = true;
                    break;
                case "--no-vsync":
                    settings.VSync = false;
                    break;
                case "--performance":
                    settings.PerformanceMonitoring = true;
                    break;
                case "--test-mode":
                    // Special mode for integration tests
                    settings.WindowWidth = 512;
                    settings.WindowHeight = 480;
                    settings.RenderScale = 2.0f;
                    break;
            }
        }

        return settings;
    }

    /// <summary>
    /// Display comprehensive help information for the 8Bitten NES Emulator CLI
    /// </summary>
    private static void ShowHelp()
    {
        // Display the banner
        AnsiConsole.Write(
            new FigletText("8Bitten")
                .LeftJustified()
                .Color(Color.Green));

        AnsiConsole.WriteLine("Cycle-Accurate NES Emulator - Command Line Interface");
        AnsiConsole.WriteLine();

        // Overview
        var overviewPanel = new Panel(
            "[bold]8Bitten[/] is a cycle-accurate Nintendo Entertainment System (NES) emulator\n" +
            "designed for research-grade accuracy and professional game development.\n\n" +
            "[yellow]Current Status:[/] Framework ready, ROM loading to be implemented")
            .Header("[bold blue]Overview[/]")
            .BorderColor(Color.Blue);
        AnsiConsole.Write(overviewPanel);
        AnsiConsole.WriteLine();

        // Usage section
        var usagePanel = new Panel(
            "[bold]CLI Mode (Interactive):[/]\n" +
            "  [green]8Bitten.Console.CLI.exe[/] [cyan]<rom-file>[/] [dim][options][/]\n" +
            "  [green]dotnet run --project src/Emulator.Console/CLI[/] [cyan]<rom-file>[/] [dim][options][/]\n\n" +
            "[bold]Headless Mode (Background Service):[/]\n" +
            "  [green]8Bitten.Console.Headless.exe[/] [cyan]<rom-file>[/] [dim][options][/]\n" +
            "  [green]dotnet run --project src/Emulator.Console/Headless[/] [cyan]<rom-file>[/] [dim][options][/]")
            .Header("[bold blue]Usage[/]")
            .BorderColor(Color.Blue);
        AnsiConsole.Write(usagePanel);
        AnsiConsole.WriteLine();

        // Parameters section
        var parametersTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Yellow)
            .AddColumn(new TableColumn("[bold]Parameter[/]").Centered())
            .AddColumn(new TableColumn("[bold]Description[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Required[/]").Centered())
            .AddColumn(new TableColumn("[bold]Example[/]").LeftAligned());

        parametersTable.AddRow(
            "[cyan]<rom-file>[/]",
            "Path to NES ROM file (.nes format)\nSupports NROM mapper (Mapper 0)",
            "[red]Yes[/]",
            "[dim]game.nes[/]");

        parametersTable.AddRow(
            "[cyan]--help, -h[/]",
            "Display this help information",
            "[green]No[/]",
            "[dim]--help[/]");

        parametersTable.AddRow(
            "[cyan]--headless[/]",
            "Run in headless mode (no UI output)\nPerfect for automated testing",
            "[green]No[/]",
            "[dim]--headless[/]");

        parametersTable.AddRow(
            "[cyan]--config[/]",
            "Path to configuration file\nJSON format for emulator settings",
            "[green]No[/]",
            "[dim]--config settings.json[/]");

        parametersTable.AddRow(
            "[cyan]--log-level[/]",
            "Logging verbosity level\n(Debug, Info, Warning, Error)",
            "[green]No[/]",
            "[dim]--log-level Debug[/]");

        parametersTable.AddRow(
            "[cyan]--save-state[/]",
            "Path to save state file\nFor loading/saving game progress",
            "[green]No[/]",
            "[dim]--save-state game.sav[/]");

        AnsiConsole.Write(parametersTable);
        AnsiConsole.WriteLine();

        // Examples section
        var examplesPanel = new Panel(
            "[bold]Basic ROM Loading:[/]\n" +
            "  [green]8Bitten.Console.CLI.exe[/] [cyan]\"Super Mario Bros.nes\"[/]\n\n" +
            "[bold]Headless Testing:[/]\n" +
            "  [green]8Bitten.Console.Headless.exe[/] [cyan]test-rom.nes[/] [yellow]--log-level Debug[/]\n\n" +
            "[bold]With Configuration:[/]\n" +
            "  [green]8Bitten.Console.CLI.exe[/] [cyan]game.nes[/] [yellow]--config emulator.json[/]\n\n" +
            "[bold]Load with Save State:[/]\n" +
            "  [green]8Bitten.Console.CLI.exe[/] [cyan]game.nes[/] [yellow]--save-state progress.sav[/]")
            .Header("[bold blue]Examples[/]")
            .BorderColor(Color.Blue);
        AnsiConsole.Write(examplesPanel);
        AnsiConsole.WriteLine();

        // Modes comparison
        var modesTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Purple)
            .AddColumn(new TableColumn("[bold]Feature[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]CLI Mode[/]").Centered())
            .AddColumn(new TableColumn("[bold]Headless Mode[/]").Centered());

        modesTable.AddRow("Visual Output", "[green]✓[/] Rich console display", "[red]✗[/] Logging only");
        modesTable.AddRow("Interactive Controls", "[green]✓[/] Keyboard input", "[red]✗[/] Automated only");
        modesTable.AddRow("Real-time Stats", "[green]✓[/] Live emulation data", "[yellow]~[/] Log-based stats");
        modesTable.AddRow("Background Service", "[red]✗[/] Interactive session", "[green]✓[/] Service mode");
        modesTable.AddRow("Automated Testing", "[yellow]~[/] Manual operation", "[green]✓[/] Perfect for CI/CD");
        modesTable.AddRow("Resource Usage", "[yellow]~[/] Moderate", "[green]✓[/] Minimal");

        AnsiConsole.Write(new Panel(modesTable)
            .Header("[bold purple]CLI vs Headless Comparison[/]")
            .BorderColor(Color.Purple));
        AnsiConsole.WriteLine();

        // Requirements section
        var requirementsPanel = new Panel(
            "[bold]System Requirements:[/]\n" +
            "• .NET 9.0 Runtime or later\n" +
            "• Windows 10/11, Linux, or macOS\n" +
            "• 50MB available RAM\n" +
            "• Audio device (for CLI mode)\n\n" +
            "[bold]Supported ROM Formats:[/]\n" +
            "• .nes files (iNES format)\n" +
            "• NROM mapper (Mapper 0) - Most classic games\n" +
            "• ROM size: 16KB-32KB PRG, 8KB CHR\n\n" +
            "[bold]Compatible Games:[/]\n" +
            "• Super Mario Bros.\n" +
            "• Donkey Kong\n" +
            "• Pac-Man\n" +
            "• And many other NROM-based games")
            .Header("[bold blue]Requirements & Compatibility[/]")
            .BorderColor(Color.Blue);
        AnsiConsole.Write(requirementsPanel);
        AnsiConsole.WriteLine();

        // Quick start
        var quickStartPanel = new Panel(
            "[bold yellow]Step 1:[/] Obtain a legal NES ROM file (.nes format)\n" +
            "[bold yellow]Step 2:[/] Choose your mode:\n" +
            "           • CLI for interactive emulation with visual feedback\n" +
            "           • Headless for automated testing or background processing\n" +
            "[bold yellow]Step 3:[/] Run the emulator:\n" +
            "           [green]8Bitten.Console.CLI.exe[/] [cyan]your-game.nes[/]\n" +
            "[bold yellow]Step 4:[/] Enjoy cycle-accurate NES emulation!\n\n" +
            "[dim]Note: ROM loading and emulation logic will be implemented in the next phase.[/]")
            .Header("[bold green]Quick Start Guide[/]")
            .BorderColor(Color.Green);
        AnsiConsole.Write(quickStartPanel);
        AnsiConsole.WriteLine();

        // Footer
        AnsiConsole.MarkupLine("[dim]8Bitten NES Emulator v1.0.0 - Research-grade cycle-accurate emulation[/]");
        AnsiConsole.MarkupLine("[dim]For more information, visit: https://github.com/ggcoleman/8bitten[/]");
    }
}
