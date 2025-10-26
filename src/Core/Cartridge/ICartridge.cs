using System;
using EightBitten.Core.Contracts;
using EightBitten.Core.Memory;

namespace EightBitten.Core.Cartridge;

/// <summary>
/// Interface for NES cartridge (ROM) management and mapper functionality
/// </summary>
public interface ICartridge : IMemoryMappedComponent
{
    /// <summary>
    /// Cartridge header information
    /// </summary>
    CartridgeHeader Header { get; }

    /// <summary>
    /// Mapper implementation for this cartridge
    /// </summary>
    IMapper Mapper { get; }

    /// <summary>
    /// PRG-ROM data (program code)
    /// </summary>
    ReadOnlyMemory<byte> PRGROM { get; }

    /// <summary>
    /// CHR-ROM data (character/pattern data), if present
    /// </summary>
    ReadOnlyMemory<byte> CHRROM { get; }

    /// <summary>
    /// CHR-RAM data (character/pattern RAM), if present
    /// </summary>
    Memory<byte> CHRRAM { get; }

    /// <summary>
    /// PRG-RAM data (save RAM), if present
    /// </summary>
    Memory<byte> PRGRAM { get; }

    /// <summary>
    /// Whether this cartridge has battery-backed save RAM
    /// </summary>
    bool HasBatteryBackedRAM { get; }

    /// <summary>
    /// Whether this cartridge uses CHR-RAM instead of CHR-ROM
    /// </summary>
    bool UsesCHRRAM { get; }

    /// <summary>
    /// Current nametable mirroring mode
    /// </summary>
    MirroringMode MirroringMode { get; }

    /// <summary>
    /// Load cartridge from ROM file data
    /// </summary>
    /// <param name="romData">ROM file data</param>
    /// <returns>True if loaded successfully</returns>
    bool LoadFromData(ReadOnlySpan<byte> romData);

    /// <summary>
    /// Get save RAM data for persistence
    /// </summary>
    /// <returns>Save RAM data, or empty if no save RAM</returns>
    ReadOnlySpan<byte> GetSaveRAM();

    /// <summary>
    /// Load save RAM data from persistence
    /// </summary>
    /// <param name="saveData">Save RAM data to load</param>
    /// <returns>True if loaded successfully</returns>
    bool LoadSaveRAM(ReadOnlySpan<byte> saveData);

    /// <summary>
    /// Read from PRG address space ($8000-$FFFF)
    /// </summary>
    /// <param name="address">CPU address</param>
    /// <returns>Byte value</returns>
    byte ReadPRG(ushort address);

    /// <summary>
    /// Write to PRG address space ($8000-$FFFF)
    /// </summary>
    /// <param name="address">CPU address</param>
    /// <param name="value">Byte value to write</param>
    void WritePRG(ushort address, byte value);

    /// <summary>
    /// Read from CHR address space ($0000-$1FFF)
    /// </summary>
    /// <param name="address">PPU address</param>
    /// <returns>Byte value</returns>
    byte ReadCHR(ushort address);

    /// <summary>
    /// Write to CHR address space ($0000-$1FFF)
    /// </summary>
    /// <param name="address">PPU address</param>
    /// <param name="value">Byte value to write</param>
    void WriteCHR(ushort address, byte value);

    /// <summary>
    /// Event fired when mirroring mode changes
    /// </summary>
    event EventHandler<MirroringChangedEventArgs>? MirroringChanged;

    /// <summary>
    /// Event fired when mapper generates an IRQ
    /// </summary>
    event EventHandler<MapperIRQEventArgs>? MapperIRQ;
}

/// <summary>
/// NES cartridge header information (iNES format)
/// </summary>
public class CartridgeHeader
{
    /// <summary>
    /// iNES header signature ("NES" + $1A)
    /// </summary>
    public uint Signature { get; set; }

    /// <summary>
    /// Number of 16KB PRG-ROM banks
    /// </summary>
    public byte PRGROMBanks { get; set; }

    /// <summary>
    /// Number of 8KB CHR-ROM banks (0 = CHR-RAM)
    /// </summary>
    public byte CHRROMBanks { get; set; }

    /// <summary>
    /// Mapper number (lower 4 bits)
    /// </summary>
    public byte MapperLow { get; set; }

    /// <summary>
    /// Mapper number (upper 4 bits) and flags
    /// </summary>
    public byte MapperHigh { get; set; }

    /// <summary>
    /// Complete mapper number
    /// </summary>
    public int MapperNumber => (MapperHigh & 0xF0) | (MapperLow >> 4);

    /// <summary>
    /// Nametable mirroring mode
    /// </summary>
    public MirroringMode Mirroring { get; set; }

    /// <summary>
    /// Whether cartridge has battery-backed PRG-RAM
    /// </summary>
    public bool HasBattery { get; set; }

    /// <summary>
    /// Whether cartridge has trainer data
    /// </summary>
    public bool HasTrainer { get; set; }

