using System;
using EightBitten.Core.Contracts;
using EightBitten.Core.Memory;

namespace EightBitten.Core.Cartridge;

/// <summary>
/// Interface for NES cartridge mappers that handle bank switching and memory mapping
/// </summary>
public interface IMapper : IComponent
{
    /// <summary>
    /// Mapper number (0-255 for standard mappers)
    /// </summary>
    int MapperNumber { get; }

    /// <summary>
    /// Mapper name for identification
    /// </summary>
    string MapperName { get; }

    /// <summary>
    /// Number of PRG-ROM banks (16KB each)
    /// </summary>
    int PRGBanks { get; }

    /// <summary>
    /// Number of CHR-ROM banks (8KB each)
    /// </summary>
    int CHRBanks { get; }

    /// <summary>
    /// Whether this mapper supports PRG-RAM
    /// </summary>
    bool SupportsPRGRAM { get; }

    /// <summary>
    /// Whether this mapper supports CHR-RAM
    /// </summary>
    bool SupportsCHRRAM { get; }

    /// <summary>
    /// Current nametable mirroring mode
    /// </summary>
    MirroringMode MirroringMode { get; }

    /// <summary>
    /// Whether mapper can generate IRQs
    /// </summary>
    bool SupportsIRQ { get; }

    /// <summary>
    /// Initialize mapper with cartridge data
    /// </summary>
    /// <param name="header">Cartridge header information</param>
    /// <param name="prgRom">PRG-ROM data</param>
    /// <param name="chrRom">CHR-ROM data (may be empty for CHR-RAM)</param>
    void Initialize(CartridgeHeader header, ReadOnlyMemory<byte> prgRom, ReadOnlyMemory<byte> chrRom);

    /// <summary>
    /// Map CPU address to PRG-ROM/RAM address
    /// </summary>
    /// <param name="address">CPU address ($8000-$FFFF)</param>
    /// <returns>Mapped address in PRG space, or null if not mapped</returns>
    int? MapPRGAddress(ushort address);

    /// <summary>
    /// Map PPU address to CHR-ROM/RAM address
    /// </summary>
    /// <param name="address">PPU address ($0000-$1FFF)</param>
    /// <returns>Mapped address in CHR space, or null if not mapped</returns>
    int? MapCHRAddress(ushort address);

    /// <summary>
    /// Handle CPU write to mapper registers
    /// </summary>
    /// <param name="address">CPU address</param>
    /// <param name="value">Value written</param>
    void WritePRG(ushort address, byte value);

    /// <summary>
    /// Handle PPU write to CHR space (for CHR-RAM)
    /// </summary>
    /// <param name="address">PPU address</param>
    /// <param name="value">Value written</param>
    void WriteCHR(ushort address, byte value);

    /// <summary>
    /// Get current IRQ state
    /// </summary>
    /// <returns>True if IRQ is asserted</returns>
    bool GetIRQState();

    /// <summary>
    /// Clear IRQ state
    /// </summary>
    void ClearIRQ();

    /// <summary>
    /// Event fired when mirroring mode changes
    /// </summary>
    event EventHandler<MirroringChangedEventArgs>? MirroringChanged;

    /// <summary>
    /// Event fired when IRQ state changes
    /// </summary>
    event EventHandler<MapperIRQEventArgs>? IRQStateChanged;
}

/// <summary>
/// Base class for mapper implementations providing common functionality
/// </summary>
public abstract class MapperBase : IMapper
{
    private CartridgeHeader _header = new();
    private ReadOnlyMemory<byte> _prgRom;
    private ReadOnlyMemory<byte> _chrRom;
    private Memory<byte> _chrRam;
    private Memory<byte> _prgRam;
    private MirroringMode _mirroringMode;
    private bool _irqAsserted;

    /// <summary>
    /// Mapper number
    /// </summary>
    public abstract int MapperNumber { get; }

    /// <summary>
    /// Mapper name
    /// </summary>
    public abstract string MapperName { get; }

