using System;
using System.Collections.Generic;

namespace EightBitten.Core.CPU;

/// <summary>
/// 6502 instruction types
/// </summary>
public enum InstructionType
{
    // Load/Store Operations
    LDA, LDX, LDY, STA, STX, STY,
    
    // Register Transfers
    TAX, TAY, TXA, TYA, TSX, TXS,
    
    // Stack Operations
    PHA, PHP, PLA, PLP,
    
    // Logical Operations
    AND, EOR, ORA, BIT,
    
    // Arithmetic Operations
    ADC, SBC, CMP, CPX, CPY,
    
    // Increment/Decrement
    INC, INX, INY, DEC, DEX, DEY,
    
    // Shifts
    ASL, LSR, ROL, ROR,
    
    // Jumps & Calls
    JMP, JSR, RTS,
    
    // Branches
    BCC, BCS, BEQ, BMI, BNE, BPL, BVC, BVS,
    
    // Status Flag Changes
    CLC, CLD, CLI, CLV, SEC, SED, SEI,
    
    // System Functions
    BRK, NOP, RTI,
    
    // Illegal/Undocumented Instructions
    LAX, SAX, DCP, ISC, RLA, RRA, SLO, SRE,
    AHX, SHY, SHX, TAS, XAA, AXS, LAS, KIL
}

