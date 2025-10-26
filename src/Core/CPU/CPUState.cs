using System;
using EightBitten.Core.Contracts;

namespace EightBitten.Core.CPU;

/// <summary>
/// Complete state of the 6502 CPU for save/load operations and debugging
/// </summary>
public class CPUState : ComponentState
{
    /// <summary>
    /// Program Counter (16-bit)
    /// </summary>
    public ushort PC { get; set; }

    /// <summary>
    /// Stack Pointer (8-bit, points to $0100-$01FF)
    /// </summary>
    public byte SP { get; set; }

    /// <summary>
    /// Accumulator register (8-bit)
    /// </summary>
    public byte A { get; set; }

    /// <summary>
    /// X index register (8-bit)
    /// </summary>
    public byte X { get; set; }

    /// <summary>
    /// Y index register (8-bit)
    /// </summary>
    public byte Y { get; set; }

    /// <summary>
    /// Processor status flags
    /// </summary>
    public ProcessorStatus P { get; set; }

    /// <summary>
    /// Current instruction being executed
    /// </summary>
    public byte CurrentInstruction { get; set; }

    /// <summary>
    /// Current instruction cycle (0-based)
    /// </summary>
    public int InstructionCycle { get; set; }

    /// <summary>
    /// Total cycles executed since reset
    /// </summary>
    public long TotalCycles { get; set; }

    /// <summary>
    /// Whether CPU is in a halted state
    /// </summary>
    public bool IsHalted { get; set; }

    /// <summary>
    /// Pending interrupt flags
    /// </summary>
    public InterruptType PendingInterrupts { get; set; }

    /// <summary>
    /// Last memory address accessed
    /// </summary>
    public ushort LastMemoryAddress { get; set; }

    /// <summary>
    /// Last memory value read/written
    /// </summary>
    public byte LastMemoryValue { get; set; }

    /// <summary>
    /// Whether last memory operation was a write
    /// </summary>
    public bool LastMemoryWasWrite { get; set; }

    public CPUState()
    {
        ComponentName = "CPU";
        Reset();
    }

    /// <summary>
    /// Reset CPU state to power-on values
    /// </summary>
    public void Reset()
    {
        // 6502 power-on state
        PC = 0x0000;  // Will be loaded from reset vector
        SP = 0xFD;    // Stack pointer starts at $01FD
        A = 0x00;
        X = 0x00;
        Y = 0x00;
        P = ProcessorStatus.Interrupt | ProcessorStatus.Unused; // I flag set, unused bit always 1
        
        CurrentInstruction = 0x00;
        InstructionCycle = 0;
        TotalCycles = 0;
        IsHalted = false;
        PendingInterrupts = InterruptType.None;
        
        LastMemoryAddress = 0x0000;
        LastMemoryValue = 0x00;
        LastMemoryWasWrite = false;
    }

    /// <summary>
    /// Create a deep copy of the CPU state
    /// </summary>
    /// <returns>Cloned CPU state</returns>
    public CPUState Clone()
    {
        return new CPUState
        {
            ComponentName = ComponentName,
            Timestamp = Timestamp,
            CycleCount = CycleCount,
            PC = PC,
            SP = SP,
            A = A,
            X = X,
            Y = Y,
            P = P,
            CurrentInstruction = CurrentInstruction,
            InstructionCycle = InstructionCycle,
            TotalCycles = TotalCycles,
            IsHalted = IsHalted,
            PendingInterrupts = PendingInterrupts,
            LastMemoryAddress = LastMemoryAddress,
            LastMemoryValue = LastMemoryValue,
            LastMemoryWasWrite = LastMemoryWasWrite
        };
    }

    public override string ToString()
    {
        return $"PC:${PC:X4} SP:${SP:X2} A:${A:X2} X:${X:X2} Y:${Y:X2} P:{P} Cycles:{TotalCycles}";
    }
}

/// <summary>
/// 6502 processor status register (P register)
/// </summary>
[Flags]
public enum ProcessorStatus : int
{
    /// <summary>
    /// Carry flag (bit 0)
    /// </summary>
    Carry = 0x01,

    /// <summary>
    /// Zero flag (bit 1)
    /// </summary>
    Zero = 0x02,

    /// <summary>
    /// Interrupt disable flag (bit 2)
    /// </summary>
    Interrupt = 0x04,

    /// <summary>
    /// Decimal mode flag (bit 3) - not used on NES
    /// </summary>
    DecimalMode = 0x08,

    /// <summary>
    /// Break flag (bit 4) - only set when pushed to stack
    /// </summary>
    Break = 0x10,

    /// <summary>
    /// Unused flag (bit 5) - always 1
    /// </summary>
    Unused = 0x20,

    /// <summary>
    /// Overflow flag (bit 6)
    /// </summary>
    Overflow = 0x40,

    /// <summary>
    /// Negative flag (bit 7)
    /// </summary>
    Negative = 0x80
}

/// <summary>
/// 6502 interrupt types
/// </summary>
[Flags]
public enum InterruptType : int
{
    /// <summary>
    /// No pending interrupts
    /// </summary>
    None = 0x00,

    /// <summary>
    /// Non-maskable interrupt (NMI)
    /// </summary>
    NMI = 0x01,

    /// <summary>
    /// Interrupt request (IRQ)
    /// </summary>
    IRQ = 0x02,

    /// <summary>
    /// Reset interrupt
    /// </summary>
    Reset = 0x04
}

/// <summary>
/// Extension methods for processor status
/// </summary>
public static class ProcessorStatusExtensions
{
    /// <summary>
    /// Check if a specific flag is set
    /// </summary>
    public static bool HasFlag(this ProcessorStatus flags, ProcessorStatus flag)
    {
        return (flags & flag) == flag;
    }

    /// <summary>
    /// Set a specific flag
    /// </summary>
    public static ProcessorStatus SetFlag(this ProcessorStatus flags, ProcessorStatus flag, bool value)
    {
        return value ? flags | flag : flags & ~flag;
    }

    /// <summary>
    /// Update Zero and Negative flags based on a value
    /// </summary>
    public static ProcessorStatus UpdateZN(this ProcessorStatus flags, byte value)
    {
        flags = flags.SetFlag(ProcessorStatus.Zero, value == 0);
        flags = flags.SetFlag(ProcessorStatus.Negative, (value & 0x80) != 0);
        return flags;
    }
}
