using System;

namespace EightBitten.Core.CPU;

/// <summary>
/// 6502 addressing modes for instruction execution
/// </summary>
public enum AddressingMode
{
    /// <summary>
    /// Implied - no operand (e.g., NOP, RTS)
    /// </summary>
    Implied,

    /// <summary>
    /// Accumulator - operates on accumulator (e.g., ASL A)
    /// </summary>
    Accumulator,

    /// <summary>
    /// Immediate - operand is next byte (e.g., LDA #$10)
    /// </summary>
    Immediate,

    /// <summary>
    /// Zero Page - operand is address in zero page (e.g., LDA $10)
    /// </summary>
    ZeroPage,

    /// <summary>
    /// Zero Page,X - zero page address + X register (e.g., LDA $10,X)
    /// </summary>
    ZeroPageX,

    /// <summary>
    /// Zero Page,Y - zero page address + Y register (e.g., LDX $10,Y)
    /// </summary>
    ZeroPageY,

    /// <summary>
    /// Absolute - operand is 16-bit address (e.g., LDA $1234)
    /// </summary>
    Absolute,

    /// <summary>
    /// Absolute,X - absolute address + X register (e.g., LDA $1234,X)
    /// </summary>
    AbsoluteX,

    /// <summary>
    /// Absolute,Y - absolute address + Y register (e.g., LDA $1234,Y)
    /// </summary>
    AbsoluteY,

    /// <summary>
    /// Indirect - address is read from operand address (e.g., JMP ($1234))
    /// </summary>
    Indirect,

    /// <summary>
    /// Indexed Indirect - (zero page + X) (e.g., LDA ($10,X))
    /// </summary>
    IndexedIndirect,

    /// <summary>
    /// Indirect Indexed - (zero page) + Y (e.g., LDA ($10),Y)
    /// </summary>
    IndirectIndexed,

    /// <summary>
    /// Relative - signed 8-bit offset for branches (e.g., BNE $10)
    /// </summary>
    Relative
}

/// <summary>
/// Addressing mode helper methods for CPU instruction execution
/// </summary>
public static class AddressingModeHelpers
{
    /// <summary>
    /// Get the number of bytes for an addressing mode
    /// </summary>
    /// <param name="mode">Addressing mode</param>
    /// <returns>Number of bytes (including opcode)</returns>
    public static int GetInstructionLength(AddressingMode mode)
    {
        return mode switch
        {
            AddressingMode.Implied => 1,
            AddressingMode.Accumulator => 1,
            AddressingMode.Immediate => 2,
            AddressingMode.ZeroPage => 2,
            AddressingMode.ZeroPageX => 2,
            AddressingMode.ZeroPageY => 2,
            AddressingMode.Absolute => 3,
            AddressingMode.AbsoluteX => 3,
            AddressingMode.AbsoluteY => 3,
            AddressingMode.Indirect => 3,
            AddressingMode.IndexedIndirect => 2,
            AddressingMode.IndirectIndexed => 2,
            AddressingMode.Relative => 2,
            _ => throw new ArgumentException($"Unknown addressing mode: {mode}")
        };
    }

    /// <summary>
    /// Get the base cycle count for an addressing mode
    /// </summary>
    /// <param name="mode">Addressing mode</param>
    /// <returns>Base cycle count</returns>
    public static int GetBaseCycles(AddressingMode mode)
    {
        return mode switch
        {
            AddressingMode.Implied => 2,
            AddressingMode.Accumulator => 2,
            AddressingMode.Immediate => 2,
            AddressingMode.ZeroPage => 3,
            AddressingMode.ZeroPageX => 4,
            AddressingMode.ZeroPageY => 4,
            AddressingMode.Absolute => 4,
            AddressingMode.AbsoluteX => 4, // +1 if page crossed
            AddressingMode.AbsoluteY => 4, // +1 if page crossed
            AddressingMode.Indirect => 5,
            AddressingMode.IndexedIndirect => 6,
            AddressingMode.IndirectIndexed => 5, // +1 if page crossed
            AddressingMode.Relative => 2, // +1 if branch taken, +2 if page crossed
            _ => throw new ArgumentException($"Unknown addressing mode: {mode}")
        };
    }

