using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EightBitten.Core.Contracts;
using EightBitten.Core.Timing;
using EightBitten.Core.Emulator;
using EightBitten.Infrastructure.Configuration;
using EightBitten.Infrastructure.Logging;
using EightBitten.Infrastructure.Platform;

namespace EightBitten.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for configuring 8Bitten services in dependency injection container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add all 8Bitten core services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection AddEightBittenCore(this IServiceCollection services, IConfiguration configuration)
    {
        // Add configuration services
        services.AddEmulatorConfiguration(configuration);
        
        // Add logging services
        services.AddLogging(configuration);
        
        // Add platform services
        services.AddPlatformServices();
        
        // Add timing services
        services.AddTimingServices();
        
        // Add core emulation services
        services.AddEmulationServices();
        
        return services;
    }

    /// <summary>
    /// Add platform abstraction services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        // Register platform services as singletons for performance
        services.AddSingleton<IPlatformServices, PlatformServices>();
        
        return services;
    }

    /// <summary>
    /// Add timing coordination services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection AddTimingServices(this IServiceCollection services)
    {
        // Register timing coordinator as singleton
        services.AddSingleton<TimingCoordinator>();
        
        return services;
    }

    /// <summary>
    /// Add core emulation services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection AddEmulationServices(this IServiceCollection services)
    {
        // Register emulator as singleton (one emulator instance per application)
        services.AddSingleton<IEmulator, EightBitten.Core.Emulator.NESEmulator>();
        
        // Register component factories
        // Component factories - commented out until implemented
        // services.AddTransient<ICPUComponentFactory, CPUComponentFactory>();
        // services.AddTransient<IPPUComponentFactory, PPUComponentFactory>();
        // services.AddTransient<IAPUComponentFactory, APUComponentFactory>();
        // services.AddTransient<IMemoryComponentFactory, MemoryComponentFactory>();
        // services.AddTransient<ICartridgeComponentFactory, CartridgeComponentFactory>();
        
        return services;
    }

    /// <summary>
    /// Add headless execution services (no audio/video output)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection AddHeadlessServices(this IServiceCollection services)
    {
        // Override platform services with headless implementations
        // Headless services - commented out until implemented
        // services.AddSingleton<IAudioService, HeadlessAudioService>();
        // services.AddSingleton<IVideoService, HeadlessVideoService>();
        // services.AddSingleton<IInputService, HeadlessInputService>();
        
        return services;
    }

    /// <summary>
    /// Add CLI execution services (with graphics but no GUI)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection AddCLIServices(this IServiceCollection services)
    {
        // Add MonoGame-based services for CLI mode
        // MonoGame services - commented out until implemented
        // services.AddSingleton<IAudioService, MonoGameAudioService>();
        // services.AddSingleton<IVideoService, MonoGameVideoService>();
        // services.AddSingleton<IInputService, MonoGameInputService>();
        
        return services;
    }

    /// <summary>
    /// Add GUI execution services (full Avalonia UI)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection AddGUIServices(this IServiceCollection services)
    {
        // Avalonia services - commented out until implemented
        // services.AddSingleton<IAudioService, AvaloniaAudioService>();
        // services.AddSingleton<IVideoService, AvaloniaVideoService>();
        // services.AddSingleton<IInputService, AvaloniaInputService>();

        // GUI-specific services - commented out until implemented
        // services.AddTransient<IMainWindowViewModel, MainWindowViewModel>();
        // services.AddTransient<IEmulatorControlViewModel, EmulatorControlViewModel>();
        // services.AddTransient<ISettingsViewModel, SettingsViewModel>();
        
        return services;
    }

    /// <summary>
    /// Add research and analysis services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection AddResearchServices(this IServiceCollection services)
    {
        // Metrics collection services - commented out until implemented
        // services.AddSingleton<IMetricsCollector, MetricsCollector>();
        // services.AddSingleton<IPerformanceAnalyzer, PerformanceAnalyzer>();
        // services.AddSingleton<ITimingAnalyzer, TimingAnalyzer>();

        // Recording and replay services - commented out until implemented
        // services.AddSingleton<IInputRecorder, InputRecorder>();
        // services.AddSingleton<IStateRecorder, StateRecorder>();
        // services.AddSingleton<IReplayManager, ReplayManager>();

        // Data export services - commented out until implemented
        // services.AddTransient<IDataExporter, CSVDataExporter>();
        // services.AddTransient<IDataExporter, JSONDataExporter>();
        // services.AddTransient<IDataExporter, HDF5DataExporter>();
        
        return services;
    }

    /// <summary>
    /// Add MCP (AI agent communication) services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection AddMCPServices(this IServiceCollection services)
    {
        // MCP server and communication services - commented out until implemented
        // services.AddSingleton<IMCPServer, MCPServer>();
        // services.AddSingleton<IAgentSessionManager, AgentSessionManager>();
        // services.AddSingleton<IGameStateProvider, GameStateProvider>();

        // Authentication and security services - commented out until implemented
        // services.AddSingleton<IMCPAuthenticationService, JWTAuthenticationService>();
        // services.AddSingleton<IRateLimitingService, RateLimitingService>();
        
        return services;
    }

    /// <summary>
    /// Add testing and validation services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection AddTestingServices(this IServiceCollection services)
    {
        // ROM testing services - commented out until implemented
        // services.AddTransient<IROMValidator, ROMValidator>();
        // services.AddTransient<ITestSuiteRunner, BlarggTestSuiteRunner>();
        // services.AddTransient<ITestSuiteRunner, NESTestSuiteRunner>();

        // Accuracy validation services - commented out until implemented
        // services.AddTransient<IAccuracyValidator, CPUAccuracyValidator>();
        // services.AddTransient<IAccuracyValidator, PPUAccuracyValidator>();
        // services.AddTransient<IAccuracyValidator, APUAccuracyValidator>();

        // Performance testing services - commented out until implemented
        // services.AddTransient<IPerformanceTester, FrameRateTester>();
        // services.AddTransient<IPerformanceTester, LatencyTester>();
        // services.AddTransient<IPerformanceTester, ThroughputTester>();
        
        return services;
    }

    /// <summary>
    /// Configure service lifetimes for optimal performance
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection ConfigureServiceLifetimes(this IServiceCollection services)
    {
        // Configure specific lifetimes for performance-critical services
        
        // Singletons for shared state and expensive initialization
        services.AddSingleton<IEmulator, EightBitten.Core.Emulator.NESEmulator>();
        services.AddSingleton<TimingCoordinator>();
        services.AddSingleton<IPlatformServices, PlatformServices>();
        
        // Scoped for request-based services (MCP) - commented out until implemented
        // services.AddScoped<IAgentSession, AgentSession>();
        // services.AddScoped<IGameStateSnapshot, GameStateSnapshot>();

        // Transient for lightweight, stateless services - commented out until implemented
        // services.AddTransient<IDataExporter, CSVDataExporter>();
        // services.AddTransient<IDataExporter, JSONDataExporter>();
        
        return services;
    }

    /// <summary>
    /// Validate service configuration for common issues
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Validation results</returns>
    public static ServiceValidationResult ValidateServices(this IServiceCollection services)
    {
        var result = new ServiceValidationResult();
        
        // Check for required services
        var requiredServices = new[]
        {
            typeof(IEmulator),
            typeof(TimingCoordinator),
            typeof(IPlatformServices),
            typeof(ConfigurationService)
        };
        
        foreach (var serviceType in requiredServices)
        {
            var registration = services.FirstOrDefault(s => s.ServiceType == serviceType);
            if (registration == null)
            {
                result.Errors.Add($"Required service {serviceType.Name} is not registered");
            }
        }
        
        // Check for circular dependencies (basic check)
        var singletonServices = services.Where(s => s.Lifetime == ServiceLifetime.Singleton).ToList();
        foreach (var service in singletonServices)
        {
            if (service.ImplementationType != null)
            {
                var constructors = service.ImplementationType.GetConstructors();
                foreach (var constructor in constructors)
                {
                    var parameters = constructor.GetParameters();
                    foreach (var parameter in parameters)
                    {
                        if (parameter.ParameterType == service.ServiceType)
                        {
                            result.Warnings.Add($"Potential circular dependency in {service.ServiceType.Name}");
                        }
                    }
                }
            }
        }
        
        result.IsValid = result.Errors.Count == 0;
        return result;
    }
}

