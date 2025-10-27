using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace EightBitten.Infrastructure.Logging;

/// <summary>
/// Diagnostic output system with structured logging (JSON format, configurable verbosity)
/// Provides comprehensive diagnostic information for debugging and validation
/// </summary>
public class DiagnosticLogger
{
    private readonly ILogger _logger;
    private readonly DiagnosticLoggerOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public DiagnosticLogger(ILogger logger, DiagnosticLoggerOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new DiagnosticLoggerOptions();

        // Cache JsonSerializerOptions to avoid creating new instances
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = _options.PrettyPrintJSON,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Log CPU state information
    /// </summary>
    /// <param name="cpuState">CPU state data</param>
    public void LogCPUState(CPUDiagnosticData cpuState)
    {
        ArgumentNullException.ThrowIfNull(cpuState);

        if (_options.IncludeCPUState)
        {
            var logData = new
            {
                Type = "CPU_STATE",
                Timestamp = DateTime.UtcNow,
                Data = cpuState
            };

            LogDiagnosticData(logData, "CPU State: A={A:X2} X={X:X2} Y={Y:X2} PC={PC:X4} SP={SP:X2} P={P:X2} Cycles={Cycles}",
                cpuState.A, cpuState.X, cpuState.Y, cpuState.PC, cpuState.SP, cpuState.P, cpuState.CycleCount);
        }
    }

    /// <summary>
    /// Log PPU state information
    /// </summary>
    /// <param name="ppuState">PPU state data</param>
    public void LogPPUState(PPUDiagnosticData ppuState)
    {
        ArgumentNullException.ThrowIfNull(ppuState);

        if (_options.IncludePPUState)
        {
            var logData = new
            {
                Type = "PPU_STATE",
                Timestamp = DateTime.UtcNow,
                Data = ppuState
            };

            LogDiagnosticData(logData, "PPU State: Scanline={Scanline} Cycle={Cycle} Frame={Frame} CTRL={CTRL:X2} MASK={MASK:X2} STATUS={STATUS:X2}",
                ppuState.Scanline, ppuState.Cycle, ppuState.FrameCount, ppuState.PPUCTRL, ppuState.PPUMASK, ppuState.PPUSTATUS);
        }
    }

    /// <summary>
    /// Log APU state information
    /// </summary>
    /// <param name="apuState">APU state data</param>
    public void LogAPUState(APUDiagnosticData apuState)
    {
        ArgumentNullException.ThrowIfNull(apuState);

        if (_options.IncludeAPUState)
        {
            var logData = new
            {
                Type = "APU_STATE",
                Timestamp = DateTime.UtcNow,
                Data = apuState
            };

            LogDiagnosticData(logData, "APU State: Pulse1={Pulse1} Pulse2={Pulse2} Triangle={Triangle} Noise={Noise} DMC={DMC}",
                apuState.Pulse1Enabled, apuState.Pulse2Enabled, apuState.TriangleEnabled, apuState.NoiseEnabled, apuState.DMCEnabled);
        }
    }

    /// <summary>
    /// Log memory access information
    /// </summary>
    /// <param name="memoryAccess">Memory access data</param>
    public void LogMemoryAccess(MemoryAccessDiagnosticData memoryAccess)
    {
        ArgumentNullException.ThrowIfNull(memoryAccess);

        if (_options.IncludeMemoryAccess)
        {
            var logData = new
            {
                Type = "MEMORY_ACCESS",
                Timestamp = DateTime.UtcNow,
                Data = memoryAccess
            };

            LogDiagnosticData(logData, "Memory {Type}: Address={Address:X4} Value={Value:X2} Cycle={Cycle}",
                memoryAccess.IsWrite ? "Write" : "Read", memoryAccess.Address, memoryAccess.Value, memoryAccess.CycleCount);
        }
    }

    /// <summary>
    /// Log timing analysis data
    /// </summary>
    /// <param name="timingData">Timing analysis data</param>
    public void LogTimingAnalysis(TimingDiagnosticData timingData)
    {
        ArgumentNullException.ThrowIfNull(timingData);

        if (_options.IncludeTimingAnalysis)
        {
            var logData = new
            {
                Type = "TIMING_ANALYSIS",
                Timestamp = DateTime.UtcNow,
                Data = timingData
            };

            LogDiagnosticData(logData, "Timing: Frame={Frame} CPU_Cycles={CPUCycles} PPU_Cycles={PPUCycles} Frame_Time={FrameTime}ms",
                timingData.FrameNumber, timingData.CPUCycles, timingData.PPUCycles, timingData.FrameTimeMs);
        }
    }

    /// <summary>
    /// Log emulation session summary
    /// </summary>
    /// <param name="sessionData">Session summary data</param>
    public void LogSessionSummary(SessionDiagnosticData sessionData)
    {
        ArgumentNullException.ThrowIfNull(sessionData);

        var logData = new
        {
            Type = "SESSION_SUMMARY",
            Timestamp = DateTime.UtcNow,
            Data = sessionData
        };

        LogDiagnosticData(logData, "Session: Duration={Duration}s Frames={Frames} Total_Cycles={Cycles} Avg_FPS={FPS:F2}",
            sessionData.DurationSeconds, sessionData.TotalFrames, sessionData.TotalCycles, sessionData.AverageFPS);
    }

    private void LogDiagnosticData(object data, string messageTemplate, params object[] args)
    {
        if (_options.OutputFormat == DiagnosticOutputFormat.JSON)
        {
            var jsonString = JsonSerializer.Serialize(data, _jsonOptions);

            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogInformation("{DiagnosticJSON}", jsonString);
            #pragma warning restore CA1848
        }
        else
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            #pragma warning disable CA2254 // Template should not vary between calls
            _logger.LogInformation(messageTemplate, args);
            #pragma warning restore CA2254
            #pragma warning restore CA1848
        }
    }
}