    /// <summary>
    /// Check if addressing mode can cause page boundary crossing
    /// </summary>
    /// <param name="mode">Addressing mode</param>
    /// <returns>True if page crossing is possible</returns>
    public static bool CanCrossPageBoundary(AddressingMode mode)
    {
        return mode switch
        {
            AddressingMode.AbsoluteX => true,
            AddressingMode.AbsoluteY => true,
            AddressingMode.IndirectIndexed => true,
            AddressingMode.Relative => true,
            _ => false
        };
    }

    /// <summary>
    /// Check if two addresses are on different pages
    /// </summary>
    /// <param name="address1">First address</param>
    /// <param name="address2">Second address</param>
    /// <returns>True if addresses are on different pages</returns>
    public static bool IsPageCrossed(ushort address1, ushort address2)
    {
        return (address1 & 0xFF00) != (address2 & 0xFF00);
    }

    /// <summary>
    /// Get addressing mode description for debugging
    /// </summary>
    /// <param name="mode">Addressing mode</param>
    /// <returns>Human-readable description</returns>
    public static string GetDescription(AddressingMode mode)
    {
        return mode switch
        {
            AddressingMode.Implied => "Implied",
            AddressingMode.Accumulator => "Accumulator",
            AddressingMode.Immediate => "Immediate (#$nn)",
            AddressingMode.ZeroPage => "Zero Page ($nn)",
            AddressingMode.ZeroPageX => "Zero Page,X ($nn,X)",
            AddressingMode.ZeroPageY => "Zero Page,Y ($nn,Y)",
            AddressingMode.Absolute => "Absolute ($nnnn)",
            AddressingMode.AbsoluteX => "Absolute,X ($nnnn,X)",
            AddressingMode.AbsoluteY => "Absolute,Y ($nnnn,Y)",
            AddressingMode.Indirect => "Indirect (($nnnn))",
            AddressingMode.IndexedIndirect => "Indexed Indirect (($nn,X))",
            AddressingMode.IndirectIndexed => "Indirect Indexed (($nn),Y)",
            AddressingMode.Relative => "Relative ($nn)",
            _ => "Unknown"
        };
    }
}