/// <summary>
/// Service validation result
/// </summary>
public class ServiceValidationResult
{
    /// <summary>
    /// Whether the service configuration is valid
    /// </summary>
    public bool IsValid { get; set; } = true;
    
    /// <summary>
    /// Validation errors that prevent startup
    /// </summary>
    public ICollection<string> Errors { get; } = new List<string>();

    /// <summary>
    /// Validation warnings that may affect performance
    /// </summary>
    public ICollection<string> Warnings { get; } = new List<string>();
    
    public override string ToString()
    {
        var result = $"Valid: {IsValid}";
        if (Errors.Count > 0)
            result += $", Errors: {Errors.Count}";
        if (Warnings.Count > 0)
            result += $", Warnings: {Warnings.Count}";
        return result;
    }
}

// Placeholder interfaces for services that will be implemented in later phases
// These allow the DI container to be configured without requiring full implementations

#pragma warning disable CA1040 // Avoid empty interfaces - These are placeholders for future implementation
public interface ICPUComponentFactory { }
public interface IPPUComponentFactory { }
public interface IAPUComponentFactory { }
public interface IMemoryComponentFactory { }
public interface ICartridgeComponentFactory { }

public interface IMetricsCollector { }
public interface IPerformanceAnalyzer { }
public interface ITimingAnalyzer { }
public interface IInputRecorder { }
public interface IStateRecorder { }
public interface IReplayManager { }
public interface IDataExporter { }

public interface IMCPServer { }
public interface IAgentSessionManager { }
public interface IGameStateProvider { }
public interface IMCPAuthenticationService { }
public interface IRateLimitingService { }
public interface IAgentSession { }
public interface IGameStateSnapshot { }

