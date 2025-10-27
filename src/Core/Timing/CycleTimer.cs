using System;

namespace EightBitten.Core.Timing;

/// <summary>
/// Cycle-accurate timing system with NTSC timing (1.789773 MHz CPU clock)
/// Provides precise timing coordination for emulation components
/// </summary>
public class CycleTimer
{
    /// <summary>
    /// NTSC CPU clock frequency in Hz (1.789773 MHz)
    /// </summary>
    public const double NTSCCPUClockFrequency = 1789773.0;

    /// <summary>
    /// NTSC frame rate (approximately 60.0988 FPS)
    /// </summary>
    public const double NTSCFrameRate = 60.0988;

    private ulong _cycleCount;
    private bool _isRunning;

    /// <summary>
    /// Current CPU clock frequency
    /// </summary>
    public static double CPUClockFrequency => NTSCCPUClockFrequency;

    /// <summary>
    /// Current cycle count
    /// </summary>
    public ulong CycleCount => _cycleCount;

    /// <summary>
    /// Whether the timer is currently running
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Start the timer
    /// </summary>
    public void Start()
    {
        _isRunning = true;
    }

    /// <summary>
    /// Stop the timer
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
    }

    /// <summary>
    /// Reset the timer to initial state
    /// </summary>
    public void Reset()
    {
        _cycleCount = 0;
        _isRunning = false;
    }

    /// <summary>
    /// Execute the specified number of cycles
    /// </summary>
    /// <param name="cycles">Number of cycles to execute</param>
    /// <exception cref="InvalidOperationException">Thrown when timer is not running</exception>
    public void ExecuteCycles(int cycles)
    {
        if (!_isRunning)
        {
            throw new InvalidOperationException("Timer is not running");
        }

        if (cycles < 0)
        {
            throw new ArgumentException("Cycles must be non-negative", nameof(cycles));
        }

        _cycleCount += (ulong)cycles;
    }

    /// <summary>
    /// Get elapsed time based on current cycle count
    /// </summary>
    /// <returns>Elapsed time</returns>
    public TimeSpan GetElapsedTime()
    {
        var seconds = _cycleCount / CPUClockFrequency;
        return TimeSpan.FromSeconds(seconds);
    }

    /// <summary>
    /// Calculate number of cycles needed for the specified time
    /// </summary>
    /// <param name="time">Target time</param>
    /// <returns>Number of cycles</returns>
    public static ulong CalculateCyclesForTime(TimeSpan time)
    {
        var cycles = time.TotalSeconds * CPUClockFrequency;
        return (ulong)Math.Round(cycles);
    }

    /// <summary>
    /// Synchronize with target cycle count
    /// </summary>
    /// <param name="targetCycles">Target cycle count</param>
    /// <returns>Time to wait if ahead of target, or TimeSpan.Zero if behind</returns>
    public TimeSpan SynchronizeWithTarget(ulong targetCycles)
    {
        if (_cycleCount <= targetCycles)
        {
            return TimeSpan.Zero; // We're behind or at target, no wait needed
        }

        // We're ahead of target, calculate wait time
        var excessCycles = _cycleCount - targetCycles;
        var waitSeconds = excessCycles / CPUClockFrequency;
        return TimeSpan.FromSeconds(waitSeconds);
    }

    /// <summary>
    /// Get number of cycles per frame for NTSC timing
    /// </summary>
    /// <returns>Cycles per frame</returns>
    public static ulong GetCyclesPerFrame()
    {
        return (ulong)Math.Round(CPUClockFrequency / NTSCFrameRate);
    }

    /// <summary>
    /// Current frame rate in FPS
    /// </summary>
    public static double FrameRate => NTSCFrameRate;

    /// <summary>
    /// Check if current cycle count is at a frame boundary
    /// </summary>
    /// <returns>True if at frame boundary</returns>
    public bool IsFrameBoundary()
    {
        var cyclesPerFrame = GetCyclesPerFrame();
        return _cycleCount % cyclesPerFrame == 0 && _cycleCount > 0;
    }

    /// <summary>
    /// Get current frame number (0-indexed)
    /// </summary>
    /// <returns>Current frame number</returns>
    public ulong GetCurrentFrame()
    {
        var cyclesPerFrame = GetCyclesPerFrame();
        return _cycleCount / cyclesPerFrame;
    }

    /// <summary>
    /// Get cycles remaining until next frame boundary
    /// </summary>
    /// <returns>Cycles until next frame</returns>
    public ulong GetCyclesUntilNextFrame()
    {
        var cyclesPerFrame = GetCyclesPerFrame();
        var currentFrameCycles = _cycleCount % cyclesPerFrame;
        return cyclesPerFrame - currentFrameCycles;
    }

    /// <summary>
    /// Get progress through current frame (0.0 to 1.0)
    /// </summary>
    /// <returns>Frame progress as percentage</returns>
    public double GetFrameProgress()
    {
        var cyclesPerFrame = GetCyclesPerFrame();
        var currentFrameCycles = _cycleCount % cyclesPerFrame;
        return (double)currentFrameCycles / cyclesPerFrame;
    }
}
