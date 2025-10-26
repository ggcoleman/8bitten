using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using EightBitten.Infrastructure.Logging;
using EightBitten.Infrastructure.Configuration;
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

            AnsiConsole.WriteLine("Cycle-Accurate NES Emulator - CLI Mode");
            AnsiConsole.WriteLine();

            var host = CreateHostBuilder(args).Build();
            
            using var scope = host.Services.CreateScope();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("8Bitten.CLI");
            
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            logger.LogInformation("8Bitten CLI Console starting...");
            #pragma warning restore CA1848

            // TODO: Implement CLI emulation logic
            AnsiConsole.MarkupLine("[yellow]CLI mode not yet implemented[/]");

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
                
                // TODO: Add emulator services
                // TODO: Add MonoGame services
            });

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
