using System;
using System.Collections.Generic;
using EightBitten.Core.Contracts;

namespace EightBitten.Core.Memory;

/// <summary>
/// Interface for NES memory mapping and address space management
/// </summary>
public interface IMemoryMap : IDisposable
{
    /// <summary>
    /// Read a byte from the specified address
    /// </summary>
    /// <param name="address">16-bit address</param>
    /// <returns>Byte value at address</returns>
    byte ReadByte(ushort address);

    /// <summary>
    /// Write a byte to the specified address
    /// </summary>
    /// <param name="address">16-bit address</param>
    /// <param name="value">Byte value to write</param>
    void WriteByte(ushort address, byte value);

    /// <summary>
    /// Read a 16-bit word from the specified address (little-endian)
    /// </summary>
    /// <param name="address">16-bit address</param>
    /// <returns>16-bit word value</returns>
    ushort ReadWord(ushort address);

    /// <summary>
    /// Write a 16-bit word to the specified address (little-endian)
    /// </summary>
    /// <param name="address">16-bit address</param>
    /// <param name="value">16-bit word value to write</param>
    void WriteWord(ushort address, ushort value);

    /// <summary>
    /// Register a memory-mapped component for a specific address range
    /// </summary>
    /// <param name="component">Component to register</param>
    /// <param name="priority">Priority for overlapping ranges (higher = higher priority)</param>
    void RegisterComponent(IMemoryMappedComponent component, int priority = 0);

    /// <summary>
    /// Unregister a memory-mapped component
    /// </summary>
    /// <param name="component">Component to unregister</param>
    void UnregisterComponent(IMemoryMappedComponent component);

    /// <summary>
    /// Get all registered components for debugging
    /// </summary>
    /// <returns>List of registered components with their ranges</returns>
    IReadOnlyList<MemoryMappingInfo> GetMappings();

    /// <summary>
    /// Reset all memory to initial state
    /// </summary>
    void Reset();

    /// <summary>
    /// Get memory state for save/load operations
    /// </summary>
    /// <returns>Memory state data</returns>
    MemoryMapState GetState();

    /// <summary>
    /// Restore memory state from saved data
    /// </summary>
    /// <param name="state">Memory state to restore</param>
    void SetState(MemoryMapState state);
}

/// <summary>
/// Interface for CPU memory map (main system memory)
/// </summary>
public interface ICPUMemoryMap : IMemoryMap
{
    /// <summary>
    /// Internal RAM (2KB, mirrored to $0000-$1FFF)
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Memory interface requires direct array access for performance")]
    byte[] InternalRAM { get; }

    /// <summary>
    /// Read from zero page (optimized for 6502 zero page addressing)
    /// </summary>
    /// <param name="address">8-bit zero page address</param>
    /// <returns>Byte value</returns>
    byte ReadZeroPage(byte address);

    /// <summary>
    /// Write to zero page (optimized for 6502 zero page addressing)
    /// </summary>
    /// <param name="address">8-bit zero page address</param>
    /// <param name="value">Byte value to write</param>
    void WriteZeroPage(byte address, byte value);

    /// <summary>
    /// Read from stack (optimized for 6502 stack operations)
    /// </summary>
    /// <param name="stackPointer">8-bit stack pointer</param>
    /// <returns>Byte value</returns>
    byte ReadStack(byte stackPointer);

    /// <summary>
    /// Write to stack (optimized for 6502 stack operations)
    /// </summary>
    /// <param name="stackPointer">8-bit stack pointer</param>
    /// <param name="value">Byte value to write</param>
    void WriteStack(byte stackPointer, byte value);

    /// <summary>
    /// Read interrupt vector
    /// </summary>
    /// <param name="vector">Interrupt vector type</param>
    /// <returns>16-bit vector address</returns>
    ushort ReadInterruptVector(InterruptVector vector);
}

/// <summary>
/// Interface for PPU memory map (video memory)
/// </summary>
public interface IPPUMemoryMap : IMemoryMap
{
    /// <summary>
    /// Read from pattern table (CHR-ROM/CHR-RAM)
    /// </summary>
    /// <param name="address">Pattern table address ($0000-$1FFF)</param>
    /// <returns>Byte value</returns>
    byte ReadPattern(ushort address);

    /// <summary>
    /// Write to pattern table (CHR-RAM only)
    /// </summary>
    /// <param name="address">Pattern table address ($0000-$1FFF)</param>
    /// <param name="value">Byte value to write</param>
    void WritePattern(ushort address, byte value);

    /// <summary>
    /// Read from nametable
    /// </summary>
    /// <param name="address">Nametable address ($2000-$2FFF)</param>
    /// <returns>Byte value</returns>
    byte ReadNameTable(ushort address);

    /// <summary>
    /// Write to nametable
    /// </summary>
    /// <param name="address">Nametable address ($2000-$2FFF)</param>
    /// <param name="value">Byte value to write</param>
    void WriteNameTable(ushort address, byte value);