/// <summary>
/// 6502 instruction definition
/// </summary>
public readonly struct InstructionDefinition : IEquatable<InstructionDefinition>
{
    /// <summary>
    /// Instruction type
    /// </summary>
    public InstructionType Type { get; }

    /// <summary>
    /// Addressing mode
    /// </summary>
    public AddressingMode AddressingMode { get; }

    /// <summary>
    /// Base cycle count
    /// </summary>
    public int Cycles { get; }

    /// <summary>
    /// Whether instruction can add extra cycle on page crossing
    /// </summary>
    public bool CanAddCycle { get; }

    /// <summary>
    /// Whether this is an illegal/undocumented instruction
    /// </summary>
    public bool IsIllegal { get; }

    /// <summary>
    /// Instruction mnemonic for debugging
    /// </summary>
    public string Mnemonic { get; }

    public InstructionDefinition(InstructionType type, AddressingMode addressingMode, int cycles,
        bool canAddCycle = false, bool isIllegal = false, string? mnemonic = null)
    {
        Type = type;
        AddressingMode = addressingMode;
        Cycles = cycles;
        CanAddCycle = canAddCycle;
        IsIllegal = isIllegal;
        Mnemonic = mnemonic ?? type.ToString();
    }

    public bool Equals(InstructionDefinition other)
    {
        return Type == other.Type &&
               AddressingMode == other.AddressingMode &&
               Cycles == other.Cycles &&
               CanAddCycle == other.CanAddCycle &&
               IsIllegal == other.IsIllegal &&
               Mnemonic == other.Mnemonic;
    }

    public override bool Equals(object? obj)
    {
        return obj is InstructionDefinition other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, AddressingMode, Cycles, CanAddCycle, IsIllegal, Mnemonic);
    }

    public static bool operator ==(InstructionDefinition left, InstructionDefinition right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(InstructionDefinition left, InstructionDefinition right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
/// 6502 instruction set lookup table and execution framework
/// </summary>
public static class InstructionSet
{
    /// <summary>
    /// Instruction lookup table indexed by opcode
    /// </summary>
    private static readonly InstructionDefinition[] Instructions = new InstructionDefinition[256];

    /// <summary>
    /// Initialize instruction set
    /// </summary>
    static InstructionSet()
    {
        InitializeInstructions();
    }

    /// <summary>
    /// Get instruction definition for opcode
    /// </summary>
    /// <param name="opcode">Instruction opcode</param>
    /// <returns>Instruction definition</returns>
    public static InstructionDefinition GetInstruction(byte opcode)
    {
        return Instructions[opcode];
    }

    /// <summary>
    /// Check if opcode is a valid instruction
    /// </summary>
    /// <param name="opcode">Instruction opcode</param>
    /// <returns>True if valid instruction</returns>
    public static bool IsValidInstruction(byte opcode)
    {
        var instruction = Instructions[opcode];
        return instruction.Type != InstructionType.KIL || instruction.IsIllegal;
    }

    /// <summary>
    /// Get all opcodes for a specific instruction type
    /// </summary>
    /// <param name="type">Instruction type</param>
    /// <returns>List of opcodes</returns>
    public static IEnumerable<byte> GetOpcodesForInstruction(InstructionType type)
    {
        for (int i = 0; i < 256; i++)
        {
            if (Instructions[i].Type == type)
                yield return (byte)i;
        }
    }

    /// <summary>
    /// Initialize the instruction lookup table
    /// </summary>
    private static void InitializeInstructions()
    {
        // Initialize all opcodes to KIL (illegal instruction that halts CPU)
        for (int i = 0; i < 256; i++)
        {
            Instructions[i] = new InstructionDefinition(InstructionType.KIL, AddressingMode.Implied, 2, false, true, "KIL");
        }

        // Load/Store Operations
        Instructions[0xA9] = new(InstructionType.LDA, AddressingMode.Immediate, 2);
        Instructions[0xA5] = new(InstructionType.LDA, AddressingMode.ZeroPage, 3);
        Instructions[0xB5] = new(InstructionType.LDA, AddressingMode.ZeroPageX, 4);
        Instructions[0xAD] = new(InstructionType.LDA, AddressingMode.Absolute, 4);
        Instructions[0xBD] = new(InstructionType.LDA, AddressingMode.AbsoluteX, 4, true);
        Instructions[0xB9] = new(InstructionType.LDA, AddressingMode.AbsoluteY, 4, true);
        Instructions[0xA1] = new(InstructionType.LDA, AddressingMode.IndexedIndirect, 6);
        Instructions[0xB1] = new(InstructionType.LDA, AddressingMode.IndirectIndexed, 5, true);

        Instructions[0xA2] = new(InstructionType.LDX, AddressingMode.Immediate, 2);
        Instructions[0xA6] = new(InstructionType.LDX, AddressingMode.ZeroPage, 3);
        Instructions[0xB6] = new(InstructionType.LDX, AddressingMode.ZeroPageY, 4);
        Instructions[0xAE] = new(InstructionType.LDX, AddressingMode.Absolute, 4);
        Instructions[0xBE] = new(InstructionType.LDX, AddressingMode.AbsoluteY, 4, true);

        Instructions[0xA0] = new(InstructionType.LDY, AddressingMode.Immediate, 2);
        Instructions[0xA4] = new(InstructionType.LDY, AddressingMode.ZeroPage, 3);
        Instructions[0xB4] = new(InstructionType.LDY, AddressingMode.ZeroPageX, 4);
        Instructions[0xAC] = new(InstructionType.LDY, AddressingMode.Absolute, 4);
        Instructions[0xBC] = new(InstructionType.LDY, AddressingMode.AbsoluteX, 4, true);

        Instructions[0x85] = new(InstructionType.STA, AddressingMode.ZeroPage, 3);
        Instructions[0x95] = new(InstructionType.STA, AddressingMode.ZeroPageX, 4);
        Instructions[0x8D] = new(InstructionType.STA, AddressingMode.Absolute, 4);
        Instructions[0x9D] = new(InstructionType.STA, AddressingMode.AbsoluteX, 5);
        Instructions[0x99] = new(InstructionType.STA, AddressingMode.AbsoluteY, 5);
        Instructions[0x81] = new(InstructionType.STA, AddressingMode.IndexedIndirect, 6);
        Instructions[0x91] = new(InstructionType.STA, AddressingMode.IndirectIndexed, 6);

        Instructions[0x86] = new(InstructionType.STX, AddressingMode.ZeroPage, 3);
        Instructions[0x96] = new(InstructionType.STX, AddressingMode.ZeroPageY, 4);
        Instructions[0x8E] = new(InstructionType.STX, AddressingMode.Absolute, 4);

        Instructions[0x84] = new(InstructionType.STY, AddressingMode.ZeroPage, 3);
        Instructions[0x94] = new(InstructionType.STY, AddressingMode.ZeroPageX, 4);
        Instructions[0x8C] = new(InstructionType.STY, AddressingMode.Absolute, 4);

        // Register Transfers
        Instructions[0xAA] = new(InstructionType.TAX, AddressingMode.Implied, 2);
        Instructions[0xA8] = new(InstructionType.TAY, AddressingMode.Implied, 2);
        Instructions[0x8A] = new(InstructionType.TXA, AddressingMode.Implied, 2);
        Instructions[0x98] = new(InstructionType.TYA, AddressingMode.Implied, 2);
        Instructions[0xBA] = new(InstructionType.TSX, AddressingMode.Implied, 2);
        Instructions[0x9A] = new(InstructionType.TXS, AddressingMode.Implied, 2);

        // Stack Operations
        Instructions[0x48] = new(InstructionType.PHA, AddressingMode.Implied, 3);
        Instructions[0x08] = new(InstructionType.PHP, AddressingMode.Implied, 3);
        Instructions[0x68] = new(InstructionType.PLA, AddressingMode.Implied, 4);
        Instructions[0x28] = new(InstructionType.PLP, AddressingMode.Implied, 4);

        // Logical Operations
        Instructions[0x29] = new(InstructionType.AND, AddressingMode.Immediate, 2);
        Instructions[0x25] = new(InstructionType.AND, AddressingMode.ZeroPage, 3);
        Instructions[0x35] = new(InstructionType.AND, AddressingMode.ZeroPageX, 4);
        Instructions[0x2D] = new(InstructionType.AND, AddressingMode.Absolute, 4);
        Instructions[0x3D] = new(InstructionType.AND, AddressingMode.AbsoluteX, 4, true);
        Instructions[0x39] = new(InstructionType.AND, AddressingMode.AbsoluteY, 4, true);
        Instructions[0x21] = new(InstructionType.AND, AddressingMode.IndexedIndirect, 6);
        Instructions[0x31] = new(InstructionType.AND, AddressingMode.IndirectIndexed, 5, true);

        Instructions[0x49] = new(InstructionType.EOR, AddressingMode.Immediate, 2);
        Instructions[0x45] = new(InstructionType.EOR, AddressingMode.ZeroPage, 3);
        Instructions[0x55] = new(InstructionType.EOR, AddressingMode.ZeroPageX, 4);
        Instructions[0x4D] = new(InstructionType.EOR, AddressingMode.Absolute, 4);
        Instructions[0x5D] = new(InstructionType.EOR, AddressingMode.AbsoluteX, 4, true);
        Instructions[0x59] = new(InstructionType.EOR, AddressingMode.AbsoluteY, 4, true);
        Instructions[0x41] = new(InstructionType.EOR, AddressingMode.IndexedIndirect, 6);
        Instructions[0x51] = new(InstructionType.EOR, AddressingMode.IndirectIndexed, 5, true);

        Instructions[0x09] = new(InstructionType.ORA, AddressingMode.Immediate, 2);
        Instructions[0x05] = new(InstructionType.ORA, AddressingMode.ZeroPage, 3);
        Instructions[0x15] = new(InstructionType.ORA, AddressingMode.ZeroPageX, 4);
        Instructions[0x0D] = new(InstructionType.ORA, AddressingMode.Absolute, 4);
        Instructions[0x1D] = new(InstructionType.ORA, AddressingMode.AbsoluteX, 4, true);
        Instructions[0x19] = new(InstructionType.ORA, AddressingMode.AbsoluteY, 4, true);
        Instructions[0x01] = new(InstructionType.ORA, AddressingMode.IndexedIndirect, 6);
        Instructions[0x11] = new(InstructionType.ORA, AddressingMode.IndirectIndexed, 5, true);

        Instructions[0x24] = new(InstructionType.BIT, AddressingMode.ZeroPage, 3);
        Instructions[0x2C] = new(InstructionType.BIT, AddressingMode.Absolute, 4);

        // System Functions
        Instructions[0x00] = new(InstructionType.BRK, AddressingMode.Implied, 7);
        Instructions[0xEA] = new(InstructionType.NOP, AddressingMode.Implied, 2);
        Instructions[0x40] = new(InstructionType.RTI, AddressingMode.Implied, 6);

        // Jumps & Calls
        Instructions[0x4C] = new(InstructionType.JMP, AddressingMode.Absolute, 3);
        Instructions[0x6C] = new(InstructionType.JMP, AddressingMode.Indirect, 5);
        Instructions[0x20] = new(InstructionType.JSR, AddressingMode.Absolute, 6);
        Instructions[0x60] = new(InstructionType.RTS, AddressingMode.Implied, 6);

        // Branches
        Instructions[0x90] = new(InstructionType.BCC, AddressingMode.Relative, 2, true);
        Instructions[0xB0] = new(InstructionType.BCS, AddressingMode.Relative, 2, true);
        Instructions[0xF0] = new(InstructionType.BEQ, AddressingMode.Relative, 2, true);
        Instructions[0x30] = new(InstructionType.BMI, AddressingMode.Relative, 2, true);
        Instructions[0xD0] = new(InstructionType.BNE, AddressingMode.Relative, 2, true);
        Instructions[0x10] = new(InstructionType.BPL, AddressingMode.Relative, 2, true);
        Instructions[0x50] = new(InstructionType.BVC, AddressingMode.Relative, 2, true);
        Instructions[0x70] = new(InstructionType.BVS, AddressingMode.Relative, 2, true);

        // Status Flag Changes
        Instructions[0x18] = new(InstructionType.CLC, AddressingMode.Implied, 2);
        Instructions[0xD8] = new(InstructionType.CLD, AddressingMode.Implied, 2);
        Instructions[0x58] = new(InstructionType.CLI, AddressingMode.Implied, 2);
        Instructions[0xB8] = new(InstructionType.CLV, AddressingMode.Implied, 2);
        Instructions[0x38] = new(InstructionType.SEC, AddressingMode.Implied, 2);
        Instructions[0xF8] = new(InstructionType.SED, AddressingMode.Implied, 2);
        Instructions[0x78] = new(InstructionType.SEI, AddressingMode.Implied, 2);

        // Add more instructions as needed...
        // This is a partial implementation - T021 will complete the full instruction set
    }

    /// <summary>
    /// Get instruction mnemonic with addressing mode for debugging
    /// </summary>
    /// <param name="opcode">Instruction opcode</param>
    /// <param name="operand1">First operand (if applicable)</param>
    /// <param name="operand2">Second operand (if applicable)</param>
    /// <returns>Formatted instruction string</returns>
    public static string FormatInstruction(byte opcode, byte operand1 = 0, byte operand2 = 0)
    {
        var instruction = Instructions[opcode];
        var mnemonic = instruction.Mnemonic;

        return instruction.AddressingMode switch
        {
            AddressingMode.Implied => mnemonic,
            AddressingMode.Accumulator => $"{mnemonic} A",
            AddressingMode.Immediate => $"{mnemonic} #${operand1:X2}",
            AddressingMode.ZeroPage => $"{mnemonic} ${operand1:X2}",
            AddressingMode.ZeroPageX => $"{mnemonic} ${operand1:X2},X",
            AddressingMode.ZeroPageY => $"{mnemonic} ${operand1:X2},Y",
            AddressingMode.Absolute => $"{mnemonic} ${operand2:X2}{operand1:X2}",
            AddressingMode.AbsoluteX => $"{mnemonic} ${operand2:X2}{operand1:X2},X",
            AddressingMode.AbsoluteY => $"{mnemonic} ${operand2:X2}{operand1:X2},Y",
            AddressingMode.Indirect => $"{mnemonic} (${operand2:X2}{operand1:X2})",
            AddressingMode.IndexedIndirect => $"{mnemonic} (${operand1:X2},X)",
            AddressingMode.IndirectIndexed => $"{mnemonic} (${operand1:X2}),Y",
            AddressingMode.Relative => $"{mnemonic} ${(ushort)(operand1 + 2):X4}",
            _ => $"{mnemonic} ???"
        };
    }
}
