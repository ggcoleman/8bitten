using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace EightBitten.Infrastructure.Platform.Audio;

/// <summary>
/// NAudio-based audio renderer for NES emulator
/// Handles audio output, buffering, and playback control
/// </summary>
public sealed class NAudioRenderer : IDisposable
{
    private readonly ILogger<NAudioRenderer> _logger;
    private WaveOutEvent? _waveOut;
    private BufferedWaveProvider? _waveProvider;
    private readonly ConcurrentQueue<float[]> _audioQueue;
    private bool _isInitialized;
    private bool _isPlaying;
    private bool _isPaused;
    private bool _disposed;
    private float _volume = 1.0f;

    /// <summary>
    /// Gets whether the audio renderer is initialized
    /// </summary>
    public bool IsInitialized => _isInitialized && !_disposed;

    /// <summary>
    /// Gets whether audio is currently playing
    /// </summary>
    public bool IsPlaying => _isPlaying && !_isPaused && !_disposed;

    /// <summary>
    /// Gets whether audio playback is paused
    /// </summary>
    public bool IsPaused => _isPaused && !_disposed;

    /// <summary>
    /// Gets the sample rate in Hz
    /// </summary>
    public int SampleRate { get; private set; }

    /// <summary>
    /// Gets the number of audio channels
    /// </summary>
    public int Channels { get; private set; }

    /// <summary>
    /// Gets the buffer size in samples
    /// </summary>
    public int BufferSize { get; private set; }

    /// <summary>
    /// Gets the current volume level (0.0 to 1.0)
    /// </summary>
    public float Volume => _volume;

    /// <summary>
    /// Initializes a new instance of the NAudioRenderer class
    /// </summary>
    /// <param name="logger">Logger for diagnostic output</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null</exception>
    public NAudioRenderer(ILogger<NAudioRenderer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _audioQueue = new ConcurrentQueue<float[]>();
        _logger.LogDebug("NAudioRenderer created");
    }

