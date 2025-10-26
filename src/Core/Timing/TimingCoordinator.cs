using System;
using System.Collections.Generic;
using System.Diagnostics;
using EightBitten.Core.Contracts;
using Microsoft.Extensions.Logging;

namespace EightBitten.Core.Timing;

/// <summary>
/// Coordinates timing between all emulated components for cycle-accurate emulation
/// </summary>
public class TimingCoordinator : IDisposable
{
    private readonly ILogger<TimingCoordinator> _logger;
    private readonly List<IClockedComponent> _components = new();
    private readonly Stopwatch _stopwatch = new();
    
    private long _masterCycles;
    private double _cyclesPerSecond;
    private bool _isRunning;
    private bool _disposed;

    /// <summary>
    /// Master clock frequency in Hz (NTSC NES CPU frequency)
    /// </summary>
    public const double NtscCpuFrequency = 1789773.0; // ~1.79 MHz

    /// <summary>
    /// PAL clock frequency in Hz
    /// </summary>
    public const double PalCpuFrequency = 1662607.0; // ~1.66 MHz

    /// <summary>
    /// PPU frequency multiplier (PPU runs 3x faster than CPU)
    /// </summary>
    public const double PpuFrequencyMultiplier = 3.0;

    /// <summary>
    /// Current master cycle count
    /// </summary>
    public long MasterCycles => _masterCycles;

    /// <summary>
    /// Current timing mode (NTSC/PAL)
    /// </summary>
    public TimingMode Mode { get; private set; } = TimingMode.NTSC;

    /// <summary>
    /// Whether timing coordinator is running
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Current cycles per second based on timing mode
    /// </summary>
    public double CyclesPerSecond => _cyclesPerSecond;

    /// <summary>
    /// Event fired when a frame is completed (based on PPU timing)
    /// </summary>
    public event EventHandler<FrameCompletedEventArgs>? FrameCompleted;

    /// <summary>
    /// Fire frame completed event
    /// </summary>
    protected virtual void OnFrameCompleted(long frameNumber, long cycleCount, TimeSpan frameTime)
    {
        FrameCompleted?.Invoke(this, new FrameCompletedEventArgs(frameNumber, cycleCount, frameTime));
    }

    public TimingCoordinator(ILogger<TimingCoordinator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        SetTimingMode(TimingMode.NTSC);
    }

    /// <summary>
    /// Set the timing mode (NTSC or PAL)
    /// </summary>
    /// <param name="mode">Timing mode to use</param>
    public void SetTimingMode(TimingMode mode)
    {
        Mode = mode;
        _cyclesPerSecond = mode == TimingMode.NTSC ? NtscCpuFrequency : PalCpuFrequency;

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogInformation("Timing mode set to {Mode} ({Frequency:F0} Hz)", mode, _cyclesPerSecond);
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Register a clocked component for timing coordination
    /// </summary>
    /// <param name="component">Component to register</param>
    public void RegisterComponent(IClockedComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);

        if (_components.Contains(component))
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogWarning("Component {ComponentName} is already registered", component.Name);
            #pragma warning restore CA1848
            return;
        }

