using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using EightBitten.Core.Cartridge;
using EightBitten.Core.Timing;
using EightBitten.Infrastructure.Logging;

namespace EightBitten.Console.Headless;

/// <summary>
/// Manages headless emulation sessions with clean shutdown on Ctrl+C
/// Provides session lifecycle management and graceful termination
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used by dependency injection")]
internal sealed class EmulationSession : IDisposable
{
    private readonly ILogger _logger;
    private readonly DiagnosticLogger _diagnosticLogger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly CycleTimer _cycleTimer;
    private bool _disposed;

    public EmulationSession(ILogger logger, DiagnosticLoggerOptions? diagnosticOptions = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _diagnosticLogger = new DiagnosticLogger(logger, diagnosticOptions);
        _cancellationTokenSource = new CancellationTokenSource();
        _cycleTimer = new CycleTimer();

        // Set up Ctrl+C handler for graceful shutdown
        System.Console.CancelKeyPress += OnCancelKeyPress;
    }

    /// <summary>
    /// Start emulation session with the specified cartridge
    /// </summary>
    /// <param name="cartridge">Loaded cartridge to emulate</param>
    /// <param name="options">Session options</param>
    /// <returns>Session result</returns>
    public async Task<EmulationSessionResult> StartAsync(ICartridge cartridge, EmulationSessionOptions options)
    {
        ArgumentNullException.ThrowIfNull(cartridge);
        ArgumentNullException.ThrowIfNull(options);

        var startTime = DateTime.UtcNow;
        var sessionId = Guid.NewGuid();

        try
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogInformation("Starting emulation session {SessionId}", sessionId);
            _logger.LogInformation("ROM: Mapper {Mapper}, PRG: {PRGSize}KB, CHR: {CHRSize}KB",
                cartridge.Header.MapperNumber, cartridge.Header.PRGROMSize / 1024, cartridge.Header.CHRROMSize / 1024);
            #pragma warning restore CA1848

            // Initialize timing
            _cycleTimer.Start();
            var targetCycles = CalculateTargetCycles(options);

            // Main emulation loop
            var result = await RunEmulationLoop(cartridge, options, targetCycles).ConfigureAwait(false);

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            // Log session summary
            var sessionData = new SessionDiagnosticData(
                DurationSeconds: duration.TotalSeconds,
                TotalFrames: _cycleTimer.GetCurrentFrame(),
                TotalCycles: _cycleTimer.CycleCount,
                AverageFPS: _cycleTimer.GetCurrentFrame() / duration.TotalSeconds,
                ROMName: "Unknown", // TODO: Extract from cartridge
                MapperNumber: (byte)cartridge.Header.MapperNumber
            );

            _diagnosticLogger.LogSessionSummary(sessionData);

            return new EmulationSessionResult(
                Success: true,
                SessionId: sessionId,
                Duration: duration,
                TotalCycles: _cycleTimer.CycleCount,
                TotalFrames: _cycleTimer.GetCurrentFrame(),
                ErrorMessage: null
            );
        }
        catch (OperationCanceledException)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogInformation("Emulation session {SessionId} cancelled by user", sessionId);
            #pragma warning restore CA1848
            return new EmulationSessionResult(
                Success: true,
                SessionId: sessionId,
                Duration: DateTime.UtcNow - startTime,
                TotalCycles: _cycleTimer.CycleCount,
                TotalFrames: _cycleTimer.GetCurrentFrame(),
                ErrorMessage: "Cancelled by user"
            );
        }
        catch (InvalidOperationException ex)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogError(ex, "Emulation session {SessionId} failed", sessionId);
            #pragma warning restore CA1848
            return new EmulationSessionResult(
                Success: false,
                SessionId: sessionId,
                Duration: DateTime.UtcNow - startTime,
                TotalCycles: _cycleTimer.CycleCount,
                TotalFrames: _cycleTimer.GetCurrentFrame(),
                ErrorMessage: ex.Message
            );
        }
        catch (OutOfMemoryException ex)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogError(ex, "Emulation session {SessionId} failed due to memory issues", sessionId);
            #pragma warning restore CA1848
            return new EmulationSessionResult(
                Success: false,
                SessionId: sessionId,
                Duration: DateTime.UtcNow - startTime,
                TotalCycles: _cycleTimer.CycleCount,
                TotalFrames: _cycleTimer.GetCurrentFrame(),
                ErrorMessage: ex.Message
            );
        }
    }

    private async Task<bool> RunEmulationLoop(ICartridge cartridge, EmulationSessionOptions options, ulong targetCycles)
    {
        var frameCount = 0UL;
        var lastDiagnosticTime = DateTime.UtcNow;

        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            // Check termination conditions
            if (options.MaxCycles.HasValue && _cycleTimer.CycleCount >= options.MaxCycles.Value)
            {
                #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                _logger.LogInformation("Reached maximum cycle limit: {Cycles}", options.MaxCycles.Value);
                #pragma warning restore CA1848
                break;
            }

            if (options.MaxFrames.HasValue && frameCount >= options.MaxFrames.Value)
            {
                #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                _logger.LogInformation("Reached maximum frame limit: {Frames}", options.MaxFrames.Value);
                #pragma warning restore CA1848
                break;
            }

            if (options.MaxDuration.HasValue && _cycleTimer.GetElapsedTime() >= options.MaxDuration.Value)
            {
                #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                _logger.LogInformation("Reached maximum duration: {Duration}", options.MaxDuration.Value);
                #pragma warning restore CA1848
                break;
            }

            // Execute one frame worth of cycles
            var cyclesPerFrame = CycleTimer.GetCyclesPerFrame();
            _cycleTimer.ExecuteCycles((int)cyclesPerFrame);

            // TODO: Integrate with actual emulator components
            // For now, just simulate emulation
            await Task.Delay(1, _cancellationTokenSource.Token).ConfigureAwait(false); // Simulate work

            frameCount++;

            // Periodic diagnostic output
            if (options.DiagnosticInterval.HasValue)
            {
                var now = DateTime.UtcNow;
                if (now - lastDiagnosticTime >= options.DiagnosticInterval.Value)
                {
                    LogDiagnosticInfo(frameCount);
                    lastDiagnosticTime = now;
                }
            }

            // Frame rate limiting (if needed)
            if (options.EnableFrameRateLimit)
            {
                var waitTime = _cycleTimer.SynchronizeWithTarget(_cycleTimer.CycleCount);
                if (waitTime > TimeSpan.Zero)
                {
                    await Task.Delay(waitTime, _cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
        }

        return true;
    }

    private void LogDiagnosticInfo(ulong frameCount)
    {
        // Log CPU state (simulated)
        var cpuState = new CPUDiagnosticData(
            A: 0x00, X: 0x00, Y: 0x00, PC: 0x8000, SP: 0xFF, P: 0x24,
            CycleCount: _cycleTimer.CycleCount, IRQPending: false, NMIPending: false
        );
        _diagnosticLogger.LogCPUState(cpuState);

        // Log PPU state (simulated)
        var ppuState = new PPUDiagnosticData(
            Scanline: (ushort)(frameCount % 262), Cycle: 0, FrameCount: frameCount,
            PPUCTRL: 0x00, PPUMASK: 0x00, PPUSTATUS: 0x80,
            VRAMAddress: 0x0000, VRAMBuffer: 0x00
        );
        _diagnosticLogger.LogPPUState(ppuState);

        // Log timing analysis
        var timingData = new TimingDiagnosticData(
            FrameNumber: frameCount,
            CPUCycles: _cycleTimer.CycleCount,
            PPUCycles: _cycleTimer.CycleCount * 3, // PPU runs at 3x CPU speed
            FrameTimeMs: 1000.0 / CycleTimer.FrameRate,
            ActualFPS: CycleTimer.FrameRate,
            TargetFPS: CycleTimer.FrameRate
        );
        _diagnosticLogger.LogTimingAnalysis(timingData);
    }

    private static ulong CalculateTargetCycles(EmulationSessionOptions options)
    {
        if (options.MaxCycles.HasValue)
        {
            return options.MaxCycles.Value;
        }

        if (options.MaxFrames.HasValue)
        {
            return options.MaxFrames.Value * CycleTimer.GetCyclesPerFrame();
        }

        if (options.MaxDuration.HasValue)
        {
            return CycleTimer.CalculateCyclesForTime(options.MaxDuration.Value);
        }

        return ulong.MaxValue; // Run indefinitely
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogInformation("Ctrl+C pressed - initiating graceful shutdown...");
        #pragma warning restore CA1848
        e.Cancel = true; // Prevent immediate termination
        _cancellationTokenSource.Cancel();
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            System.Console.CancelKeyPress -= OnCancelKeyPress;
            _cancellationTokenSource?.Dispose();
            _cycleTimer?.Stop();
            _disposed = true;
        }
    }
}

/// <summary>
/// Options for emulation session configuration
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for configuration")]
internal sealed class EmulationSessionOptions
{
    public ulong? MaxCycles { get; set; }
    public ulong? MaxFrames { get; set; }
    public TimeSpan? MaxDuration { get; set; }
    public TimeSpan? DiagnosticInterval { get; set; } = TimeSpan.FromSeconds(1);
    public bool EnableFrameRateLimit { get; set; }
    public DiagnosticLoggerOptions? DiagnosticOptions { get; set; }
}

/// <summary>
/// Result of an emulation session
/// </summary>
internal sealed record EmulationSessionResult(
    bool Success,
    Guid SessionId,
    TimeSpan Duration,
    ulong TotalCycles,
    ulong TotalFrames,
    string? ErrorMessage
);