public interface IROMValidator { }
public interface ITestSuiteRunner { }
public interface IAccuracyValidator { }
public interface IPerformanceTester { }

public interface IMainWindowViewModel { }
public interface IEmulatorControlViewModel { }
public interface ISettingsViewModel { }
#pragma warning restore CA1040

// Placeholder implementations for platform services
public sealed class PlatformServices : IPlatformServices
{
    public PlatformInfo Platform { get; } = new PlatformInfo();
    public IFileSystemService FileSystem { get; } = new StubFileSystemService();
    public IAudioService Audio { get; } = new StubAudioService();
    public IVideoService Video { get; } = new StubVideoService();
    public IInputService Input { get; } = new StubInputService();
    public ITimingService Timing { get; } = new StubTimingService();
    public IThreadingService Threading { get; } = new StubThreadingService();

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task ShutdownAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

// Note: NESEmulator implementation is in EightBitten.Core.Emulator.NESEmulator

// Stub service implementations
#pragma warning disable CA1822 // Mark members as static - These are interface implementations
public sealed class StubFileSystemService : IFileSystemService
{
    public string[] GetSupportedExtensions() => Array.Empty<string>();
    public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default) => Task.FromResult(Array.Empty<byte>());
    public Task WriteAllBytesAsync(string path, byte[] data, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public bool FileExists(string path) => false;
    public bool DirectoryExists(string path) => false;
    public void CreateDirectory(string path) { }
    public string TempPath => string.Empty;
    public string CombinePath(params string[] paths) => string.Join("/", paths);
    public string GetApplicationDataDirectory() => string.Empty;
    public string GetDocumentsDirectory() => string.Empty;
    public string GetTemporaryDirectory() => string.Empty;
    public void EnsureDirectoryExists(string path) { }
    public long GetFileSize(string path) => 0;
}

public sealed class StubAudioService : IAudioService
{
    public bool IsInitialized => false;
    public float Volume { get; set; }
    public bool IsMuted { get; set; }
    public AudioCapabilities Capabilities { get; } = new AudioCapabilities();
    public bool IsPlaying => false;
    public int SampleRate { get; set; } = 44100;
    public int Channels { get; set; } = 2;

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public bool Initialize(int sampleRate, int channels, int bufferSize) => true;
    public void Start() { }
    public void PlaySample(float[] samples) { }
    public void QueueSamples(ReadOnlySpan<float> samples) { }
    public void StopPlayback() { }
    public double GetLatency() => 0.0;
    public void Dispose() { }
}

public sealed class StubVideoService : IVideoService
{
    public bool IsInitialized => false;
    public Resolution CurrentResolution { get; set; }
    public bool IsFullscreen { get; set; }
    public VideoCapabilities Capabilities { get; } = new VideoCapabilities();
    public Resolution Resolution { get; set; } = new Resolution(256, 240);

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public bool Initialize(int width, int height, bool fullscreen) => true;
    public void RenderFrame(byte[] frameData) { }
    public void PresentFrame(ReadOnlySpan<uint> frameData, int width, int height) { }
    public void SetResolution(Resolution resolution) { }
    public void ToggleFullscreen() { }
    public void SetVSync(bool enabled) { }
    public void Dispose() { }
}

public sealed class StubInputService : IInputService
{
    public bool IsInitialized => false;
    public InputState CurrentState => new InputState();
    public InputCapabilities Capabilities { get; } = new InputCapabilities();
    #pragma warning disable CS0067 // Event is never used
    public event EventHandler<InputEventArgs>? InputChanged;
    #pragma warning restore CS0067

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Update() { }
    public void Poll() { }
    public bool IsKeyPressed(Key key) => false;
    public bool IsGamepadButtonPressed(int player, GamepadButton button) => false;
    public void Dispose() { }
}

public sealed class StubTimingService : ITimingService
{
    public bool IsInitialized => false;
    public double TargetFrameRate { get; set; }
    public double ActualFrameRate => 60.0;
    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void WaitForNextFrame() { }
    public long GetTimestamp() => DateTimeOffset.UtcNow.Ticks;
    public long GetFrequency() => TimeSpan.TicksPerSecond;
    public void PreciseSleep(TimeSpan duration) => Thread.Sleep(duration);
    public void Dispose() { }
}

public sealed class StubThreadingService : IThreadingService
{
    public bool IsInitialized => false;
    public int ThreadCount => Environment.ProcessorCount;
    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RunAsync(Action action, CancellationToken cancellationToken = default) => Task.Run(action, cancellationToken);
    public Thread CreateEmulationThread(Action action, string name) => new Thread(() => action()) { Name = name };
    public void SetThreadAffinity(Thread thread, int[] coreIds) { }
    public IDisposable CreateHighResolutionTimer(TimeSpan interval, Action callback) => new StubTimer();
    public void Dispose() { }
}

// Stub types for missing platform types
public class InputState { }

public sealed class StubTimer : IDisposable
{
    public void Dispose() { }
}
#pragma warning restore CA1822
