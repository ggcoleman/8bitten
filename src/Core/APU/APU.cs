using System;
using System.Collections.Generic;
using EightBitten.Core.Contracts;
using Microsoft.Extensions.Logging;

namespace EightBitten.Core.APU;

/// <summary>
/// NES APU (Audio Processing Unit) emulation core
/// Silent mode - accurate timing and register behavior without audio output
/// </summary>
public sealed class AudioProcessingUnit : IClockedComponent, IMemoryMappedComponent
{
    private readonly ILogger<AudioProcessingUnit> _logger;
    private APUState _state;
    private bool _isInitialized;
    private bool _isSilent;
    private int _frameCounter;
    private int _frameSequenceStep;

    // APU timing constants
    private const int FRAME_COUNTER_RATE = 240; // Hz
    private const int CPU_FREQUENCY = 1789773; // Hz
    private const int CYCLES_PER_FRAME_COUNTER = CPU_FREQUENCY / FRAME_COUNTER_RATE;

    /// <summary>
    /// Component name
    /// </summary>
    public string Name => "APU";

    /// <summary>
    /// Whether APU is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// APU clock frequency (same as CPU frequency)
    /// </summary>
    public double ClockFrequency => 1789773.0; // NTSC frequency

    /// <summary>
    /// Current APU state
    /// </summary>
    public APUState CurrentState => _state.Clone();

    /// <summary>
    /// Whether APU is in silent mode (no audio output)
    /// </summary>
    public bool IsSilent => _isSilent;

    /// <summary>
    /// Memory address ranges this APU responds to (APU registers)
    /// </summary>
    public IReadOnlyList<MemoryRange> AddressRanges => new[]
    {
        new MemoryRange(0x4000, 0x4017) // APU registers
    };

    /// <summary>
    /// Event fired when frame counter generates an interrupt
    /// </summary>
    public event EventHandler<APUInterruptEventArgs>? FrameInterrupt;

