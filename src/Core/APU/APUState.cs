using System;
using EightBitten.Core.Contracts;

namespace EightBitten.Core.APU;

/// <summary>
/// Complete state of the Audio Processing Unit (APU) for save/load operations
/// </summary>
public class APUState : ComponentState
{
    /// <summary>
    /// Pulse channel 1 state
    /// </summary>
    public PulseChannelState Pulse1 { get; set; } = new();

    /// <summary>
    /// Pulse channel 2 state
    /// </summary>
    public PulseChannelState Pulse2 { get; set; } = new();

    /// <summary>
    /// Triangle channel state
    /// </summary>
    public TriangleChannelState Triangle { get; set; } = new();

    /// <summary>
    /// Noise channel state
    /// </summary>
    public NoiseChannelState Noise { get; set; } = new();

    /// <summary>
    /// DMC (Delta Modulation Channel) state
    /// </summary>
    public DMCChannelState DMC { get; set; } = new();

    /// <summary>
    /// Frame counter state
    /// </summary>
    public FrameCounterState FrameCounter { get; set; } = new();

    /// <summary>
    /// APU status register ($4015)
    /// </summary>
    public APUStatus Status { get; set; }

    /// <summary>
    /// Current APU cycle count
    /// </summary>
    public long APUCycles { get; set; }

    /// <summary>
    /// Whether IRQ is pending
    /// </summary>
    public bool IRQPending { get; set; }

    /// <summary>
    /// Audio output buffer for mixing
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "State class requires arrays for audio buffer")]
    public float[] OutputBuffer { get; set; } = new float[4096];

    /// <summary>
    /// Current position in output buffer
    /// </summary>
    public int BufferPosition { get; set; }

    /// <summary>
    /// Sample rate for audio output
    /// </summary>
    public int SampleRate { get; set; } = 44100;

    /// <summary>
    /// Whether audio output is enabled
    /// </summary>
    public bool AudioEnabled { get; set; } = true;

    public APUState()
    {
        ComponentName = "APU";
        Reset();
    }

    /// <summary>
    /// Reset APU state to power-on values
    /// </summary>
    public void Reset()
    {
        Pulse1.Reset();
        Pulse2.Reset();
        Triangle.Reset();
        Noise.Reset();
        DMC.Reset();
        FrameCounter.Reset();
        
        Status = 0;
        APUCycles = 0;
        IRQPending = false;
        BufferPosition = 0;
        
        Array.Clear(OutputBuffer);
    }

    /// <summary>
    /// Create a deep copy of the APU state
    /// </summary>
    public APUState Clone()
    {
        var clone = new APUState
        {
            ComponentName = ComponentName,
            Timestamp = Timestamp,
            CycleCount = CycleCount,
            Status = Status,
            APUCycles = APUCycles,
            IRQPending = IRQPending,
            BufferPosition = BufferPosition,
            SampleRate = SampleRate,
            AudioEnabled = AudioEnabled
        };

        clone.Pulse1 = Pulse1.Clone();
        clone.Pulse2 = Pulse2.Clone();
        clone.Triangle = Triangle.Clone();
        clone.Noise = Noise.Clone();
        clone.DMC = DMC.Clone();
        clone.FrameCounter = FrameCounter.Clone();

        Array.Copy(OutputBuffer, clone.OutputBuffer, OutputBuffer.Length);

        return clone;
    }

    public override string ToString()
    {
        return $"APU Cycles:{APUCycles} Status:${Status:X2} IRQ:{IRQPending} Buffer:{BufferPosition}/{OutputBuffer.Length}";
    }
}

/// <summary>
/// APU Status register ($4015) flags
/// </summary>
[Flags]
public enum APUStatus : int
{
    Pulse1Enable = 0x01,    // Pulse channel 1 enable
    Pulse2Enable = 0x02,    // Pulse channel 2 enable
    TriangleEnable = 0x04,  // Triangle channel enable
    NoiseEnable = 0x08,     // Noise channel enable
    DMCEnable = 0x10,       // DMC enable
    FrameIRQ = 0x40,        // Frame counter IRQ
    DMCIRQ = 0x80           // DMC IRQ
}

