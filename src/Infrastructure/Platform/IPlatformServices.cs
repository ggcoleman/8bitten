using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EightBitten.Infrastructure.Platform;

/// <summary>
/// Platform abstraction layer for cross-platform functionality
/// </summary>
public interface IPlatformServices
{
    /// <summary>
    /// Current platform information
    /// </summary>
    PlatformInfo Platform { get; }

    /// <summary>
    /// File system operations
    /// </summary>
    IFileSystemService FileSystem { get; }

    /// <summary>
    /// Audio output services
    /// </summary>
    IAudioService Audio { get; }

    /// <summary>
    /// Video output services
    /// </summary>
    IVideoService Video { get; }

    /// <summary>
    /// Input handling services
    /// </summary>
    IInputService Input { get; }

    /// <summary>
    /// High-resolution timing services
    /// </summary>
    ITimingService Timing { get; }

    /// <summary>
    /// Threading and synchronization services
    /// </summary>
    IThreadingService Threading { get; }

    /// <summary>
    /// Initialize platform services
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing initialization</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Shutdown platform services
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing shutdown</returns>
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Platform information
/// </summary>
public class PlatformInfo
{
    /// <summary>
    /// Operating system type
    /// </summary>
    public OperatingSystem OS { get; set; }

    /// <summary>
    /// CPU architecture
    /// </summary>
    public Architecture Architecture { get; set; }

    /// <summary>
    /// Number of logical CPU cores
    /// </summary>
    public int LogicalCores { get; set; }

    /// <summary>
    /// Available system memory in bytes
    /// </summary>
    public long AvailableMemory { get; set; }

    /// <summary>
    /// Whether high-resolution timing is available
    /// </summary>
    public bool HighResolutionTiming { get; set; }

    /// <summary>
    /// Platform-specific capabilities
    /// </summary>
    public PlatformCapabilities Capabilities { get; set; } = new();

    public override string ToString()
    {
        return $"{OS} {Architecture}, {LogicalCores} cores, {AvailableMemory / (1024 * 1024 * 1024):F1}GB RAM";
    }
}

/// <summary>
/// Operating system enumeration
/// </summary>
public enum OperatingSystem
{
    Windows,
    macOS,
    Linux,
    FreeBSD,
    Unknown
}

/// <summary>
/// CPU architecture enumeration
/// </summary>
public enum Architecture
{
    x86,
    x64,
    ARM,
    ARM64,
    Unknown
}

/// <summary>
/// Platform-specific capabilities
/// </summary>
public class PlatformCapabilities
{
    /// <summary>
    /// Audio capabilities
    /// </summary>
    public AudioCapabilities Audio { get; set; } = new();

    /// <summary>
    /// Video capabilities
    /// </summary>
    public VideoCapabilities Video { get; set; } = new();