    /// <summary>
    /// Initializes the audio renderer with specified parameters
    /// </summary>
    /// <param name="sampleRate">Sample rate in Hz (e.g., 44100)</param>
    /// <param name="channels">Number of channels (1 for mono, 2 for stereo)</param>
    /// <param name="bufferSize">Buffer size in samples</param>
    /// <returns>True if initialization succeeded, false otherwise</returns>
    public bool Initialize(int sampleRate, int channels, int bufferSize)
    {
        if (_disposed)
        {
            _logger.LogError("Cannot initialize disposed audio renderer");
            return false;
        }

        if (sampleRate <= 0 || channels <= 0 || bufferSize <= 0)
        {
            _logger.LogError("Invalid audio parameters: SampleRate={SampleRate}, Channels={Channels}, BufferSize={BufferSize}",
                sampleRate, channels, bufferSize);
            return false;
        }

        try
        {
            SampleRate = sampleRate;
            Channels = channels;
            BufferSize = bufferSize;

            // Create wave format
            var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);

            // Create wave provider with buffering
            _waveProvider = new BufferedWaveProvider(waveFormat)
            {
                BufferLength = bufferSize * channels * sizeof(float) * 4, // 4x buffer for safety
                DiscardOnBufferOverflow = true
            };

            // Create wave output device
            _waveOut = new WaveOutEvent
            {
                DesiredLatency = 100, // 100ms latency
                NumberOfBuffers = 3
            };

            _waveOut.Init(_waveProvider);

            _logger.LogInformation("Audio renderer initialized: {SampleRate}Hz, {Channels} channels, {BufferSize} samples",
                sampleRate, channels, bufferSize);
            
            _isInitialized = true;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize audio renderer");
            Cleanup();
            return false;
        }
    }

    /// <summary>
    /// Plays audio samples
    /// </summary>
    /// <param name="samples">Audio sample data</param>
    /// <returns>True if samples were queued successfully, false otherwise</returns>
    public bool PlayAudio(float[] samples)
    {
        ArgumentNullException.ThrowIfNull(samples);

        if (!_isInitialized || _disposed || _waveProvider == null)
        {
            _logger.LogWarning("Audio renderer not initialized or disposed");
            return false;
        }

        try
        {
            // Apply volume scaling
            var scaledSamples = new float[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                scaledSamples[i] = samples[i] * _volume;
            }

            // Convert to byte array
            var byteArray = new byte[scaledSamples.Length * sizeof(float)];
            Buffer.BlockCopy(scaledSamples, 0, byteArray, 0, byteArray.Length);

            // Add to wave provider buffer
            _waveProvider.AddSamples(byteArray, 0, byteArray.Length);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play audio samples");
            return false;
        }
    }

    /// <summary>
    /// Starts audio playback
    /// </summary>
    /// <returns>True if playback started successfully, false otherwise</returns>
    public bool StartPlayback()
    {
        if (!_isInitialized || _disposed || _waveOut == null)
        {
            _logger.LogWarning("Audio renderer not initialized or disposed");
            return false;
        }

        try
        {
            _waveOut.Play();
            _isPlaying = true;
            _isPaused = false;
            _logger.LogDebug("Audio playback started");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start audio playback");
            return false;
        }
    }

    /// <summary>
    /// Stops audio playback
    /// </summary>
    public void StopPlayback()
    {
        if (!_isInitialized || _disposed || _waveOut == null)
        {
            return;
        }

        try
        {
            _waveOut.Stop();
            _isPlaying = false;
            _isPaused = false;
            _logger.LogDebug("Audio playback stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop audio playback");
        }
    }

    /// <summary>
    /// Pauses audio playback
    /// </summary>
    public void PausePlayback()
    {
        if (!_isInitialized || _disposed || _waveOut == null || !_isPlaying)
        {
            return;
        }

        try
        {
            _waveOut.Pause();
            _isPaused = true;
            _logger.LogDebug("Audio playback paused");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause audio playback");
        }
    }

    /// <summary>
    /// Resumes audio playback
    /// </summary>
    public void ResumePlayback()
    {
        if (!_isInitialized || _disposed || _waveOut == null || !_isPaused)
        {
            return;
        }

        try
        {
            _waveOut.Play();
            _isPaused = false;
            _logger.LogDebug("Audio playback resumed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume audio playback");
        }
    }

    /// <summary>
    /// Sets the audio volume
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0)</param>
    /// <exception cref="ArgumentException">Thrown when volume is out of range</exception>
    public void SetVolume(float volume)
    {
        if (volume < 0.0f || volume > 1.0f || float.IsNaN(volume) || float.IsInfinity(volume))
        {
            throw new ArgumentException("Volume must be between 0.0 and 1.0", nameof(volume));
        }

        _volume = volume;
        
        if (_waveOut != null)
        {
            _waveOut.Volume = volume;
        }

        _logger.LogDebug("Audio volume set to {Volume}", volume);
    }

    /// <summary>
    /// Gets the current audio latency
    /// </summary>
    /// <returns>Audio latency as TimeSpan</returns>
    public TimeSpan GetLatency()
    {
        if (!_isInitialized || _waveProvider == null)
        {
            return TimeSpan.Zero;
        }

        try
        {
            var bufferedBytes = _waveProvider.BufferedBytes;
            var bytesPerSecond = SampleRate * Channels * sizeof(float);
            var latencySeconds = (double)bufferedBytes / bytesPerSecond;
            return TimeSpan.FromSeconds(latencySeconds);
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Gets the current buffer usage as a percentage
    /// </summary>
    /// <returns>Buffer usage (0.0 to 1.0)</returns>
    public float GetBufferUsage()
    {
        if (!_isInitialized || _waveProvider == null)
        {
            return 0.0f;
        }

        try
        {
            var bufferedBytes = _waveProvider.BufferedBytes;
            var totalBufferSize = _waveProvider.BufferLength;
            return (float)bufferedBytes / totalBufferSize;
        }
        catch
        {
            return 0.0f;
        }
    }

    /// <summary>
    /// Cleans up audio resources
    /// </summary>
    private void Cleanup()
    {
        try
        {
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _waveOut = null;

            _waveProvider = null;

            // Clear audio queue
            while (_audioQueue.TryDequeue(out _)) { }

            _isInitialized = false;
            _isPlaying = false;
            _isPaused = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during audio cleanup");
        }
    }

    /// <summary>
    /// Disposes of audio resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Cleanup();
        _disposed = true;
        _logger.LogDebug("NAudioRenderer disposed");
    }
}