/// <summary>
/// Pulse channel state (channels 1 and 2)
/// </summary>
public class PulseChannelState
{
    public byte DutyCycle { get; set; }         // Duty cycle (0-3)
    public bool LengthCounterHalt { get; set; } // Length counter halt flag
    public bool ConstantVolume { get; set; }    // Constant volume flag
    public byte Volume { get; set; }            // Volume/envelope period
    public ushort Timer { get; set; }           // Timer period
    public byte LengthCounter { get; set; }     // Length counter
    public bool SweepEnabled { get; set; }      // Sweep enabled
    public byte SweepPeriod { get; set; }       // Sweep period
    public bool SweepNegate { get; set; }       // Sweep negate flag
    public byte SweepShift { get; set; }        // Sweep shift amount
    public byte EnvelopeVolume { get; set; }    // Current envelope volume
    public byte EnvelopePeriod { get; set; }    // Envelope period counter
    public bool EnvelopeStart { get; set; }     // Envelope start flag
    public ushort TimerCounter { get; set; }    // Current timer value
    public byte SequencePosition { get; set; }  // Position in duty cycle sequence
    public byte SweepCounter { get; set; }      // Sweep period counter
    public bool SweepReload { get; set; }       // Sweep reload flag

    public void Reset()
    {
        DutyCycle = 0;
        LengthCounterHalt = false;
        ConstantVolume = false;
        Volume = 0;
        Timer = 0;
        LengthCounter = 0;
        SweepEnabled = false;
        SweepPeriod = 0;
        SweepNegate = false;
        SweepShift = 0;
        EnvelopeVolume = 0;
        EnvelopePeriod = 0;
        EnvelopeStart = false;
        TimerCounter = 0;
        SequencePosition = 0;
        SweepCounter = 0;
        SweepReload = false;
    }

    public PulseChannelState Clone()
    {
        return new PulseChannelState
        {
            DutyCycle = DutyCycle,
            LengthCounterHalt = LengthCounterHalt,
            ConstantVolume = ConstantVolume,
            Volume = Volume,
            Timer = Timer,
            LengthCounter = LengthCounter,
            SweepEnabled = SweepEnabled,
            SweepPeriod = SweepPeriod,
            SweepNegate = SweepNegate,
            SweepShift = SweepShift,
            EnvelopeVolume = EnvelopeVolume,
            EnvelopePeriod = EnvelopePeriod,
            EnvelopeStart = EnvelopeStart,
            TimerCounter = TimerCounter,
            SequencePosition = SequencePosition,
            SweepCounter = SweepCounter,
            SweepReload = SweepReload
        };
    }
}

/// <summary>
/// Triangle channel state
/// </summary>
public class TriangleChannelState
{
    public bool LengthCounterHalt { get; set; } // Length counter halt/linear counter control
    public byte LinearCounterLoad { get; set; } // Linear counter load value
    public ushort Timer { get; set; }           // Timer period
    public byte LengthCounter { get; set; }     // Length counter
    public byte LinearCounter { get; set; }     // Linear counter
    public bool LinearCounterReload { get; set; } // Linear counter reload flag
    public ushort TimerCounter { get; set; }    // Current timer value
    public byte SequencePosition { get; set; }  // Position in triangle sequence

    public void Reset()
    {
        LengthCounterHalt = false;
        LinearCounterLoad = 0;
        Timer = 0;
        LengthCounter = 0;
        LinearCounter = 0;
        LinearCounterReload = false;
        TimerCounter = 0;
        SequencePosition = 0;
    }

    public TriangleChannelState Clone()
    {
        return new TriangleChannelState
        {
            LengthCounterHalt = LengthCounterHalt,
            LinearCounterLoad = LinearCounterLoad,
            Timer = Timer,
            LengthCounter = LengthCounter,
            LinearCounter = LinearCounter,
            LinearCounterReload = LinearCounterReload,
            TimerCounter = TimerCounter,
            SequencePosition = SequencePosition
        };
    }
}

