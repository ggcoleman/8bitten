using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace EightBitten.Infrastructure.Configuration;

/// <summary>
/// Main configuration class for 8Bitten emulator
/// </summary>
public class EmulatorConfiguration
{
    /// <summary>
    /// Core emulation settings
    /// </summary>
    public CoreSettings Core { get; set; } = new();

    /// <summary>
    /// Performance and timing settings
    /// </summary>
    public PerformanceSettings Performance { get; set; } = new();

    /// <summary>
    /// Audio configuration
    /// </summary>
    public AudioSettings Audio { get; set; } = new();

    /// <summary>
    /// Video configuration
    /// </summary>
    public VideoSettings Video { get; set; } = new();

    /// <summary>
    /// Input configuration
    /// </summary>
    public InputSettings Input { get; set; } = new();

    /// <summary>
    /// Research and analysis settings
    /// </summary>
    public ResearchSettings Research { get; set; } = new();

    /// <summary>
    /// MCP interface settings
    /// </summary>
    public McpSettings Mcp { get; set; } = new();
}

/// <summary>
/// Core emulation settings
/// </summary>
public class CoreSettings
{
    /// <summary>
    /// Enable cycle-accurate timing (default: true)
    /// </summary>
    public bool CycleAccurateTiming { get; set; } = true;

    /// <summary>
    /// Performance mode: Maximum, Balanced, Performance
    /// </summary>
    public string PerformanceMode { get; set; } = "Maximum";

    /// <summary>
    /// Enable hardware quirk reproduction
    /// </summary>
    public bool EnableHardwareQuirks { get; set; } = true;

    /// <summary>
    /// Default ROM directory
    /// </summary>
    public string RomDirectory { get; set; } = "./roms";

    /// <summary>
    /// Save state directory
    /// </summary>
    public string SaveStateDirectory { get; set; } = "./saves";
}

/// <summary>
/// Performance and timing settings
/// </summary>
public class PerformanceSettings
{
    /// <summary>
    /// Target frame rate (default: 60.0988 FPS for NTSC)
    /// </summary>
    public double TargetFrameRate { get; set; } = 60.0988;

    /// <summary>
    /// Enable frame limiting
    /// </summary>
    public bool EnableFrameLimiting { get; set; } = true;

    /// <summary>
    /// Maximum input latency in milliseconds
    /// </summary>
    public double MaxInputLatency { get; set; } = 16.67;

    /// <summary>
    /// Enable performance metrics collection
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Metrics collection interval in milliseconds
    /// </summary>
    public int MetricsInterval { get; set; } = 1000;
}

/// <summary>
/// Audio configuration
/// </summary>
public class AudioSettings
{
    /// <summary>
    /// Enable audio output
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Sample rate in Hz
    /// </summary>
    public int SampleRate { get; set; } = 44100;

    /// <summary>
    /// Buffer size in samples
    /// </summary>
    public int BufferSize { get; set; } = 1024;

    /// <summary>
    /// Master volume (0.0 to 1.0)
    /// </summary>
    public float Volume { get; set; } = 0.8f;

    /// <summary>
    /// Enable low-pass filter
    /// </summary>
    public bool EnableLowPassFilter { get; set; } = true;
}

/// <summary>
/// Video configuration
/// </summary>
public class VideoSettings
{
    /// <summary>
    /// Enable video output
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Window width in pixels
    /// </summary>
    public int WindowWidth { get; set; } = 768;

    /// <summary>
    /// Window height in pixels
    /// </summary>
    public int WindowHeight { get; set; } = 720;

    /// <summary>
    /// Enable fullscreen mode
    /// </summary>
    public bool Fullscreen { get; set; }

    /// <summary>
    /// Enable VSync
    /// </summary>
    public bool VSync { get; set; } = true;

    /// <summary>
    /// Scaling filter: Nearest, Linear, CRT
    /// </summary>
    public string ScalingFilter { get; set; } = "Nearest";
}

/// <summary>
/// Input configuration
/// </summary>
public class InputSettings
{
    /// <summary>
    /// Player 1 controller mapping
    /// </summary>
    public ControllerMapping Player1 { get; set; } = new();

    /// <summary>
    /// Player 2 controller mapping
    /// </summary>
    public ControllerMapping Player2 { get; set; } = new();

    /// <summary>
    /// Enable input recording
    /// </summary>
    public bool EnableRecording { get; set; }

    /// <summary>
    /// Input recording directory
    /// </summary>
    public string RecordingDirectory { get; set; } = "./recordings";
}

/// <summary>
/// Controller button mapping
/// </summary>
public class ControllerMapping
{
    public string A { get; set; } = "Z";
    public string B { get; set; } = "X";
    public string Select { get; set; } = "RShift";
    public string Start { get; set; } = "Enter";
    public string Up { get; set; } = "Up";
    public string Down { get; set; } = "Down";
    public string Left { get; set; } = "Left";
    public string Right { get; set; } = "Right";
}

/// <summary>
/// Research and analysis settings
/// </summary>
public class ResearchSettings
{
    /// <summary>
    /// Enable research mode
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Data export directory
    /// </summary>
    public string ExportDirectory { get; set; } = "./research-data";

    /// <summary>
    /// Enable deterministic replay
    /// </summary>
    public bool EnableDeterministicReplay { get; set; } = true;

    /// <summary>
    /// Export format: CSV, JSON, HDF5, Binary
    /// </summary>
    public string ExportFormat { get; set; } = "JSON";

    /// <summary>
    /// Metrics collection frequency (every N frames)
    /// </summary>
    public int MetricsFrequency { get; set; } = 1;
}

/// <summary>
/// MCP interface settings
/// </summary>
public class McpSettings
{
    /// <summary>
    /// Enable MCP interface
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// MCP server port
    /// </summary>
    public int Port { get; set; } = 8080;

    /// <summary>
    /// Enable authentication
    /// </summary>
    public bool EnableAuthentication { get; set; } = true;

    /// <summary>
    /// JWT secret key
    /// </summary>
    public string JwtSecret { get; set; } = "";

    /// <summary>
    /// Token expiration time in hours
    /// </summary>
    public int TokenExpirationHours { get; set; } = 24;

    /// <summary>
    /// Rate limiting: requests per minute
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 100;
}

/// <summary>
/// Configuration service for managing emulator settings
/// </summary>
public class ConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly string _configPath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;
        _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "8Bitten",
            "config.json"
        );
    }

    /// <summary>
    /// Load configuration from file or create default
    /// </summary>
    public EmulatorConfiguration LoadConfiguration()
    {
        if (File.Exists(_configPath))
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<EmulatorConfiguration>(json) ?? new EmulatorConfiguration();
        }

        var defaultConfig = new EmulatorConfiguration();
        SaveConfiguration(defaultConfig);
        return defaultConfig;
    }

    /// <summary>
    /// Save configuration to file
    /// </summary>
    public void SaveConfiguration(EmulatorConfiguration configuration)
    {
        var directory = Path.GetDirectoryName(_configPath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(configuration, JsonOptions);
        File.WriteAllText(_configPath, json);
    }
}

/// <summary>
/// Extension methods for configuration setup
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Add emulator configuration services
    /// </summary>
    public static IServiceCollection AddEmulatorConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ConfigurationService>();
        services.AddSingleton(provider =>
        {
            var configService = provider.GetRequiredService<ConfigurationService>();
            return configService.LoadConfiguration();
        });

        return services;
    }
}
