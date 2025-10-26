using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace EightBitten.Infrastructure.Logging;

/// <summary>
/// Configuration for logging infrastructure in 8Bitten emulator
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Configure logging services for dependency injection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection AddLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            
            // Console logging for development
            builder.AddConsole();

            // TODO: Add file logging provider when needed
            // builder.AddFile(configuration.GetSection("Logging:File"));

            // Configure log levels from configuration
            builder.AddConfiguration(configuration.GetSection("Logging"));
        });

        return services;
    }

    /// <summary>
    /// Create a logger factory with default configuration
    /// </summary>
    /// <returns>Configured logger factory</returns>
    public static ILoggerFactory CreateLoggerFactory()
    {
        return LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
    }
}

/// <summary>
/// Log event IDs for 8Bitten emulator components
/// </summary>
public static class LogEvents
{
    // Core emulation events (1000-1999)
    public static readonly EventId CpuInstruction = new(1001, "CPU_INSTRUCTION");
    public static readonly EventId PpuRender = new(1002, "PPU_RENDER");
    public static readonly EventId ApuAudio = new(1003, "APU_AUDIO");
    public static readonly EventId MemoryAccess = new(1004, "MEMORY_ACCESS");
    public static readonly EventId CartridgeLoad = new(1005, "CARTRIDGE_LOAD");
    public static readonly EventId TimingSync = new(1006, "TIMING_SYNC");

    // Infrastructure events (2000-2999)
    public static readonly EventId Configuration = new(2001, "CONFIGURATION");
    public static readonly EventId Performance = new(2002, "PERFORMANCE");
    public static readonly EventId Recording = new(2003, "RECORDING");
    public static readonly EventId Analysis = new(2004, "ANALYSIS");

    // Interface events (3000-3999)
    public static readonly EventId McpConnection = new(3001, "MCP_CONNECTION");
    public static readonly EventId CliCommand = new(3002, "CLI_COMMAND");
    public static readonly EventId GuiInteraction = new(3003, "GUI_INTERACTION");
    public static readonly EventId ResearchExport = new(3004, "RESEARCH_EXPORT");

    // Error events (9000-9999)
    public static readonly EventId EmulationError = new(9001, "EMULATION_ERROR");
    public static readonly EventId ConfigurationError = new(9002, "CONFIGURATION_ERROR");
    public static readonly EventId PerformanceError = new(9003, "PERFORMANCE_ERROR");
    public static readonly EventId InterfaceError = new(9004, "INTERFACE_ERROR");
}

/// <summary>
/// Extension methods for structured logging
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates for high performance logging scenarios", Justification = "These are convenience methods for non-performance-critical logging")]
public static class LoggerExtensions
{
    /// <summary>
    /// Log CPU instruction execution
    /// </summary>
    public static void LogCpuInstruction(this ILogger logger, string instruction, ushort address, byte opcode)
    {
        logger.LogDebug(LogEvents.CpuInstruction, 
            "CPU executing {Instruction} at ${Address:X4} (opcode: ${Opcode:X2})", 
            instruction, address, opcode);
    }

    /// <summary>
    /// Log PPU rendering operation
    /// </summary>
    public static void LogPpuRender(this ILogger logger, int scanline, int cycle)
    {
        logger.LogTrace(LogEvents.PpuRender, 
            "PPU rendering scanline {Scanline}, cycle {Cycle}", 
            scanline, cycle);
    }

    /// <summary>
    /// Log memory access operation
    /// </summary>
    public static void LogMemoryAccess(this ILogger logger, ushort address, byte value, bool isWrite)
    {
        logger.LogTrace(LogEvents.MemoryAccess, 
            "Memory {Operation} at ${Address:X4}: ${Value:X2}", 
            isWrite ? "WRITE" : "READ", address, value);
    }

    /// <summary>
    /// Log performance metrics
    /// </summary>
    public static void LogPerformance(this ILogger logger, string metric, double value, string unit)
    {
        logger.LogInformation(LogEvents.Performance, 
            "Performance metric {Metric}: {Value} {Unit}", 
            metric, value, unit);
    }

    /// <summary>
    /// Log emulation error with context
    /// </summary>
    public static void LogEmulationError(this ILogger logger, Exception exception, string component, string context)
    {
        logger.LogError(LogEvents.EmulationError, exception, 
            "Emulation error in {Component}: {Context}", 
            component, context);
    }
}