    /// <summary>
    /// Component name for logging
    /// </summary>
    public virtual string Name => $"Mapper{MapperNumber:D3}";

    /// <summary>
    /// Whether mapper is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Number of PRG-ROM banks
    /// </summary>
    public int PRGBanks => _header.PRGROMBanks;

    /// <summary>
    /// Number of CHR-ROM banks
    /// </summary>
    public int CHRBanks => _header.CHRROMBanks;

    /// <summary>
    /// Whether mapper supports PRG-RAM
    /// </summary>
    public virtual bool SupportsPRGRAM => _header.PRGRAMBanks > 0;

    /// <summary>
    /// Whether mapper supports CHR-RAM
    /// </summary>
    public virtual bool SupportsCHRRAM => _header.UsesCHRRAM;

    /// <summary>
    /// Current mirroring mode
    /// </summary>
    public MirroringMode MirroringMode => _mirroringMode;

    /// <summary>
    /// Whether mapper supports IRQ generation
    /// </summary>
    public virtual bool SupportsIRQ => false;

    /// <summary>
    /// Protected access to cartridge header
    /// </summary>
    protected CartridgeHeader Header => _header;

    /// <summary>
    /// Protected access to PRG-ROM data
    /// </summary>
    protected ReadOnlyMemory<byte> PRGROMData => _prgRom;

    /// <summary>
    /// Protected access to CHR-ROM data
    /// </summary>
    protected ReadOnlyMemory<byte> CHRROMData => _chrRom;

    /// <summary>
    /// Protected access to CHR-RAM data
    /// </summary>
    protected Memory<byte> CHRRAMData => _chrRam;

    /// <summary>
    /// Protected access to PRG-RAM data
    /// </summary>
    protected Memory<byte> PRGRAMData => _prgRam;

    /// <summary>
    /// Event fired when mirroring mode changes
    /// </summary>
    public event EventHandler<MirroringChangedEventArgs>? MirroringChanged;

    /// <summary>
    /// Event fired when IRQ state changes
    /// </summary>
    public event EventHandler<MapperIRQEventArgs>? IRQStateChanged;

    /// <summary>
    /// Initialize mapper with cartridge data
    /// </summary>
    public virtual void Initialize(CartridgeHeader header, ReadOnlyMemory<byte> prgRom, ReadOnlyMemory<byte> chrRom)
    {
        ArgumentNullException.ThrowIfNull(header);

        _header = header;
        _prgRom = prgRom;
        _chrRom = chrRom;
        _mirroringMode = header.Mirroring;

        // Initialize CHR-RAM if needed
        if (SupportsCHRRAM)
        {
            _chrRam = new byte[8192]; // 8KB CHR-RAM
        }

        // Initialize PRG-RAM if needed
        if (SupportsPRGRAM)
        {
            _prgRam = new byte[header.PRGRAMSize];
        }

        OnInitialize();
    }

    /// <summary>
    /// Override in derived classes for mapper-specific initialization
    /// </summary>
    protected virtual void OnInitialize() { }

    /// <summary>
    /// Initialize component
    /// </summary>
    public virtual void Initialize() => OnInitialize();

    /// <summary>
    /// Reset mapper to initial state
    /// </summary>
    public virtual void Reset()
    {
        _irqAsserted = false;
        _mirroringMode = _header.Mirroring;
        OnReset();
    }

    /// <summary>
    /// Override in derived classes for mapper-specific reset
    /// </summary>
    protected virtual void OnReset() { }

    /// <summary>
    /// Execute one cycle (for mappers with timing-sensitive features)
    /// </summary>
    public virtual int ExecuteCycle() => 1;

    /// <summary>
    /// Get mapper state for save/load
    /// </summary>
    public virtual ComponentState GetState()
    {
        return new MapperState
        {
            ComponentName = Name,
            MapperNumber = MapperNumber,
            MirroringMode = _mirroringMode,
            IRQAsserted = _irqAsserted,
            PRGRAMData = SupportsPRGRAM ? _prgRam.ToArray() : null,
            CHRRAMData = SupportsCHRRAM ? _chrRam.ToArray() : null
        };
    }