    /// <summary>
    /// Input capabilities
    /// </summary>
    public InputCapabilities Input { get; set; } = new();
}

/// <summary>
/// Audio system capabilities
/// </summary>
public class AudioCapabilities
{
    /// <summary>
    /// Supported sample rates
    /// </summary>
    public IReadOnlyList<int> SupportedSampleRates { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Supported channel counts
    /// </summary>
    public IReadOnlyList<int> SupportedChannels { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Minimum buffer size in samples
    /// </summary>
    public int MinBufferSize { get; set; }

    /// <summary>
    /// Maximum buffer size in samples
    /// </summary>
    public int MaxBufferSize { get; set; }

    /// <summary>
    /// Audio latency in milliseconds
    /// </summary>
    public double LatencyMs { get; set; }
}

/// <summary>
/// Video system capabilities
/// </summary>
public class VideoCapabilities
{
    /// <summary>
    /// Supported display resolutions
    /// </summary>
    public IReadOnlyList<Resolution> SupportedResolutions { get; set; } = Array.Empty<Resolution>();

    /// <summary>
    /// Supported refresh rates
    /// </summary>
    public IReadOnlyList<double> SupportedRefreshRates { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Whether VSync is supported
    /// </summary>
    public bool SupportsVSync { get; set; }

    /// <summary>
    /// Whether fullscreen is supported
    /// </summary>
    public bool SupportsFullscreen { get; set; }

    /// <summary>
    /// Hardware acceleration support
    /// </summary>
    public bool HardwareAcceleration { get; set; }
}

/// <summary>
/// Input system capabilities
/// </summary>
public class InputCapabilities
{
    /// <summary>
    /// Whether keyboard input is supported
    /// </summary>
    public bool SupportsKeyboard { get; set; }

    /// <summary>
    /// Whether mouse input is supported
    /// </summary>
    public bool SupportsMouse { get; set; }

    /// <summary>
    /// Whether gamepad input is supported
    /// </summary>
    public bool SupportsGamepad { get; set; }

    /// <summary>
    /// Number of supported gamepads
    /// </summary>
    public int MaxGamepads { get; set; }

    /// <summary>
    /// Input polling rate in Hz
    /// </summary>
    public double PollingRate { get; set; }
}

/// <summary>
/// Display resolution
/// </summary>
public readonly struct Resolution : IEquatable<Resolution>
{
    public int Width { get; }
    public int Height { get; }

    public Resolution(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public bool Equals(Resolution other)
    {
        return Width == other.Width && Height == other.Height;
    }

    public override bool Equals(object? obj)
    {
        return obj is Resolution other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height);
    }

    public static bool operator ==(Resolution left, Resolution right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Resolution left, Resolution right)
    {
        return !left.Equals(right);
    }

    public override string ToString() => $"{Width}x{Height}";
}

/// <summary>
/// File system operations interface
/// </summary>
public interface IFileSystemService
{
    /// <summary>
    /// Get application data directory
    /// </summary>
    /// <returns>Application data directory path</returns>
    string GetApplicationDataDirectory();

    /// <summary>
    /// Get user documents directory
    /// </summary>
    /// <returns>User documents directory path</returns>
    string GetDocumentsDirectory();

    /// <summary>
    /// Get temporary directory
    /// </summary>
    /// <returns>Temporary directory path</returns>
    string GetTemporaryDirectory();

    /// <summary>
    /// Ensure directory exists
    /// </summary>
    /// <param name="path">Directory path</param>
    void EnsureDirectoryExists(string path);

    /// <summary>
    /// Read file as bytes asynchronously
    /// </summary>
    /// <param name="path">File path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File contents as byte array</returns>
    Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Write bytes to file asynchronously
    /// </summary>
    /// <param name="path">File path</param>
    /// <param name="data">Data to write</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the write operation</returns>
    Task WriteAllBytesAsync(string path, byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if file exists
    /// </summary>
    /// <param name="path">File path</param>
    /// <returns>True if file exists</returns>
    bool FileExists(string path);

    /// <summary>
    /// Get file size in bytes
    /// </summary>
    /// <param name="path">File path</param>
    /// <returns>File size in bytes</returns>
    long GetFileSize(string path);
}

/// <summary>
/// Audio output service interface
/// </summary>
public interface IAudioService
{
    /// <summary>
    /// Audio capabilities
    /// </summary>
    AudioCapabilities Capabilities { get; }

    /// <summary>
    /// Whether audio is currently playing
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Current sample rate
    /// </summary>
    int SampleRate { get; }

    /// <summary>
    /// Current channel count
    /// </summary>
    int Channels { get; }

    /// <summary>
    /// Initialize audio system
    /// </summary>
    /// <param name="sampleRate">Sample rate in Hz</param>
    /// <param name="channels">Number of channels</param>
    /// <param name="bufferSize">Buffer size in samples</param>
    /// <returns>True if initialized successfully</returns>
    bool Initialize(int sampleRate, int channels, int bufferSize);

    /// <summary>
    /// Start audio playback
    /// </summary>
    void Start();

    /// <summary>
    /// Stop audio playback
    /// </summary>
    void StopPlayback();

    /// <summary>
    /// Queue audio samples for playback
    /// </summary>
    /// <param name="samples">Audio samples</param>
    void QueueSamples(ReadOnlySpan<float> samples);

    /// <summary>
    /// Get current playback latency
    /// </summary>
    /// <returns>Latency in milliseconds</returns>
    double GetLatency();
}

/// <summary>
/// Video output service interface
/// </summary>
public interface IVideoService
{
    /// <summary>
    /// Video capabilities
    /// </summary>
    VideoCapabilities Capabilities { get; }

    /// <summary>
    /// Current display resolution
    /// </summary>
    Resolution Resolution { get; }

    /// <summary>
    /// Whether in fullscreen mode
    /// </summary>
    bool IsFullscreen { get; }

    /// <summary>
    /// Initialize video system
    /// </summary>
    /// <param name="width">Display width</param>
    /// <param name="height">Display height</param>
    /// <param name="fullscreen">Whether to start in fullscreen</param>
    /// <returns>True if initialized successfully</returns>
    bool Initialize(int width, int height, bool fullscreen = false);

    /// <summary>
    /// Present a frame to the display
    /// </summary>
    /// <param name="frameData">Frame pixel data (RGBA)</param>
    /// <param name="width">Frame width</param>
    /// <param name="height">Frame height</param>
    void PresentFrame(ReadOnlySpan<uint> frameData, int width, int height);

    /// <summary>
    /// Toggle fullscreen mode
    /// </summary>
    void ToggleFullscreen();

    /// <summary>
    /// Set VSync enabled/disabled
    /// </summary>
    /// <param name="enabled">Whether to enable VSync</param>
    void SetVSync(bool enabled);
}

/// <summary>
/// Input handling service interface
/// </summary>
public interface IInputService
{
    /// <summary>
    /// Input capabilities
    /// </summary>
    InputCapabilities Capabilities { get; }

    /// <summary>
    /// Poll for input updates
    /// </summary>
    void Poll();

    /// <summary>
    /// Check if a key is currently pressed
    /// </summary>
    /// <param name="key">Key to check</param>
    /// <returns>True if key is pressed</returns>
    bool IsKeyPressed(Key key);

    /// <summary>
    /// Check if a gamepad button is pressed
    /// </summary>
    /// <param name="player">Player number (0-3)</param>
    /// <param name="button">Button to check</param>
    /// <returns>True if button is pressed</returns>
    bool IsGamepadButtonPressed(int player, GamepadButton button);

    /// <summary>
    /// Event fired when input state changes
    /// </summary>
    event EventHandler<InputEventArgs>? InputChanged;
}

/// <summary>
/// High-resolution timing service interface
/// </summary>
public interface ITimingService
{
    /// <summary>
    /// Get high-resolution timestamp
    /// </summary>
    /// <returns>Timestamp in ticks</returns>
    long GetTimestamp();

    /// <summary>
    /// Get timestamp frequency (ticks per second)
    /// </summary>
    /// <returns>Frequency in Hz</returns>
    long GetFrequency();

    /// <summary>
    /// Sleep for a precise duration
    /// </summary>
    /// <param name="duration">Duration to sleep</param>
    void PreciseSleep(TimeSpan duration);
}

/// <summary>
/// Threading and synchronization service interface
/// </summary>
public interface IThreadingService
{
    /// <summary>
    /// Create a high-priority thread for emulation
    /// </summary>
    /// <param name="action">Action to run on thread</param>
    /// <param name="name">Thread name</param>
    /// <returns>Thread handle</returns>
    Thread CreateEmulationThread(Action action, string name);

    /// <summary>
    /// Set thread affinity to specific CPU cores
    /// </summary>
    /// <param name="thread">Thread to set affinity for</param>
    /// <param name="coreIds">CPU core IDs</param>
    void SetThreadAffinity(Thread thread, int[] coreIds);

    /// <summary>
    /// Create a high-resolution timer
    /// </summary>
    /// <param name="interval">Timer interval</param>
    /// <param name="callback">Callback to invoke</param>
    /// <returns>Timer handle</returns>
    IDisposable CreateHighResolutionTimer(TimeSpan interval, Action callback);
}

/// <summary>
/// Key enumeration for input
/// </summary>
public enum Key
{
    Unknown,
    A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,
    Space, Enter, Escape, Tab, Backspace, Delete,
    Left, Right, Up, Down,
    LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt
}

/// <summary>
/// Gamepad button enumeration
/// </summary>
public enum GamepadButton
{
    A, B, X, Y,
    DPadUp, DPadDown, DPadLeft, DPadRight,
    LeftShoulder, RightShoulder,
    LeftStick, RightStick,
    Start, Back
}

/// <summary>
/// Input event arguments
/// </summary>
public class InputEventArgs : EventArgs
{
    public Key Key { get; set; }
    public GamepadButton Button { get; set; }
    public int Player { get; set; }
    public bool IsPressed { get; set; }
}
