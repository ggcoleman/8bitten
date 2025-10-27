using System;
using Microsoft.Extensions.Logging;

namespace EightBitten.Core.APU;

/// <summary>
/// APU audio generator for producing NES audio output
/// Handles sound channel mixing and audio sample generation
/// </summary>
public sealed class AudioGenerator : IDisposable
{
    private readonly ILogger<AudioGenerator> _logger;
    private readonly AudioProcessingUnit _apu;
    private readonly float[] _mixBuffer;
    private readonly int _sampleRate;
    private readonly int _bufferSize;
    private bool _disposed;

    /// <summary>
    /// Gets whether the audio generator is initialized
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Gets the sample rate in Hz
    /// </summary>
    public int SampleRate => _sampleRate;

    /// <summary>
    /// Gets the buffer size in samples
    /// </summary>
    public int BufferSize => _bufferSize;

    /// <summary>
    /// Gets the current sample count
    /// </summary>
    public long SampleCount { get; private set; }

    /// <summary>
    /// Initializes a new instance of the AudioGenerator class
    /// </summary>
    /// <param name="apu">APU instance to generate audio from</param>
    /// <param name="sampleRate">Audio sample rate in Hz</param>
    /// <param name="bufferSize">Audio buffer size in samples</param>
    /// <param name="logger">Logger for diagnostic output</param>
    /// <exception cref="ArgumentNullException">Thrown when apu or logger is null</exception>
    /// <exception cref="ArgumentException">Thrown when sample rate or buffer size is invalid</exception>
    public AudioGenerator(AudioProcessingUnit apu, int sampleRate, int bufferSize, ILogger<AudioGenerator> logger)
    {
        _apu = apu ?? throw new ArgumentNullException(nameof(apu));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        if (sampleRate <= 0)
            throw new ArgumentException("Sample rate must be positive", nameof(sampleRate));
        if (bufferSize <= 0)
            throw new ArgumentException("Buffer size must be positive", nameof(bufferSize));

        _sampleRate = sampleRate;
        _bufferSize = bufferSize;
        _mixBuffer = new float[bufferSize];
        
        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogDebug("APU AudioGenerator created: {SampleRate}Hz, {BufferSize} samples", sampleRate, bufferSize);
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Initializes the audio generator
    /// </summary>
    /// <returns>True if initialization succeeded, false otherwise</returns>
    public bool Initialize()
    {
        if (_disposed)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogError("Cannot initialize disposed audio generator");
            #pragma warning restore CA1848
            return false;
        }

        try
        {
            // Clear mix buffer
            Array.Clear(_mixBuffer, 0, _mixBuffer.Length);
            SampleCount = 0;

            IsInitialized = true;
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogInformation("APU AudioGenerator initialized");
            #pragma warning restore CA1848
            return true;
        }
        #pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
        #pragma warning restore CA1031
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogError(ex, "Failed to initialize APU audio generator");
            #pragma warning restore CA1848
            return false;
        }
    }

    /// <summary>
    /// Generates audio samples from APU state
    /// </summary>
    /// <param name="sampleCount">Number of samples to generate</param>
    /// <returns>Array of audio samples</returns>
    public float[] GenerateSamples(int sampleCount)
    {
        if (!IsInitialized || _disposed)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogWarning("Audio generator not initialized or disposed");
            #pragma warning restore CA1848
            return new float[sampleCount];
        }

        if (sampleCount <= 0)
        {
            return Array.Empty<float>();
        }

        try
        {
            var samples = new float[sampleCount];
            var apuState = (APUState)_apu.GetState();
            
            for (int i = 0; i < sampleCount; i++)
            {
                samples[i] = GenerateSample(apuState);
                SampleCount++;
            }

            return samples;
        }
        #pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
        #pragma warning restore CA1031
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogError(ex, "Error generating audio samples");
            #pragma warning restore CA1848
            return new float[sampleCount];
        }
    }

    /// <summary>
    /// Generates a single audio sample
    /// </summary>
    /// <param name="apuState">Current APU state</param>
    /// <returns>Audio sample value</returns>
    private float GenerateSample(APUState apuState)
    {
        float sample = 0.0f;

        // Mix pulse channels
        sample += GeneratePulse1Sample(apuState);
        sample += GeneratePulse2Sample(apuState);
        
        // Mix triangle channel
        sample += GenerateTriangleSample(apuState);
        
        // Mix noise channel
        sample += GenerateNoiseSample(apuState);
        
        // Mix DMC channel
        sample += GenerateDMCSample(apuState);

        // Apply master volume and clipping
        sample *= 0.2f; // Reduce overall volume
        return Math.Clamp(sample, -1.0f, 1.0f);
    }

    /// <summary>
    /// Generates pulse channel 1 sample
    /// </summary>
    /// <param name="apuState">Current APU state</param>
    /// <returns>Pulse 1 sample value</returns>
    private float GeneratePulse1Sample(APUState apuState)
    {
        if ((apuState.Status & APUStatus.Pulse1Enable) == 0 || apuState.Pulse1.LengthCounter == 0)
        {
            return 0.0f;
        }

        // Simple square wave generation
        var frequency = GetPulseFrequency(apuState.Pulse1.Timer);
        var phase = (SampleCount * frequency / _sampleRate) % 1.0;
        var dutyCycle = GetDutyCycle(apuState.Pulse1.DutyCycle);

        var amplitude = apuState.Pulse1.Volume / 15.0f;
        return (float)(phase < dutyCycle ? amplitude : -amplitude);
    }

    /// <summary>
    /// Generates pulse channel 2 sample
    /// </summary>
    /// <param name="apuState">Current APU state</param>
    /// <returns>Pulse 2 sample value</returns>
    private float GeneratePulse2Sample(APUState apuState)
    {
        if ((apuState.Status & APUStatus.Pulse2Enable) == 0 || apuState.Pulse2.LengthCounter == 0)
        {
            return 0.0f;
        }

        // Simple square wave generation
        var frequency = GetPulseFrequency(apuState.Pulse2.Timer);
        var phase = (SampleCount * frequency / _sampleRate) % 1.0;
        var dutyCycle = GetDutyCycle(apuState.Pulse2.DutyCycle);

        var amplitude = apuState.Pulse2.Volume / 15.0f;
        return (float)(phase < dutyCycle ? amplitude : -amplitude);
    }

    /// <summary>
    /// Generates triangle channel sample
    /// </summary>
    /// <param name="apuState">Current APU state</param>
    /// <returns>Triangle sample value</returns>
    private float GenerateTriangleSample(APUState apuState)
    {
        if ((apuState.Status & APUStatus.TriangleEnable) == 0 || apuState.Triangle.LengthCounter == 0)
        {
            return 0.0f;
        }

        // Simple triangle wave generation
        var frequency = GetTriangleFrequency(apuState.Triangle.Timer);
        var phase = (SampleCount * frequency / _sampleRate) % 1.0;

        // Triangle wave: -1 to 1 over one period
        var amplitude = 0.5f; // Triangle channel has fixed amplitude
        if (phase < 0.5)
        {
            return amplitude * (float)(4.0 * phase - 1.0);
        }
        else
        {
            return amplitude * (float)(3.0 - 4.0 * phase);
        }
    }

    /// <summary>
    /// Generates noise channel sample
    /// </summary>
    /// <param name="apuState">Current APU state</param>
    /// <returns>Noise sample value</returns>
    private float GenerateNoiseSample(APUState apuState)
    {
        if ((apuState.Status & APUStatus.NoiseEnable) == 0 || apuState.Noise.LengthCounter == 0)
        {
            return 0.0f;
        }

        // Simple noise generation using linear feedback shift register
        var amplitude = apuState.Noise.Volume / 15.0f;
        var noiseValue = (SampleCount * 31) % 2; // Simplified noise pattern
        return noiseValue == 0 ? amplitude : -amplitude;
    }

    /// <summary>
    /// Generates DMC channel sample
    /// </summary>
    /// <param name="apuState">Current APU state</param>
    /// <returns>DMC sample value</returns>
    private static float GenerateDMCSample(APUState apuState)
    {
        if ((apuState.Status & APUStatus.DMCEnable) == 0)
        {
            return 0.0f;
        }

        // Simple DMC generation (delta modulation)
        var amplitude = apuState.DMC.DirectLoad / 127.0f;
        return amplitude;
    }

    /// <summary>
    /// Calculates pulse channel frequency from timer value
    /// </summary>
    /// <param name="timer">Timer value</param>
    /// <returns>Frequency in Hz</returns>
    private static double GetPulseFrequency(ushort timer)
    {
        if (timer == 0) return 0.0;
        return 1789773.0 / (16.0 * (timer + 1));
    }

    /// <summary>
    /// Calculates triangle channel frequency from timer value
    /// </summary>
    /// <param name="timer">Timer value</param>
    /// <returns>Frequency in Hz</returns>
    private static double GetTriangleFrequency(ushort timer)
    {
        if (timer == 0) return 0.0;
        return 1789773.0 / (32.0 * (timer + 1));
    }

    /// <summary>
    /// Gets duty cycle percentage from duty cycle value
    /// </summary>
    /// <param name="dutyCycle">Duty cycle value (0-3)</param>
    /// <returns>Duty cycle percentage (0.0-1.0)</returns>
    private static double GetDutyCycle(byte dutyCycle)
    {
        return dutyCycle switch
        {
            0 => 0.125, // 12.5%
            1 => 0.25,  // 25%
            2 => 0.5,   // 50%
            3 => 0.75,  // 75%
            _ => 0.5
        };
    }

    /// <summary>
    /// Disposes of audio generator resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            IsInitialized = false;
            _disposed = true;
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogDebug("APU AudioGenerator disposed");
            #pragma warning restore CA1848
        }
        #pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
        #pragma warning restore CA1031
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogError(ex, "Error during audio generator disposal");
            #pragma warning restore CA1848
        }
    }
}