    /// <summary>
    /// Read from palette RAM
    /// </summary>
    /// <param name="address">Palette address ($3F00-$3FFF)</param>
    /// <returns>Byte value</returns>
    byte ReadPalette(ushort address);

    /// <summary>
    /// Write to palette RAM
    /// </summary>
    /// <param name="address">Palette address ($3F00-$3FFF)</param>
    /// <param name="value">Byte value to write</param>
    void WritePalette(ushort address, byte value);

    /// <summary>
    /// Current nametable mirroring mode
    /// </summary>
    MirroringMode MirroringMode { get; set; }
}

/// <summary>
/// NES interrupt vectors
/// </summary>
public enum InterruptVector
{
    /// <summary>
    /// No interrupt vector
    /// </summary>
    None = 0,

    /// <summary>
    /// Non-maskable interrupt vector ($FFFA-$FFFB)
    /// </summary>
    NMI = 0xFFFA,

    /// <summary>
    /// Reset vector ($FFFC-$FFFD)
    /// </summary>
    Reset = 0xFFFC,

    /// <summary>
    /// Interrupt request vector ($FFFE-$FFFF)
    /// </summary>
    IRQ = 0xFFFE
}

/// <summary>
/// PPU nametable mirroring modes
/// </summary>
public enum MirroringMode
{
    /// <summary>
    /// Horizontal mirroring (vertical scrolling)
    /// </summary>
    Horizontal,

    /// <summary>
    /// Vertical mirroring (horizontal scrolling)
    /// </summary>
    Vertical,

    /// <summary>
    /// Single screen mirroring (lower bank)
    /// </summary>
    SingleScreenLower,

    /// <summary>
    /// Single screen mirroring (upper bank)
    /// </summary>
    SingleScreenUpper,

    /// <summary>
    /// Four screen mirroring (no mirroring)
    /// </summary>
    FourScreen
}

/// <summary>
/// Information about a memory mapping registration
/// </summary>
public class MemoryMappingInfo
{
    /// <summary>
    /// The registered component
    /// </summary>
    public IMemoryMappedComponent Component { get; }

    /// <summary>
    /// Address ranges handled by this component
    /// </summary>
    public IReadOnlyList<MemoryRange> Ranges { get; }

    /// <summary>
    /// Priority of this mapping
    /// </summary>
    public int Priority { get; }

    public MemoryMappingInfo(IMemoryMappedComponent component, IReadOnlyList<MemoryRange> ranges, int priority)
    {
        Component = component;
        Ranges = ranges;
        Priority = priority;
    }

    public override string ToString()
    {
        var rangeStr = string.Join(", ", Ranges);
        return $"{Component.Name}: {rangeStr} (Priority: {Priority})";
    }
}

/// <summary>
/// Memory map state for save/load operations
/// </summary>
public class MemoryMapState : ComponentState
{
    /// <summary>
    /// Internal RAM state (for CPU memory map)
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "State class requires arrays for serialization")]
    public byte[]? InternalRAM { get; set; }

    /// <summary>
    /// VRAM state (for PPU memory map)
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "State class requires arrays for serialization")]
    public byte[]? VRAM { get; set; }

    /// <summary>
    /// Palette RAM state (for PPU memory map)
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "State class requires arrays for serialization")]
    public byte[]? PaletteRAM { get; set; }

    /// <summary>
    /// Current mirroring mode (for PPU memory map)
    /// </summary>
    public MirroringMode MirroringMode { get; set; }

    /// <summary>
    /// Component states for registered memory-mapped components
    /// </summary>
    public Dictionary<string, ComponentState> ComponentStates { get; } = new();

    public MemoryMapState()
    {
        ComponentName = "MemoryMap";
    }

    /// <summary>
    /// Create a deep copy of the memory map state
    /// </summary>
    public MemoryMapState Clone()
    {
        var clone = new MemoryMapState
        {
            ComponentName = ComponentName,
            Timestamp = Timestamp,
            CycleCount = CycleCount,
            MirroringMode = MirroringMode
        };

        if (InternalRAM != null)
        {
            clone.InternalRAM = new byte[InternalRAM.Length];
            Array.Copy(InternalRAM, clone.InternalRAM, InternalRAM.Length);
        }

        if (VRAM != null)
        {
            clone.VRAM = new byte[VRAM.Length];
            Array.Copy(VRAM, clone.VRAM, VRAM.Length);
        }

        if (PaletteRAM != null)
        {
            clone.PaletteRAM = new byte[PaletteRAM.Length];
            Array.Copy(PaletteRAM, clone.PaletteRAM, PaletteRAM.Length);
        }

        foreach (var kvp in ComponentStates)
        {
            clone.ComponentStates[kvp.Key] = kvp.Value; // Note: shallow copy of component states
        }

        return clone;
    }
}
