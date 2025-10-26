using System;
using System.Threading;
using System.Threading.Tasks;

namespace EightBitten.Core.Contracts;

/// <summary>
/// Core emulator interface defining the main emulation lifecycle and control
/// </summary>
public interface IEmulator : IDisposable
{
    /// <summary>
    /// Current emulation state
    /// </summary>
    EmulatorState State { get; }

    /// <summary>
    /// Current frame number since emulation start
    /// </summary>
    long FrameNumber { get; }

    /// <summary>
    /// Current cycle count since emulation start
    /// </summary>
    long CycleCount { get; }

    /// <summary>
    /// Whether the emulator is running in headless mode (no graphics/audio output)
    /// </summary>
    bool IsHeadless { get; }

    /// <summary>
    /// Load a ROM cartridge into the emulator
    /// </summary>
    /// <param name="romData">ROM file data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if ROM loaded successfully</returns>
    Task<bool> LoadRomAsync(ReadOnlyMemory<byte> romData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Start emulation
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the emulation execution</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pause emulation
    /// </summary>
    void Pause();

    /// <summary>
    /// Resume emulation from paused state
    /// </summary>
    void ResumeEmulation();

    /// <summary>
    /// Stop emulation and reset to initial state
    /// </summary>
    void StopEmulation();

    /// <summary>
    /// Reset the emulated system to power-on state
    /// </summary>
    void Reset();

    /// <summary>
    /// Execute a single frame of emulation
    /// </summary>
    /// <returns>True if frame executed successfully</returns>
    bool StepFrame();

    /// <summary>
    /// Execute a single CPU instruction
    /// </summary>
    /// <returns>Number of cycles executed</returns>
    int StepInstruction();

    /// <summary>
    /// Save current emulation state
    /// </summary>
    /// <returns>Serialized state data</returns>
    Task<byte[]> SaveStateAsync();

    /// <summary>
    /// Load emulation state from saved data
    /// </summary>
    /// <param name="stateData">Serialized state data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if state loaded successfully</returns>
    Task<bool> LoadStateAsync(ReadOnlyMemory<byte> stateData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event fired when emulation state changes
    /// </summary>
    event EventHandler<EmulatorStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Event fired when a frame is completed
    /// </summary>
    event EventHandler<FrameCompletedEventArgs>? FrameCompleted;

    /// <summary>
    /// Event fired when an error occurs during emulation
    /// </summary>
    event EventHandler<EmulationErrorEventArgs>? EmulationError;
}

/// <summary>
/// Emulator execution state
/// </summary>
public enum EmulatorState
{
    /// <summary>
    /// Emulator is not initialized
    /// </summary>
    Uninitialized,

    /// <summary>
    /// Emulator is ready but not running
    /// </summary>
    Ready,

    /// <summary>
    /// Emulator is actively running
    /// </summary>
    Running,

    /// <summary>
    /// Emulator is paused
    /// </summary>
    Paused,

    /// <summary>
    /// Emulator has stopped
    /// </summary>
    Stopped,

    /// <summary>
    /// Emulator encountered an error
    /// </summary>
    Error
}

/// <summary>
/// Event arguments for emulator state changes
/// </summary>
public class EmulatorStateChangedEventArgs : EventArgs
{
    public EmulatorState PreviousState { get; }
    public EmulatorState NewState { get; }
    public string? Reason { get; }

    public EmulatorStateChangedEventArgs(EmulatorState previousState, EmulatorState newState, string? reason = null)
    {
        PreviousState = previousState;
        NewState = newState;
        Reason = reason;
    }
}

/// <summary>
/// Event arguments for frame completion
/// </summary>
public class FrameCompletedEventArgs : EventArgs
{
    public long FrameNumber { get; }
    public long CycleCount { get; }
    public TimeSpan FrameTime { get; }

    public FrameCompletedEventArgs(long frameNumber, long cycleCount, TimeSpan frameTime)
    {
        FrameNumber = frameNumber;
        CycleCount = cycleCount;
        FrameTime = frameTime;
    }
}

/// <summary>
/// Event arguments for emulation errors
/// </summary>
public class EmulationErrorEventArgs : EventArgs
{
    public Exception Exception { get; }
    public string Context { get; }
    public bool IsFatal { get; }

    public EmulationErrorEventArgs(Exception exception, string context, bool isFatal = false)
    {
        Exception = exception;
        Context = context;
        IsFatal = isFatal;
    }
}