    public AudioProcessingUnit(ILogger<AudioProcessingUnit> logger, bool silent = true)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _state = new APUState();
        _isSilent = silent;
    }

    /// <summary>
    /// Initialize APU component
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
            return;

        _state.Reset();
        _frameCounter = 0;
        _frameSequenceStep = 0;
        _isInitialized = true;

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogInformation("APU initialized in {Mode} mode", _isSilent ? "silent" : "audio");
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Reset APU to power-on state
    /// </summary>
    public void Reset()
    {
        _state.Reset();
        _frameCounter = 0;
        _frameSequenceStep = 0;

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogDebug("APU reset");
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Execute multiple APU cycles
    /// </summary>
    /// <param name="cycles">Number of cycles to execute</param>
    /// <returns>Number of cycles actually executed</returns>
    public int ExecuteCycles(int cycles)
    {
        int executed = 0;
        for (int i = 0; i < cycles && IsEnabled && _isInitialized; i++)
        {
            executed += ExecuteCycle();
        }
        return executed;
    }

    /// <summary>
    /// Execute one APU cycle
    /// </summary>
    /// <returns>Number of cycles consumed (always 1)</returns>
    public int ExecuteCycle()
    {
        if (!IsEnabled || !_isInitialized)
            return 1;

        try
        {
            // Update frame counter
            _frameCounter++;
            if (_frameCounter >= CYCLES_PER_FRAME_COUNTER)
            {
                _frameCounter = 0;
                ExecuteFrameSequencer();
            }

            // Update sound channels (in silent mode, just update timing)
            UpdatePulseChannels();
            UpdateTriangleChannel();
            UpdateNoiseChannel();
            UpdateDMCChannel();

            return 1;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogError(ex, "Error executing APU cycle");
            #pragma warning restore CA1848
            return 1;
        }
    }

    /// <summary>
    /// Synchronize with master clock
    /// </summary>
    /// <param name="masterCycles">Master clock cycles</param>
    public void SynchronizeClock(long masterCycles)
    {
        // APU runs at CPU frequency, so execute one cycle
        ExecuteCycle();
    }

    /// <summary>
    /// Read from APU register
    /// </summary>
    /// <param name="address">Register address</param>
    /// <returns>Register value</returns>
    public byte ReadMemory(ushort address)
    {
        return address switch
        {
            0x4015 => ReadStatus(),
            _ => 0x00 // Most APU registers are write-only
        };
    }

    /// <summary>
    /// Read byte from APU (IMemoryMappedComponent interface)
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <returns>Byte value</returns>
    public byte ReadByte(ushort address) => ReadMemory(address);

    /// <summary>
    /// Write byte to APU (IMemoryMappedComponent interface)
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <param name="value">Byte value</param>
    public void WriteByte(ushort address, byte value) => WriteMemory(address, value);

    /// <summary>
    /// Check if APU handles the specified address
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <returns>True if APU handles this address</returns>
    public bool HandlesAddress(ushort address)
    {
        return address >= 0x4000 && address <= 0x4017;
    }

    /// <summary>
    /// Write to APU register
    /// </summary>
    /// <param name="address">Register address</param>
    /// <param name="value">Value to write</param>
    public void WriteMemory(ushort address, byte value)
    {
        switch (address)
        {
            // Pulse 1 registers
            case 0x4000: WritePulse1Control(value); break;
            case 0x4001: WritePulse1Sweep(value); break;
            case 0x4002: WritePulse1TimerLow(value); break;
            case 0x4003: WritePulse1TimerHigh(value); break;

            // Pulse 2 registers
            case 0x4004: WritePulse2Control(value); break;
            case 0x4005: WritePulse2Sweep(value); break;
            case 0x4006: WritePulse2TimerLow(value); break;
            case 0x4007: WritePulse2TimerHigh(value); break;

            // Triangle registers
            case 0x4008: WriteTriangleControl(value); break;
            case 0x400A: WriteTriangleTimerLow(value); break;
            case 0x400B: WriteTriangleTimerHigh(value); break;

            // Noise registers
            case 0x400C: WriteNoiseControl(value); break;
            case 0x400E: WriteNoisePeriod(value); break;
            case 0x400F: WriteNoiseLength(value); break;

            // DMC registers
            case 0x4010: WriteDMCControl(value); break;
            case 0x4011: WriteDMCOutput(value); break;
            case 0x4012: WriteDMCSampleAddress(value); break;
            case 0x4013: WriteDMCSampleLength(value); break;

            // Status and frame counter
            case 0x4015: WriteStatus(value); break;
            case 0x4017: WriteFrameCounter(value); break;
        }
    }

    /// <summary>
    /// Get component state for save/load
    /// </summary>
    /// <returns>Component state</returns>
    public ComponentState GetState()
    {
        return _state.Clone();
    }

    /// <summary>
    /// Set component state from save data
    /// </summary>
    /// <param name="state">Component state</param>
    public void SetState(ComponentState state)
    {
        if (state is APUState apuState)
        {
            _state = apuState.Clone();
        }
    }

    /// <summary>
    /// Dispose APU resources
    /// </summary>
    public void Dispose()
    {
        // No unmanaged resources to dispose
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Execute frame sequencer step
    /// </summary>
    private void ExecuteFrameSequencer()
    {
        // 4-step or 5-step frame sequence
        var is5Step = _state.FrameCounter.Mode;
        var maxSteps = is5Step ? 5 : 4;

        _frameSequenceStep++;
        if (_frameSequenceStep >= maxSteps)
        {
            _frameSequenceStep = 0;
            
            // Generate frame interrupt if enabled and in 4-step mode
            if (!is5Step && !_state.FrameCounter.IRQInhibit)
            {
                OnFrameInterrupt();
            }
        }

        // Update envelopes and length counters based on step
        if (_frameSequenceStep == 1 || _frameSequenceStep == 3)
        {
            // Quarter frame: update envelopes and triangle linear counter
            UpdateEnvelopes();
            UpdateTriangleLinearCounter();
        }

        if (_frameSequenceStep == 1 || (_frameSequenceStep == 3 && !is5Step) || (_frameSequenceStep == 4 && is5Step))
        {
            // Half frame: update length counters and sweep units
            UpdateLengthCounters();
            UpdateSweepUnits();
        }
    }

    /// <summary>
    /// Update pulse channels
    /// </summary>
    private void UpdatePulseChannels()
    {
        // In silent mode, we just update the timing and state
        // Full implementation would generate audio samples
        
        // Update pulse 1 timer
        if (_state.Pulse1.Timer > 0)
        {
            _state.Pulse1.Timer--;
        }
        else
        {
            _state.Pulse1.TimerCounter = _state.Pulse1.Timer;
            _state.Pulse1.SequencePosition = (byte)((_state.Pulse1.SequencePosition + 1) & 7);
        }

        // Update pulse 2 timer
        if (_state.Pulse2.Timer > 0)
        {
            _state.Pulse2.Timer--;
        }
        else
        {
            _state.Pulse2.TimerCounter = _state.Pulse2.Timer;
            _state.Pulse2.SequencePosition = (byte)((_state.Pulse2.SequencePosition + 1) & 7);
        }
    }

    /// <summary>
    /// Update triangle channel
    /// </summary>
    private void UpdateTriangleChannel()
    {
        // Update triangle timer
        if (_state.Triangle.Timer > 0)
        {
            _state.Triangle.Timer--;
        }
        else
        {
            _state.Triangle.TimerCounter = _state.Triangle.Timer;
            if (_state.Triangle.LengthCounter > 0 && _state.Triangle.LinearCounter > 0)
            {
                _state.Triangle.SequencePosition = (byte)((_state.Triangle.SequencePosition + 1) & 31);
            }
        }
    }

    /// <summary>
    /// Update noise channel
    /// </summary>
    private void UpdateNoiseChannel()
    {
        // Update noise timer
        if (_state.Noise.TimerCounter > 0)
        {
            _state.Noise.TimerCounter--;
        }
        else
        {
            _state.Noise.TimerCounter = _state.Noise.Period;
            
            // Update shift register
            var feedback = (_state.Noise.ShiftRegister & 1) ^ 
                          ((_state.Noise.ShiftRegister >> (_state.Noise.Mode ? 6 : 1)) & 1);
            _state.Noise.ShiftRegister = (ushort)((_state.Noise.ShiftRegister >> 1) | (feedback << 14));
        }
    }

    /// <summary>
    /// Update DMC channel
    /// </summary>
    private void UpdateDMCChannel()
    {
        // Update DMC timer
        if (_state.DMC.TimerCounter > 0)
        {
            _state.DMC.TimerCounter--;
        }
        else
        {
            _state.DMC.TimerCounter = _state.DMC.Rate;
            
            // In silent mode, we don't actually fetch samples
            // Full implementation would read from memory and update output
        }
    }

    /// <summary>
    /// Update envelope generators
    /// </summary>
    private void UpdateEnvelopes()
    {
        // Update pulse channel envelopes
        // Update envelopes using individual properties
        UpdateChannelEnvelope(_state.Pulse1);
        UpdateChannelEnvelope(_state.Pulse2);
        UpdateChannelEnvelope(_state.Noise);
    }

    /// <summary>
    /// Update channel envelope for pulse channels
    /// </summary>
    private static void UpdateChannelEnvelope(PulseChannelState channel)
    {
        if (channel.EnvelopeStart)
        {
            channel.EnvelopeStart = false;
            channel.EnvelopeVolume = 15;
            channel.EnvelopePeriod = channel.Volume;
        }
        else if (channel.EnvelopePeriod > 0)
        {
            channel.EnvelopePeriod--;
        }
        else
        {
            channel.EnvelopePeriod = channel.Volume;
            if (channel.EnvelopeVolume > 0)
            {
                channel.EnvelopeVolume--;
            }
            else if (channel.LengthCounterHalt)
            {
                channel.EnvelopeVolume = 15;
            }
        }
    }

    /// <summary>
    /// Update channel envelope for noise channel
    /// </summary>
    private static void UpdateChannelEnvelope(NoiseChannelState channel)
    {
        if (channel.EnvelopeStart)
        {
            channel.EnvelopeStart = false;
            channel.EnvelopeVolume = 15;
            channel.EnvelopePeriod = channel.Volume;
        }
        else if (channel.EnvelopePeriod > 0)
        {
            channel.EnvelopePeriod--;
        }
        else
        {
            channel.EnvelopePeriod = channel.Volume;
            if (channel.EnvelopeVolume > 0)
            {
                channel.EnvelopeVolume--;
            }
            else if (channel.LengthCounterHalt)
            {
                channel.EnvelopeVolume = 15;
            }
        }
    }

    /// <summary>
    /// Update single envelope generator
    /// </summary>
    private static void UpdateEnvelope(ref EnvelopeState envelope)
    {
        if (envelope.Start)
        {
            envelope.Start = false;
            envelope.DecayLevel = 15;
            envelope.Divider = envelope.Period;
        }
        else if (envelope.Divider > 0)
        {
            envelope.Divider--;
        }
        else
        {
            envelope.Divider = envelope.Period;
            if (envelope.DecayLevel > 0)
            {
                envelope.DecayLevel--;
            }
            else if (envelope.Loop)
            {
                envelope.DecayLevel = 15;
            }
        }
    }

    /// <summary>
    /// Update triangle linear counter
    /// </summary>
    private void UpdateTriangleLinearCounter()
    {
        if (_state.Triangle.LinearCounterReload)
        {
            _state.Triangle.LinearCounter = _state.Triangle.LinearCounterLoad;
        }
        else if (_state.Triangle.LinearCounter > 0)
        {
            _state.Triangle.LinearCounter--;
        }

        if (!_state.Triangle.LengthCounterHalt)
        {
            _state.Triangle.LinearCounterReload = false;
        }
    }

    /// <summary>
    /// Update length counters
    /// </summary>
    private void UpdateLengthCounters()
    {
        // Update length counters for all channels
        if (!_state.Pulse1.LengthCounterHalt && _state.Pulse1.LengthCounter > 0)
            _state.Pulse1.LengthCounter--;

        if (!_state.Pulse2.LengthCounterHalt && _state.Pulse2.LengthCounter > 0)
            _state.Pulse2.LengthCounter--;

        if (!_state.Triangle.LengthCounterHalt && _state.Triangle.LengthCounter > 0)
            _state.Triangle.LengthCounter--;

        if (!_state.Noise.LengthCounterHalt && _state.Noise.LengthCounter > 0)
            _state.Noise.LengthCounter--;
    }

    /// <summary>
    /// Update sweep units
    /// </summary>
    private static void UpdateSweepUnits()
    {
        // Update sweep units for pulse channels
        // In silent mode, we just update the state without affecting audio
    }

    // Register write implementations (simplified for silent mode)
    private void WritePulse1Control(byte value) => _state.Pulse1.DutyCycle = (byte)((value >> 6) & 3);
    private static void WritePulse1Sweep(byte value) { /* Sweep configuration */ }
    private void WritePulse1TimerLow(byte value) => _state.Pulse1.Timer = (ushort)((_state.Pulse1.Timer & 0xFF00) | value);
    private void WritePulse1TimerHigh(byte value) => _state.Pulse1.Timer = (ushort)((_state.Pulse1.Timer & 0x00FF) | ((value & 7) << 8));

    private void WritePulse2Control(byte value) => _state.Pulse2.DutyCycle = (byte)((value >> 6) & 3);
    private static void WritePulse2Sweep(byte value) { /* Sweep configuration */ }
    private void WritePulse2TimerLow(byte value) => _state.Pulse2.Timer = (ushort)((_state.Pulse2.Timer & 0xFF00) | value);
    private void WritePulse2TimerHigh(byte value) => _state.Pulse2.Timer = (ushort)((_state.Pulse2.Timer & 0x00FF) | ((value & 7) << 8));

    private void WriteTriangleControl(byte value) => _state.Triangle.LengthCounterHalt = (value & 0x80) != 0;
    private void WriteTriangleTimerLow(byte value) => _state.Triangle.Timer = (ushort)((_state.Triangle.Timer & 0xFF00) | value);
    private void WriteTriangleTimerHigh(byte value) => _state.Triangle.Timer = (ushort)((_state.Triangle.Timer & 0x00FF) | ((value & 7) << 8));

    private void WriteNoiseControl(byte value) => _state.Noise.LengthCounterHalt = (value & 0x20) != 0;
    private void WriteNoisePeriod(byte value) => _state.Noise.Mode = (value & 0x80) != 0;
    private void WriteNoiseLength(byte value) => _state.Noise.LengthCounter = GetLengthCounterValue((byte)(value >> 3));

    private void WriteDMCControl(byte value) => _state.DMC.IRQEnable = (value & 0x80) != 0;
    private void WriteDMCOutput(byte value) => _state.DMC.DirectLoad = (byte)(value & 0x7F);
    private void WriteDMCSampleAddress(byte value) => _state.DMC.SampleAddress = (ushort)(0xC000 + (value * 64));
    private void WriteDMCSampleLength(byte value) => _state.DMC.SampleLength = (ushort)((value * 16) + 1);

    private byte ReadStatus()
    {
        var status = (byte)_state.Status;
        
        // Clear DMC interrupt flag after reading
        _state.Status &= ~APUStatus.DMCIRQ;
        
        return status;
    }

    private void WriteStatus(byte value)
    {
        _state.Status = (APUStatus)(value & 0x1F);
        
        // Enable/disable channels
        if ((_state.Status & APUStatus.Pulse1Enable) == 0) _state.Pulse1.LengthCounter = 0;
        if ((_state.Status & APUStatus.Pulse2Enable) == 0) _state.Pulse2.LengthCounter = 0;
        if ((_state.Status & APUStatus.TriangleEnable) == 0) _state.Triangle.LengthCounter = 0;
        if ((_state.Status & APUStatus.NoiseEnable) == 0) _state.Noise.LengthCounter = 0;
    }

    private void WriteFrameCounter(byte value)
    {
        _state.FrameCounter.Mode = (value & 0x80) != 0;
        _state.FrameCounter.IRQInhibit = (value & 0x40) != 0;
        
        // Reset frame sequencer
        _frameSequenceStep = 0;
        _frameCounter = 0;
    }

    private static byte GetLengthCounterValue(byte index)
    {
        // Length counter lookup table
        byte[] lengthTable = { 10, 254, 20, 2, 40, 4, 80, 6, 160, 8, 60, 10, 14, 12, 26, 14,
                              12, 16, 24, 18, 48, 20, 96, 22, 192, 24, 72, 26, 16, 28, 32, 30 };
        return lengthTable[index & 0x1F];
    }

    private void OnFrameInterrupt()
    {
        FrameInterrupt?.Invoke(this, new APUInterruptEventArgs("Frame Counter"));
    }
}

/// <summary>
/// Event arguments for APU interrupts
/// </summary>
public class APUInterruptEventArgs : EventArgs
{
    public string Source { get; }

    public APUInterruptEventArgs(string source)
    {
        Source = source;
    }
}

/// <summary>
/// Envelope generator state
/// </summary>
public struct EnvelopeState : IEquatable<EnvelopeState>
{
    public bool Start { get; set; }
    public bool Loop { get; set; }
    public byte Period { get; set; }
    public byte Divider { get; set; }
    public byte DecayLevel { get; set; }

    public readonly bool Equals(EnvelopeState other)
    {
        return Start == other.Start && Loop == other.Loop &&
               Period == other.Period && Divider == other.Divider &&
               DecayLevel == other.DecayLevel;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is EnvelopeState other && Equals(other);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Start, Loop, Period, Divider, DecayLevel);
    }

    public static bool operator ==(EnvelopeState left, EnvelopeState right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(EnvelopeState left, EnvelopeState right)
    {
        return !left.Equals(right);
    }
}