/// <summary>
/// Configuration options for diagnostic logger
/// </summary>
public class DiagnosticLoggerOptions
{
    public bool IncludeCPUState { get; set; } = true;
    public bool IncludePPUState { get; set; } = true;
    public bool IncludeAPUState { get; set; } = true;
    public bool IncludeMemoryAccess { get; set; } // Can be very verbose
    public bool IncludeTimingAnalysis { get; set; } = true;
    public DiagnosticOutputFormat OutputFormat { get; set; } = DiagnosticOutputFormat.Text;
    public bool PrettyPrintJSON { get; set; } = true;
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;
}

/// <summary>
/// Diagnostic output format options
/// </summary>
public enum DiagnosticOutputFormat
{
    Text,
    JSON
}

/// <summary>
/// CPU diagnostic data structure
/// </summary>
public record CPUDiagnosticData(
    byte A, byte X, byte Y, ushort PC, byte SP, byte P,
    ulong CycleCount, bool IRQPending, bool NMIPending);

/// <summary>
/// PPU diagnostic data structure
/// </summary>
public record PPUDiagnosticData(
    ushort Scanline, ushort Cycle, ulong FrameCount,
    byte PPUCTRL, byte PPUMASK, byte PPUSTATUS,
    ushort VRAMAddress, byte VRAMBuffer);

/// <summary>
/// APU diagnostic data structure
/// </summary>
public record APUDiagnosticData(
    bool Pulse1Enabled, bool Pulse2Enabled, bool TriangleEnabled,
    bool NoiseEnabled, bool DMCEnabled, byte StatusRegister);

/// <summary>
/// Memory access diagnostic data structure
/// </summary>
public record MemoryAccessDiagnosticData(
    ushort Address, byte Value, bool IsWrite, ulong CycleCount, string Component);

/// <summary>
/// Timing analysis diagnostic data structure
/// </summary>
public record TimingDiagnosticData(
    ulong FrameNumber, ulong CPUCycles, ulong PPUCycles,
    double FrameTimeMs, double ActualFPS, double TargetFPS);

/// <summary>
/// Session summary diagnostic data structure
/// </summary>
public record SessionDiagnosticData(
    double DurationSeconds, ulong TotalFrames, ulong TotalCycles,
    double AverageFPS, string ROMName, byte MapperNumber);