    /// <summary>
    /// Set mapper state from save data
    /// </summary>
    public virtual void SetState(ComponentState state)
    {
        if (state is MapperState mapperState)
        {
            _mirroringMode = mapperState.MirroringMode;
            _irqAsserted = mapperState.IRQAsserted;

            if (mapperState.PRGRAMData != null && SupportsPRGRAM)
            {
                mapperState.PRGRAMData.CopyTo(_prgRam.Span);
            }

            if (mapperState.CHRRAMData != null && SupportsCHRRAM)
            {
                mapperState.CHRRAMData.CopyTo(_chrRam.Span);
            }

            OnStateRestored(mapperState);
        }
    }

    /// <summary>
    /// Override in derived classes for mapper-specific state restoration
    /// </summary>
    protected virtual void OnStateRestored(MapperState state) { }

    /// <summary>
    /// Map CPU address to PRG space
    /// </summary>
    public abstract int? MapPRGAddress(ushort address);

    /// <summary>
    /// Map PPU address to CHR space
    /// </summary>
    public abstract int? MapCHRAddress(ushort address);

    /// <summary>
    /// Handle PRG write
    /// </summary>
    public abstract void WritePRG(ushort address, byte value);

    /// <summary>
    /// Handle CHR write
    /// </summary>
    public virtual void WriteCHR(ushort address, byte value)
    {
        if (SupportsCHRRAM)
        {
            var mappedAddress = MapCHRAddress(address);
            if (mappedAddress.HasValue && mappedAddress.Value < _chrRam.Length)
            {
                _chrRam.Span[mappedAddress.Value] = value;
            }
        }
    }

    /// <summary>
    /// Get IRQ state
    /// </summary>
    public virtual bool GetIRQState() => _irqAsserted;

    /// <summary>
    /// Clear IRQ
    /// </summary>
    public virtual void ClearIRQ()
    {
        if (_irqAsserted)
        {
            _irqAsserted = false;
            IRQStateChanged?.Invoke(this, new MapperIRQEventArgs(false, Name));
        }
    }

    /// <summary>
    /// Set IRQ state
    /// </summary>
    protected virtual void SetIRQ(bool asserted)
    {
        if (_irqAsserted != asserted)
        {
            _irqAsserted = asserted;
            IRQStateChanged?.Invoke(this, new MapperIRQEventArgs(asserted, Name));
        }
    }

    /// <summary>
    /// Set mirroring mode
    /// </summary>
    protected virtual void SetMirroring(MirroringMode mode)
    {
        if (_mirroringMode != mode)
        {
            var previousMode = _mirroringMode;
            _mirroringMode = mode;
            MirroringChanged?.Invoke(this, new MirroringChangedEventArgs(previousMode, mode));
        }
    }

    /// <summary>
    /// Dispose mapper resources
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose pattern implementation
    /// </summary>
    /// <param name="disposing">True if disposing managed resources</param>
    protected virtual void Dispose(bool disposing)
    {
        // Override in derived classes if needed
    }
}

/// <summary>
/// Mapper state for save/load operations
/// </summary>
public class MapperState : ComponentState
{
    /// <summary>
    /// Mapper number
    /// </summary>
    public int MapperNumber { get; set; }

    /// <summary>
    /// Current mirroring mode
    /// </summary>
    public MirroringMode MirroringMode { get; set; }

    /// <summary>
    /// IRQ asserted state
    /// </summary>
    public bool IRQAsserted { get; set; }

    /// <summary>
    /// PRG-RAM data (if present)
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "State class requires arrays for serialization")]
    public byte[]? PRGRAMData { get; set; }

    /// <summary>
    /// CHR-RAM data (if present)
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "State class requires arrays for serialization")]
    public byte[]? CHRRAMData { get; set; }

    public MapperState()
    {
        ComponentName = "Mapper";
    }
}