/// <summary>
/// Result of address calculation for an instruction
/// </summary>
public readonly struct AddressResult : IEquatable<AddressResult>
{
    /// <summary>
    /// Effective address for the operation
    /// </summary>
    public ushort Address { get; }

    /// <summary>
    /// Whether a page boundary was crossed
    /// </summary>
    public bool PageCrossed { get; }

    /// <summary>
    /// Additional cycles due to page crossing or other factors
    /// </summary>
    public int ExtraCycles { get; }

    public AddressResult(ushort address, bool pageCrossed = false, int extraCycles = 0)
    {
        Address = address;
        PageCrossed = pageCrossed;
        ExtraCycles = extraCycles;
    }

    /// <summary>
    /// Create result with page crossing penalty
    /// </summary>
    public static AddressResult WithPageCrossing(ushort address, bool pageCrossed)
    {
        return new AddressResult(address, pageCrossed, pageCrossed ? 1 : 0);
    }

    public bool Equals(AddressResult other)
    {
        return Address == other.Address && PageCrossed == other.PageCrossed && ExtraCycles == other.ExtraCycles;
    }

    public override bool Equals(object? obj)
    {
        return obj is AddressResult other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Address, PageCrossed, ExtraCycles);
    }

    public static bool operator ==(AddressResult left, AddressResult right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AddressResult left, AddressResult right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
/// Address calculation methods for CPU instructions
/// </summary>
public static class AddressCalculator
{
    /// <summary>
    /// Calculate effective address for an addressing mode
    /// </summary>
    /// <param name="cpu">CPU instance</param>
    /// <param name="mode">Addressing mode</param>
    /// <param name="operand1">First operand byte (if applicable)</param>
    /// <param name="operand2">Second operand byte (if applicable)</param>
    /// <returns>Address calculation result</returns>
    public static AddressResult CalculateAddress(CPU6502 cpu, AddressingMode mode, byte operand1 = 0, byte operand2 = 0)
    {
        ArgumentNullException.ThrowIfNull(cpu);
        var state = cpu.CurrentState;
        
        return mode switch
        {
            AddressingMode.Implied => new AddressResult(0),
            AddressingMode.Accumulator => new AddressResult(0),
            AddressingMode.Immediate => new AddressResult((ushort)(state.PC - 1)), // PC already advanced
            
            AddressingMode.ZeroPage => new AddressResult(operand1),
            
            AddressingMode.ZeroPageX => new AddressResult((byte)(operand1 + state.X)),
            
            AddressingMode.ZeroPageY => new AddressResult((byte)(operand1 + state.Y)),
            
            AddressingMode.Absolute => new AddressResult((ushort)(operand1 | (operand2 << 8))),
            
            AddressingMode.AbsoluteX => CalculateAbsoluteIndexed(operand1, operand2, state.X),
            
            AddressingMode.AbsoluteY => CalculateAbsoluteIndexed(operand1, operand2, state.Y),
            
            AddressingMode.Indirect => CalculateIndirect(cpu, operand1, operand2),
            
            AddressingMode.IndexedIndirect => CalculateIndexedIndirect(cpu, operand1, state.X),
            
            AddressingMode.IndirectIndexed => CalculateIndirectIndexed(cpu, operand1, state.Y),
            
            AddressingMode.Relative => CalculateRelative(state.PC, operand1),
            
            _ => throw new ArgumentException($"Unknown addressing mode: {mode}")
        };
    }

    private static AddressResult CalculateAbsoluteIndexed(byte low, byte high, byte index)
    {
        var baseAddress = (ushort)(low | (high << 8));
        var effectiveAddress = (ushort)(baseAddress + index);
        var pageCrossed = AddressingModeHelpers.IsPageCrossed(baseAddress, effectiveAddress);
        
        return AddressResult.WithPageCrossing(effectiveAddress, pageCrossed);
    }

    private static AddressResult CalculateIndirect(CPU6502 cpu, byte low, byte high)
    {
        var indirectAddress = (ushort)(low | (high << 8));
        
        // 6502 bug: if indirect address is on page boundary, high byte wraps within page
        ushort effectiveAddress;
        if ((indirectAddress & 0xFF) == 0xFF)
        {
            var lowByte = cpu.ReadByte(indirectAddress);
            var highByte = cpu.ReadByte((ushort)(indirectAddress & 0xFF00)); // Wrap to start of page
            effectiveAddress = (ushort)(lowByte | (highByte << 8));
        }
        else
        {
            effectiveAddress = cpu.ReadWord(indirectAddress);
        }
        
        return new AddressResult(effectiveAddress);
    }

    private static AddressResult CalculateIndexedIndirect(CPU6502 cpu, byte zeroPageAddress, byte index)
    {
        var indirectAddress = (byte)(zeroPageAddress + index);
        var effectiveAddress = cpu.ReadWord(indirectAddress);
        
        return new AddressResult(effectiveAddress);
    }

    private static AddressResult CalculateIndirectIndexed(CPU6502 cpu, byte zeroPageAddress, byte index)
    {
        var baseAddress = cpu.ReadWord(zeroPageAddress);
        var effectiveAddress = (ushort)(baseAddress + index);
        var pageCrossed = AddressingModeHelpers.IsPageCrossed(baseAddress, effectiveAddress);
        
        return AddressResult.WithPageCrossing(effectiveAddress, pageCrossed);
    }

    private static AddressResult CalculateRelative(ushort pc, byte offset)
    {
        // Convert unsigned offset to signed
        var signedOffset = (sbyte)offset;
        var targetAddress = (ushort)(pc + signedOffset);
        var pageCrossed = AddressingModeHelpers.IsPageCrossed(pc, targetAddress);
        
        return AddressResult.WithPageCrossing(targetAddress, pageCrossed);
    }
}