        _components.Add(component);
        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogDebug("Registered component {ComponentName} with frequency {Frequency:F0} Hz",
            component.Name, component.ClockFrequency);
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Unregister a clocked component
    /// </summary>
    /// <param name="component">Component to unregister</param>
    public void UnregisterComponent(IClockedComponent component)
    {
        if (component == null)
            return;

        if (_components.Remove(component))
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogDebug("Unregistered component {ComponentName}", component.Name);
            #pragma warning restore CA1848
        }
    }

    /// <summary>
    /// Start timing coordination
    /// </summary>
    public void Start()
    {
        if (_isRunning)
            return;

        _isRunning = true;
        _stopwatch.Restart();
        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogInformation("Timing coordinator started");
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Stop timing coordination
    /// </summary>
    public void Stop()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _stopwatch.Stop();
        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogInformation("Timing coordinator stopped at {Cycles} cycles", _masterCycles);
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Reset timing to initial state
    /// </summary>
    public void Reset()
    {
        _masterCycles = 0;
        _stopwatch.Reset();
        
        // Reset all registered components
        foreach (var component in _components)
        {
            component.Reset();
        }
        
        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogInformation("Timing coordinator reset");
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Execute a single master cycle, updating all components
    /// </summary>
    /// <returns>True if cycle executed successfully</returns>
    public bool ExecuteCycle()
    {
        if (!_isRunning)
            return false;

        try
        {
            _masterCycles++;

            // Update all registered components
            foreach (var component in _components)
            {
                if (component.IsEnabled)
                {
                    component.SynchronizeClock(_masterCycles);
                }
            }

            return true;
        }
        catch (InvalidOperationException ex)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogError(ex, "Invalid operation during cycle {Cycle}", _masterCycles);
            #pragma warning restore CA1848
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogError(ex, "Unexpected error executing cycle {Cycle}", _masterCycles);
            #pragma warning restore CA1848
            return false;
        }
    }

    /// <summary>
    /// Execute multiple cycles
    /// </summary>
    /// <param name="cycles">Number of cycles to execute</param>
    /// <returns>Number of cycles actually executed</returns>
    public int ExecuteCycles(int cycles)
    {
        if (!_isRunning || cycles <= 0)
            return 0;

        int executed = 0;
        for (int i = 0; i < cycles; i++)
        {
            if (ExecuteCycle())
                executed++;
            else
                break;
        }

        return executed;
    }

    /// <summary>
    /// Execute cycles for a specific time duration
    /// </summary>
    /// <param name="duration">Time duration to execute</param>
    /// <returns>Number of cycles executed</returns>
    public int ExecuteForDuration(TimeSpan duration)
    {
        if (!_isRunning)
            return 0;

        double targetCycles = duration.TotalSeconds * _cyclesPerSecond;
        return ExecuteCycles((int)Math.Round(targetCycles));
    }

    /// <summary>
    /// Execute until a target frame count is reached
    /// </summary>
    /// <param name="targetFrame">Target frame number</param>
    /// <returns>True if target reached</returns>
    public bool ExecuteToFrame(long targetFrame)
    {
        if (!_isRunning)
            return false;

        // Calculate cycles per frame based on timing mode
        double cyclesPerFrame = Mode == TimingMode.NTSC ? 29780.5 : 33247.5; // Approximate
        long targetCycles = (long)(targetFrame * cyclesPerFrame);

        while (_masterCycles < targetCycles && _isRunning)
        {
            if (!ExecuteCycle())
                return false;
        }

        return true;
    }

    /// <summary>
    /// Current timing statistics
    /// </summary>
    public TimingStatistics Statistics
    {
        get
        {
            var elapsed = _stopwatch.Elapsed;
            double actualFrequency = elapsed.TotalSeconds > 0 ? _masterCycles / elapsed.TotalSeconds : 0;
            double efficiency = _cyclesPerSecond > 0 ? actualFrequency / _cyclesPerSecond : 0;

            return new TimingStatistics
            {
                MasterCycles = _masterCycles,
                ElapsedTime = elapsed,
                TargetFrequency = _cyclesPerSecond,
                ActualFrequency = actualFrequency,
                Efficiency = efficiency,
                ComponentCount = _components.Count,
                IsRunning = _isRunning
            };
        }
    }

    /// <summary>
    /// Current timing state for save/load operations
    /// </summary>
    public TimingState State
    {
        get
        {
            return new TimingState
            {
                MasterCycles = _masterCycles,
                Mode = Mode,
                IsRunning = _isRunning,
                ElapsedTicks = _stopwatch.ElapsedTicks
            };
        }
    }

    /// <summary>
    /// Restore timing state from saved data
    /// </summary>
    /// <param name="state">Timing state to restore</param>
    public void SetState(TimingState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        _masterCycles = state.MasterCycles;
        SetTimingMode(state.Mode);
        
        if (state.IsRunning && !_isRunning)
        {
            Start();
        }
        else if (!state.IsRunning && _isRunning)
        {
            Stop();
        }

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogDebug("Timing state restored: {Cycles} cycles, {Mode} mode",
            _masterCycles, Mode);
        #pragma warning restore CA1848
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Stop();
                _components.Clear();
                _stopwatch.Stop();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Timing mode enumeration
/// </summary>
public enum TimingMode
{
    /// <summary>
    /// NTSC timing (60 Hz, ~1.79 MHz)
    /// </summary>
    NTSC,

    /// <summary>
    /// PAL timing (50 Hz, ~1.66 MHz)
    /// </summary>
    PAL
}

/// <summary>
/// Timing statistics for monitoring performance
/// </summary>
public class TimingStatistics
{
    public long MasterCycles { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public double TargetFrequency { get; set; }
    public double ActualFrequency { get; set; }
    public double Efficiency { get; set; }
    public int ComponentCount { get; set; }
    public bool IsRunning { get; set; }

    public override string ToString()
    {
        return $"Cycles: {MasterCycles:N0}, Freq: {ActualFrequency:F0}/{TargetFrequency:F0} Hz ({Efficiency:P1}), " +
               $"Components: {ComponentCount}, Running: {IsRunning}";
    }
}

/// <summary>
/// Timing state for save/load operations
/// </summary>
public class TimingState : ComponentState
{
    public long MasterCycles { get; set; }
    public TimingMode Mode { get; set; }
    public bool IsRunning { get; set; }
    public long ElapsedTicks { get; set; }

    public TimingState()
    {
        ComponentName = "TimingCoordinator";
    }
}
