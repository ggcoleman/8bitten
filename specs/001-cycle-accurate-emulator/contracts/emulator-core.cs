// 8Bitten - Core Emulator Interface Contracts
// Generated from specification requirements

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EightBitten.Core.Contracts
{
    /// <summary>
    /// Main emulator interface providing core emulation functionality
    /// </summary>
    public interface IEmulator
    {
        /// <summary>
        /// Load ROM cartridge into emulator
        /// </summary>
        /// <param name="romData">ROM file data</param>
        /// <returns>True if ROM loaded successfully</returns>
        Task<bool> LoadROMAsync(byte[] romData);

        /// <summary>
        /// Start emulation execution
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// Stop emulation execution
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Reset emulator to initial state
        /// </summary>
        Task ResetAsync();

        /// <summary>
        /// Execute single frame of emulation
        /// </summary>
        /// <returns>Frame execution metrics</returns>
        Task<FrameMetrics> ExecuteFrameAsync();

        /// <summary>
        /// Current emulation state
        /// </summary>
        EmulatorState State { get; }

        /// <summary>
        /// Frame rate statistics
        /// </summary>
        PerformanceMetrics Performance { get; }

        /// <summary>
        /// Event fired when frame is completed
        /// </summary>
        event EventHandler<FrameCompletedEventArgs> FrameCompleted;
    }

    /// <summary>
    /// CPU emulation interface
    /// </summary>
    public interface ICPU
    {
        /// <summary>
        /// Execute single CPU instruction
        /// </summary>
        /// <returns>Cycles consumed</returns>
        int ExecuteInstruction();

        /// <summary>
        /// Trigger interrupt request
        /// </summary>
        void TriggerIRQ();

        /// <summary>
        /// Trigger non-maskable interrupt
        /// </summary>
        void TriggerNMI();

        /// <summary>
        /// Reset CPU to initial state
        /// </summary>
        void Reset();

        /// <summary>
        /// Current CPU state
        /// </summary>
        CPUState State { get; }

        /// <summary>
        /// Total cycles executed
        /// </summary>
        ulong CycleCount { get; }
    }

    /// <summary>
    /// PPU emulation interface
    /// </summary>
    public interface IPPU
    {
        /// <summary>
        /// Execute PPU for specified number of cycles
        /// </summary>
        /// <param name="cycles">Number of cycles to execute</param>
        void ExecuteCycles(int cycles);

        /// <summary>
        /// Read from PPU register
        /// </summary>
        /// <param name="address">Register address</param>
        /// <returns>Register value</returns>
        byte ReadRegister(ushort address);

        /// <summary>
        /// Write to PPU register
        /// </summary>
        /// <param name="address">Register address</param>
        /// <param name="value">Value to write</param>
        void WriteRegister(ushort address, byte value);

        /// <summary>
        /// Get current frame buffer
        /// </summary>
        /// <returns>Frame buffer data (256x240 pixels)</returns>
        ReadOnlySpan<uint> GetFrameBuffer();

        /// <summary>
        /// Current PPU state
        /// </summary>
        PPUState State { get; }

        /// <summary>
        /// Event fired when VBlank starts
        /// </summary>
        event EventHandler VBlankStarted;
    }

    /// <summary>
    /// APU emulation interface
    /// </summary>
    public interface IAPU
    {
        /// <summary>
        /// Execute APU for specified number of cycles
        /// </summary>
        /// <param name="cycles">Number of cycles to execute</param>
        void ExecuteCycles(int cycles);

        /// <summary>
        /// Read from APU register
        /// </summary>
        /// <param name="address">Register address</param>
        /// <returns>Register value</returns>
        byte ReadRegister(ushort address);

        /// <summary>
        /// Write to APU register
        /// </summary>
        /// <param name="address">Register address</param>
        /// <param name="value">Value to write</param>
        void WriteRegister(ushort address, byte value);

        /// <summary>
        /// Get audio samples for current frame
        /// </summary>
        /// <returns>Audio sample buffer</returns>
        ReadOnlySpan<float> GetAudioSamples();

        /// <summary>
        /// Current APU state
        /// </summary>
        APUState State { get; }
    }

    /// <summary>
    /// Memory management interface
    /// </summary>
    public interface IMemoryMap
    {
        /// <summary>
        /// Read byte from memory address
        /// </summary>
        /// <param name="address">Memory address</param>
        /// <returns>Byte value</returns>
        byte ReadByte(ushort address);

        /// <summary>
        /// Write byte to memory address
        /// </summary>
        /// <param name="address">Memory address</param>
        /// <param name="value">Byte value</param>
        void WriteByte(ushort address, byte value);

        /// <summary>
        /// Read word from memory address (little-endian)
        /// </summary>
        /// <param name="address">Memory address</param>
        /// <returns>Word value</returns>
        ushort ReadWord(ushort address);

        /// <summary>
        /// Write word to memory address (little-endian)
        /// </summary>
        /// <param name="address">Memory address</param>
        /// <param name="value">Word value</param>
        void WriteWord(ushort address, ushort value);

        /// <summary>
        /// Install cartridge mapper
        /// </summary>
        /// <param name="mapper">Cartridge mapper</param>
        void InstallMapper(IMapper mapper);
    }

    /// <summary>
    /// Cartridge mapper interface
    /// </summary>
    public interface IMapper
    {
        /// <summary>
        /// Mapper number (iNES format)
        /// </summary>
        byte MapperNumber { get; }

        /// <summary>
        /// Map CPU address to ROM/RAM
        /// </summary>
        /// <param name="address">CPU address</param>
        /// <returns>Mapped address and memory type</returns>
        (uint mappedAddress, MemoryType type) MapCPUAddress(ushort address);

        /// <summary>
        /// Map PPU address to CHR ROM/RAM
        /// </summary>
        /// <param name="address">PPU address</param>
        /// <returns>Mapped address and memory type</returns>
        (uint mappedAddress, MemoryType type) MapPPUAddress(ushort address);

        /// <summary>
        /// Handle mapper register write
        /// </summary>
        /// <param name="address">Register address</param>
        /// <param name="value">Value written</param>
        void WriteRegister(ushort address, byte value);

        /// <summary>
        /// Reset mapper to initial state
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Save state management interface
    /// </summary>
    public interface ISaveStateManager
    {
        /// <summary>
        /// Save current emulator state
        /// </summary>
        /// <param name="name">Save state name</param>
        /// <returns>Save state identifier</returns>
        Task<Guid> SaveStateAsync(string name);

        /// <summary>
        /// Load emulator state
        /// </summary>
        /// <param name="saveId">Save state identifier</param>
        /// <returns>True if loaded successfully</returns>
        Task<bool> LoadStateAsync(Guid saveId);

        /// <summary>
        /// Delete save state
        /// </summary>
        /// <param name="saveId">Save state identifier</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteStateAsync(Guid saveId);

        /// <summary>
        /// List available save states
        /// </summary>
        /// <returns>Collection of save state metadata</returns>
        Task<IEnumerable<SaveStateMetadata>> ListStatesAsync();
    }

    /// <summary>
    /// Configuration management interface
    /// </summary>
    public interface IConfigurationManager
    {
        /// <summary>
        /// Load configuration from file
        /// </summary>
        /// <returns>Configuration object</returns>
        Task<EmulatorConfiguration> LoadConfigurationAsync();

        /// <summary>
        /// Save configuration to file
        /// </summary>
        /// <param name="config">Configuration to save</param>
        Task SaveConfigurationAsync(EmulatorConfiguration config);

        /// <summary>
        /// Get default configuration
        /// </summary>
        /// <returns>Default configuration</returns>
        EmulatorConfiguration GetDefaultConfiguration();

        /// <summary>
        /// Validate configuration
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <returns>Validation result</returns>
        ConfigurationValidationResult ValidateConfiguration(EmulatorConfiguration config);
    }

    /// <summary>
    /// CLI output and visualization interface using Spectre.Console
    /// </summary>
    public interface ICLIRenderer
    {
        /// <summary>
        /// Display emulator status with rich formatting
        /// </summary>
        /// <param name="status">Current emulator status</param>
        void DisplayStatus(EmulatorStatus status);

        /// <summary>
        /// Show progress bar for operations
        /// </summary>
        /// <param name="operation">Operation name</param>
        /// <param name="progress">Progress percentage (0-100)</param>
        void ShowProgress(string operation, double progress);

        /// <summary>
        /// Display register states in formatted table
        /// </summary>
        /// <param name="cpuState">CPU register state</param>
        /// <param name="ppuState">PPU register state</param>
        void DisplayRegisters(CPUState cpuState, PPUState ppuState);

        /// <summary>
        /// Show performance metrics with color coding
        /// </summary>
        /// <param name="metrics">Performance metrics</param>
        void DisplayPerformanceMetrics(PerformanceMetrics metrics);

        /// <summary>
        /// Display diagnostic information with syntax highlighting
        /// </summary>
        /// <param name="diagnostics">Diagnostic data</param>
        void DisplayDiagnostics(DiagnosticInfo diagnostics);

        /// <summary>
        /// Show interactive menu for user selection
        /// </summary>
        /// <param name="title">Menu title</param>
        /// <param name="options">Available options</param>
        /// <returns>Selected option index</returns>
        Task<int> ShowInteractiveMenu(string title, string[] options);
    }

    /// <summary>
    /// Research and analysis interface for academic and competitive use
    /// </summary>
    public interface IResearchAnalyzer
    {
        /// <summary>
        /// Start metrics collection session
        /// </summary>
        /// <param name="sessionConfig">Collection configuration</param>
        /// <returns>Session identifier</returns>
        Task<Guid> StartMetricsSession(MetricsConfiguration sessionConfig);

        /// <summary>
        /// Stop metrics collection and finalize data
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        Task StopMetricsSession(Guid sessionId);

        /// <summary>
        /// Export session data in specified format
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <param name="format">Export format (CSV, JSON, HDF5)</param>
        /// <param name="filePath">Output file path</param>
        Task ExportSessionData(Guid sessionId, ExportFormat format, string filePath);

        /// <summary>
        /// Analyze performance data and identify optimization opportunities
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>Analysis results with recommendations</returns>
        Task<AnalysisResult> AnalyzePerformance(Guid sessionId);

        /// <summary>
        /// Compare multiple sessions for differences and improvements
        /// </summary>
        /// <param name="sessionIds">Sessions to compare</param>
        /// <returns>Comparison results with statistical analysis</returns>
        Task<ComparisonResult> CompareSessions(Guid[] sessionIds);

        /// <summary>
        /// Generate statistical summary of session data
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>Statistical summary with confidence intervals</returns>
        Task<StatisticalSummary> GenerateStatistics(Guid sessionId);
    }

    /// <summary>
    /// Input recording and replay interface for deterministic reproduction
    /// </summary>
    public interface IInputRecorder
    {
        /// <summary>
        /// Start recording input sequence
        /// </summary>
        /// <param name="recordingConfig">Recording configuration</param>
        /// <returns>Recording identifier</returns>
        Task<Guid> StartRecording(RecordingConfiguration recordingConfig);

        /// <summary>
        /// Stop recording and save input sequence
        /// </summary>
        /// <param name="recordingId">Recording identifier</param>
        /// <param name="filePath">Output file path</param>
        Task StopRecording(Guid recordingId, string filePath);

        /// <summary>
        /// Load and replay input sequence
        /// </summary>
        /// <param name="filePath">Input recording file</param>
        /// <returns>Replay session identifier</returns>
        Task<Guid> StartReplay(string filePath);

        /// <summary>
        /// Verify replay determinism against original recording
        /// </summary>
        /// <param name="originalRecording">Original recording file</param>
        /// <param name="replaySession">Replay session identifier</param>
        /// <returns>Verification result with difference analysis</returns>
        Task<VerificationResult> VerifyDeterminism(string originalRecording, Guid replaySession);

        /// <summary>
        /// Analyze input efficiency and timing optimization
        /// </summary>
        /// <param name="recordingId">Recording identifier</param>
        /// <returns>Input analysis with optimization suggestions</returns>
        Task<InputAnalysis> AnalyzeInputEfficiency(Guid recordingId);
    }

    /// <summary>
    /// Real-time overlay and metrics display interface
    /// </summary>
    public interface IOverlayManager
    {
        /// <summary>
        /// Add performance metrics overlay
        /// </summary>
        /// <param name="config">Overlay configuration</param>
        /// <returns>Overlay identifier</returns>
        Guid AddMetricsOverlay(OverlayConfiguration config);

        /// <summary>
        /// Add register state display overlay
        /// </summary>
        /// <param name="config">Overlay configuration</param>
        /// <returns>Overlay identifier</returns>
        Guid AddRegisterOverlay(OverlayConfiguration config);

        /// <summary>
        /// Add memory map visualization overlay
        /// </summary>
        /// <param name="config">Overlay configuration</param>
        /// <returns>Overlay identifier</returns>
        Guid AddMemoryOverlay(OverlayConfiguration config);

        /// <summary>
        /// Update overlay content and positioning
        /// </summary>
        /// <param name="overlayId">Overlay identifier</param>
        /// <param name="config">Updated configuration</param>
        void UpdateOverlay(Guid overlayId, OverlayConfiguration config);

        /// <summary>
        /// Remove overlay from display
        /// </summary>
        /// <param name="overlayId">Overlay identifier</param>
        void RemoveOverlay(Guid overlayId);

        /// <summary>
        /// Toggle overlay visibility
        /// </summary>
        /// <param name="overlayId">Overlay identifier</param>
        /// <param name="visible">Visibility state</param>
        void SetOverlayVisibility(Guid overlayId, bool visible);
    }

    // Supporting enums and data structures
    public enum EmulatorState
    {
        Stopped,
        Running,
        Paused,
        Error
    }

    public enum MemoryType
    {
        ROM,
        RAM,
        Register,
        Unmapped
    }

    public record FrameMetrics(
        ulong FrameNumber,
        double ExecutionTime,
        int CPUCycles,
        int PPUCycles,
        int APUCycles
    );

    public record PerformanceMetrics(
        double AverageFPS,
        double FrameTime,
        double CPUUsage,
        long MemoryUsage
    );

    public record SaveStateMetadata(
        Guid Id,
        string Name,
        DateTime CreatedAt,
        long Size,
        string? Description
    );

    public record EmulatorStatus(
        EmulatorState State,
        string? CurrentROM,
        ulong FrameCount,
        double FPS,
        string? StatusMessage
    );

    public record DiagnosticInfo(
        string Component,
        string Message,
        DiagnosticLevel Level,
        DateTime Timestamp,
        Dictionary<string, object>? AdditionalData = null
    );

    public enum DiagnosticLevel
    {
        Info,
        Warning,
        Error,
        Debug
    }

    public enum ExportFormat
    {
        CSV,
        JSON,
        HDF5,
        Binary
    }

    public record MetricsConfiguration(
        bool CollectTiming,
        bool CollectPerformance,
        bool CollectMemoryAccess,
        bool CollectInputLatency,
        int SamplingRate,
        string[] CustomMetrics
    );

    public record RecordingConfiguration(
        bool RecordInput,
        bool RecordState,
        bool RecordTiming,
        bool RecordMemoryAccess,
        int CompressionLevel
    );

    public record OverlayConfiguration(
        float X,
        float Y,
        float Width,
        float Height,
        float Transparency,
        string[] DisplayFields,
        string FontFamily,
        int FontSize,
        uint BackgroundColor,
        uint TextColor
    );

    public record AnalysisResult(
        TimeSpan SessionDuration,
        double AverageFrameTime,
        double FrameTimeVariance,
        int FrameDropCount,
        double InputLatencyAverage,
        double InputLatencyMax,
        string[] OptimizationRecommendations,
        Dictionary<string, double> PerformanceMetrics
    );

    public record ComparisonResult(
        Guid[] SessionIds,
        Dictionary<string, double> MetricDifferences,
        double StatisticalSignificance,
        string[] SignificantChanges,
        TimeSpan[] SegmentTimings,
        string ComparisonSummary
    );

    public record StatisticalSummary(
        Dictionary<string, double> Means,
        Dictionary<string, double> StandardDeviations,
        Dictionary<string, double> ConfidenceIntervals,
        Dictionary<string, double> Percentiles,
        int SampleCount,
        TimeSpan TotalDuration
    );

    public record VerificationResult(
        bool IsDeterministic,
        int FramesDifferent,
        double MaxTimingDifference,
        string[] DifferenceDetails,
        double SimilarityScore
    );

    public record InputAnalysis(
        double InputEfficiency,
        int SuboptimalInputs,
        TimeSpan[] OptimalTimingWindows,
        string[] EfficiencyRecommendations,
        Dictionary<string, int> InputPatternFrequency
    );

    public class FrameCompletedEventArgs : EventArgs
    {
        public FrameMetrics Metrics { get; }
        public ReadOnlySpan<uint> FrameBuffer { get; }
        public ReadOnlySpan<float> AudioSamples { get; }

        public FrameCompletedEventArgs(FrameMetrics metrics, ReadOnlySpan<uint> frameBuffer, ReadOnlySpan<float> audioSamples)
        {
            Metrics = metrics;
            FrameBuffer = frameBuffer;
            AudioSamples = audioSamples;
        }
    }
}