    /// <summary>
    /// Whether cartridge uses four-screen mirroring
    /// </summary>
    public bool FourScreenMirroring { get; set; }

    /// <summary>
    /// iNES format version (1.0 or 2.0)
    /// </summary>
    public INESVersion Version { get; set; }

    /// <summary>
    /// Number of 8KB PRG-RAM banks
    /// </summary>
    public byte PRGRAMBanks { get; set; }

    /// <summary>
    /// TV system (NTSC/PAL)
    /// </summary>
    public TVSystem TVSystem { get; set; }

    /// <summary>
    /// Calculate total PRG-ROM size in bytes
    /// </summary>
    public int PRGROMSize => PRGROMBanks * 16384; // 16KB banks

    /// <summary>
    /// Calculate total CHR-ROM size in bytes
    /// </summary>
    public int CHRROMSize => CHRROMBanks * 8192; // 8KB banks

    /// <summary>
    /// Calculate total PRG-RAM size in bytes
    /// </summary>
    public int PRGRAMSize => PRGRAMBanks > 0 ? PRGRAMBanks * 8192 : 8192; // Default 8KB if not specified

    /// <summary>
    /// Whether this cartridge uses CHR-RAM
    /// </summary>
    public bool UsesCHRRAM => CHRROMBanks == 0;

    public override string ToString()
    {
        return $"Mapper {MapperNumber:D3}, PRG: {PRGROMBanks}x16KB, CHR: {(UsesCHRRAM ? "RAM" : $"{CHRROMBanks}x8KB")}, " +
               $"Mirror: {Mirroring}, Battery: {HasBattery}";
    }
}

/// <summary>
/// iNES format version
/// </summary>
public enum INESVersion
{
    /// <summary>
    /// iNES 1.0 format
    /// </summary>
    Version1,

    /// <summary>
    /// iNES 2.0 format (NES 2.0)
    /// </summary>
    Version2
}

/// <summary>
/// TV system type
/// </summary>
public enum TVSystem
{
    /// <summary>
    /// NTSC (North America, Japan)
    /// </summary>
    NTSC,

    /// <summary>
    /// PAL (Europe, Australia)
    /// </summary>
    PAL,

    /// <summary>
    /// Dual compatible
    /// </summary>
    Dual
}

/// <summary>
/// Event arguments for mirroring mode changes
/// </summary>
public class MirroringChangedEventArgs : EventArgs
{
    /// <summary>
    /// Previous mirroring mode
    /// </summary>
    public MirroringMode PreviousMode { get; }

    /// <summary>
    /// New mirroring mode
    /// </summary>
    public MirroringMode NewMode { get; }

    public MirroringChangedEventArgs(MirroringMode previousMode, MirroringMode newMode)
    {
        PreviousMode = previousMode;
        NewMode = newMode;
    }
}

/// <summary>
/// Event arguments for mapper IRQ events
/// </summary>
public class MapperIRQEventArgs : EventArgs
{
    /// <summary>
    /// Whether IRQ is being asserted (true) or cleared (false)
    /// </summary>
    public bool IRQAsserted { get; }

    /// <summary>
    /// Source of the IRQ (for debugging)
    /// </summary>
    public string Source { get; }

    public MapperIRQEventArgs(bool irqAsserted, string source)
    {
        IRQAsserted = irqAsserted;
        Source = source;
    }
}

/// <summary>
/// Cartridge state for save/load operations
/// </summary>
public class CartridgeState : ComponentState
{
    /// <summary>
    /// Cartridge header information
    /// </summary>
    public CartridgeHeader Header { get; set; } = new();

    /// <summary>
    /// PRG-RAM data (if present)
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "State class requires arrays for serialization")]
    public byte[]? PRGRAM { get; set; }

    /// <summary>
    /// CHR-RAM data (if present)
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "State class requires arrays for serialization")]
    public byte[]? CHRRAM { get; set; }

    /// <summary>
    /// Mapper state
    /// </summary>
    public ComponentState? MapperState { get; set; }

    /// <summary>
    /// Current mirroring mode
    /// </summary>
    public MirroringMode MirroringMode { get; set; }

    public CartridgeState()
    {
        ComponentName = "Cartridge";
    }

    /// <summary>
    /// Create a deep copy of the cartridge state
    /// </summary>
    public CartridgeState Clone()
    {
        var clone = new CartridgeState
        {
            ComponentName = ComponentName,
            Timestamp = Timestamp,
            CycleCount = CycleCount,
            Header = Header, // Header is immutable after loading
            MirroringMode = MirroringMode,
            MapperState = MapperState // Note: shallow copy, mapper should handle deep copy
        };

        if (PRGRAM != null)
        {
            clone.PRGRAM = new byte[PRGRAM.Length];
            Array.Copy(PRGRAM, clone.PRGRAM, PRGRAM.Length);
        }

        if (CHRRAM != null)
        {
            clone.CHRRAM = new byte[CHRRAM.Length];
            Array.Copy(CHRRAM, clone.CHRRAM, CHRRAM.Length);
        }

        return clone;
    }
}