/// <summary>
/// Noise channel state
/// </summary>
public class NoiseChannelState
{
    public bool LengthCounterHalt { get; set; } // Length counter halt flag
    public bool ConstantVolume { get; set; }    // Constant volume flag
    public byte Volume { get; set; }            // Volume/envelope period
    public bool Mode { get; set; }              // Mode flag (noise type)
    public byte Period { get; set; }            // Noise period
    public byte LengthCounter { get; set; }     // Length counter
    public byte EnvelopeVolume { get; set; }    // Current envelope volume
    public byte EnvelopePeriod { get; set; }    // Envelope period counter
    public bool EnvelopeStart { get; set; }     // Envelope start flag
    public ushort TimerCounter { get; set; }    // Current timer value
    public ushort ShiftRegister { get; set; }   // Linear feedback shift register

    public void Reset()
    {
        LengthCounterHalt = false;
        ConstantVolume = false;
        Volume = 0;
        Mode = false;
        Period = 0;
        LengthCounter = 0;
        EnvelopeVolume = 0;
        EnvelopePeriod = 0;
        EnvelopeStart = false;
        TimerCounter = 0;
        ShiftRegister = 1; // LFSR starts with 1
    }

    public NoiseChannelState Clone()
    {
        return new NoiseChannelState
        {
            LengthCounterHalt = LengthCounterHalt,
            ConstantVolume = ConstantVolume,
            Volume = Volume,
            Mode = Mode,
            Period = Period,
            LengthCounter = LengthCounter,
            EnvelopeVolume = EnvelopeVolume,
            EnvelopePeriod = EnvelopePeriod,
            EnvelopeStart = EnvelopeStart,
            TimerCounter = TimerCounter,
            ShiftRegister = ShiftRegister
        };
    }
}

/// <summary>
/// DMC (Delta Modulation Channel) state
/// </summary>
public class DMCChannelState
{
    public bool IRQEnable { get; set; }         // IRQ enable flag
    public bool Loop { get; set; }              // Loop flag
    public byte Rate { get; set; }              // Sample rate
    public byte DirectLoad { get; set; }        // Direct load value
    public ushort SampleAddress { get; set; }   // Sample address
    public ushort SampleLength { get; set; }    // Sample length
    public ushort CurrentAddress { get; set; }  // Current address
    public ushort BytesRemaining { get; set; }  // Bytes remaining
    public byte SampleBuffer { get; set; }      // Sample buffer
    public bool SampleBufferEmpty { get; set; } // Sample buffer empty flag
    public byte ShiftRegister { get; set; }     // Shift register
    public byte BitsRemaining { get; set; }     // Bits remaining in shift register
    public bool Silence { get; set; }           // Silence flag
    public ushort TimerCounter { get; set; }    // Current timer value

    public void Reset()
    {
        IRQEnable = false;
        Loop = false;
        Rate = 0;
        DirectLoad = 0;
        SampleAddress = 0;
        SampleLength = 0;
        CurrentAddress = 0;
        BytesRemaining = 0;
        SampleBuffer = 0;
        SampleBufferEmpty = true;
        ShiftRegister = 0;
        BitsRemaining = 0;
        Silence = true;
        TimerCounter = 0;
    }

    public DMCChannelState Clone()
    {
        return new DMCChannelState
        {
            IRQEnable = IRQEnable,
            Loop = Loop,
            Rate = Rate,
            DirectLoad = DirectLoad,
            SampleAddress = SampleAddress,
            SampleLength = SampleLength,
            CurrentAddress = CurrentAddress,
            BytesRemaining = BytesRemaining,
            SampleBuffer = SampleBuffer,
            SampleBufferEmpty = SampleBufferEmpty,
            ShiftRegister = ShiftRegister,
            BitsRemaining = BitsRemaining,
            Silence = Silence,
            TimerCounter = TimerCounter
        };
    }
}

/// <summary>
/// Frame counter state
/// </summary>
public class FrameCounterState
{
    public bool Mode { get; set; }              // Frame counter mode (0: 4-step, 1: 5-step)
    public bool IRQInhibit { get; set; }        // IRQ inhibit flag
    public int Step { get; set; }               // Current step in sequence
    public int Counter { get; set; }            // Cycle counter

    public void Reset()
    {
        Mode = false;
        IRQInhibit = false;
        Step = 0;
        Counter = 0;
    }

    public FrameCounterState Clone()
    {
        return new FrameCounterState
        {
            Mode = Mode,
            IRQInhibit = IRQInhibit,
            Step = Step,
            Counter = Counter
        };
    }
}
