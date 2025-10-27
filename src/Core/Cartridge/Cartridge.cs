using System;
using System.Collections.Generic;
using EightBitten.Core.Cartridge.Mappers;
using EightBitten.Core.Contracts;
using EightBitten.Core.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EightBitten.Core.Cartridge;

/// <summary>
/// NES cartridge implementation that manages ROM data and mapper
/// </summary>
public sealed class GameCartridge : ICartridge
{
    private readonly ILogger _logger;
    private CartridgeHeader _header;
    private NROM? _mapper;
    private ReadOnlyMemory<byte> _prgRom;
    private ReadOnlyMemory<byte> _chrRom;
    private Memory<byte> _chrRam = new byte[0x2000]; // 8KB CHR RAM
    private Memory<byte> _prgRam = new byte[0x2000]; // 8KB PRG RAM
    private bool _isLoaded;

    /// <summary>
    /// Component name
    /// </summary>
    public string Name => "Cartridge";

    /// <summary>
    /// Whether cartridge is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Cartridge header information
    /// </summary>
    public CartridgeHeader Header => _header;

    /// <summary>
    /// Whether cartridge is loaded
    /// </summary>
    public bool IsLoaded => _isLoaded;

    /// <summary>
    /// Current mapper instance
    /// </summary>
    public IMapper Mapper => _mapper ?? throw new InvalidOperationException("No mapper loaded");

    /// <summary>
    /// PRG-ROM data
    /// </summary>
    public ReadOnlyMemory<byte> PRGROM => _prgRom;

    /// <summary>
    /// CHR-ROM data
    /// </summary>
    public ReadOnlyMemory<byte> CHRROM => _chrRom;

    /// <summary>
    /// CHR-RAM data (if using CHR-RAM)
    /// </summary>
    public Memory<byte> CHRRAM => _chrRam;

    /// <summary>
    /// PRG-RAM data (if present)
    /// </summary>
    public Memory<byte> PRGRAM => _prgRam;

    /// <summary>
    /// Whether cartridge has battery-backed RAM
    /// </summary>
    public bool HasBatteryBackedRAM => _header.HasBattery;

    /// <summary>
    /// Whether cartridge uses CHR-RAM instead of CHR-ROM
    /// </summary>
    public bool UsesCHRRAM => _header.CHRROMBanks == 0;

    /// <summary>
    /// Current mirroring mode
    /// </summary>
    public MirroringMode MirroringMode => _header.Mirroring;

    /// <summary>
    /// Memory address ranges this cartridge responds to
    /// </summary>
    public IReadOnlyList<MemoryRange> AddressRanges => new[]
    {
        new MemoryRange(0x8000, 0xFFFF), // CPU address space
        new MemoryRange(0x0000, 0x1FFF)  // PPU address space
    };

    /// <summary>
    /// Event fired when mirroring mode changes
    /// </summary>
#pragma warning disable CS0067 // Event is never used
    public event EventHandler<MirroringChangedEventArgs>? MirroringChanged;
#pragma warning restore CS0067

    /// <summary>
    /// Event fired when mapper generates an IRQ
    /// </summary>
#pragma warning disable CS0067 // Event is never used
    public event EventHandler<MapperIRQEventArgs>? MapperIRQ;
#pragma warning restore CS0067

    public GameCartridge(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _header = new CartridgeHeader();
    }

    /// <summary>
    /// Initialize cartridge component
    /// </summary>
    public void Initialize()
    {
        if (_isLoaded && _mapper != null)
        {
            _mapper.Initialize();
        }
    }

    /// <summary>
    /// Reset cartridge to initial state
    /// </summary>
    public void Reset()
    {
        _mapper?.Reset();

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogDebug("Cartridge reset");
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Load ROM from data (ICartridge interface)
    /// </summary>
    /// <param name="romData">ROM data</param>
    /// <returns>True if loaded successfully</returns>
    public bool LoadFromData(ReadOnlySpan<byte> romData)
    {
        try
        {
            // Use the comprehensive ROM validation first
            var validationResult = ROMValidator.ValidateROM(romData.ToArray());
            if (!validationResult.IsValid)
            {
                #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                _logger.LogWarning("ROM validation failed: {Error}", validationResult.ErrorMessage);
                #pragma warning restore CA1848
                return false;
            }

            // Parse iNES header properly
            var header = ParseINESHeader(romData);

            // Calculate data offsets
            var headerSize = 16;
            var trainerSize = header.HasTrainer ? 512 : 0;
            var prgRomStart = headerSize + trainerSize;
            var prgRomSize = header.PRGROMBanks * 16384; // 16KB per bank
            var chrRomStart = prgRomStart + prgRomSize;
            var chrRomSize = header.CHRROMBanks * 8192; // 8KB per bank

            // Extract ROM data
            var prgData = romData.Slice(prgRomStart, prgRomSize).ToArray();
            var chrData = chrRomSize > 0 ? romData.Slice(chrRomStart, chrRomSize).ToArray() : Array.Empty<byte>();

            LoadROM(header, prgData, chrData);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NotSupportedException)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogError(ex, "Failed to load ROM data");
            #pragma warning restore CA1848
            return false;
        }
    }

    /// <summary>
    /// Get save RAM data
    /// </summary>
    /// <returns>Save RAM data</returns>
    public ReadOnlySpan<byte> GetSaveRAM()
    {
        return _prgRam.Span;
    }

    /// <summary>
    /// Load save RAM data
    /// </summary>
    /// <param name="saveData">Save RAM data</param>
    /// <returns>True if loaded successfully</returns>
    public bool LoadSaveRAM(ReadOnlySpan<byte> saveData)
    {
        if (_prgRam.Length >= saveData.Length)
        {
            saveData.CopyTo(_prgRam.Span);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Read from PRG space
    /// </summary>
    /// <param name="address">Address</param>
    /// <returns>Byte value</returns>
    public byte ReadPRG(ushort address)
    {
        if (_mapper == null) return 0x00;

        var mappedAddress = _mapper.MapPRGAddress(address);
        if (mappedAddress.HasValue && mappedAddress.Value < _prgRom.Length)
        {
            return _prgRom.Span[mappedAddress.Value];
        }
        return 0x00;
    }

    /// <summary>
    /// Write to PRG space
    /// </summary>
    /// <param name="address">Address</param>
    /// <param name="value">Byte value</param>
    public void WritePRG(ushort address, byte value)
    {
        _mapper?.WritePRG(address, value);
    }

    /// <summary>
    /// Read from CHR space
    /// </summary>
    /// <param name="address">Address</param>
    /// <returns>Byte value</returns>
    public byte ReadCHR(ushort address)
    {
        if (_mapper == null) return 0x00;

        var mappedAddress = _mapper.MapCHRAddress(address);
        if (mappedAddress.HasValue)
        {
            if (_header.CHRROMBanks > 0 && mappedAddress.Value < _chrRom.Length)
            {
                return _chrRom.Span[mappedAddress.Value];
            }
            else if (_chrRam.Length > 0 && mappedAddress.Value < _chrRam.Length)
            {
                return _chrRam.Span[mappedAddress.Value];
            }
        }
        return 0x00;
    }

    /// <summary>
    /// Write to CHR space
    /// </summary>
    /// <param name="address">Address</param>
    /// <param name="value">Byte value</param>
    public void WriteCHR(ushort address, byte value)
    {
        if (_mapper == null) return;

        var mappedAddress = _mapper.MapCHRAddress(address);
        if (mappedAddress.HasValue && _chrRam.Length > 0 && mappedAddress.Value < _chrRam.Length)
        {
            _chrRam.Span[mappedAddress.Value] = value;
        }
    }

    /// <summary>
    /// Load ROM data into cartridge
    /// </summary>
    /// <param name="header">Cartridge header</param>
    /// <param name="prgRom">PRG-ROM data</param>
    /// <param name="chrRom">CHR-ROM data</param>
    public void LoadROM(CartridgeHeader header, ReadOnlyMemory<byte> prgRom, ReadOnlyMemory<byte> chrRom)
    {
        ArgumentNullException.ThrowIfNull(header);

        try
        {
            _header = header;
            _prgRom = prgRom;
            _chrRom = chrRom;

            // Create appropriate mapper
            _mapper = CreateMapper(header.MapperNumber);
            
            // Initialize mapper with ROM data
            _mapper.Initialize(header, prgRom, chrRom);

            _isLoaded = true;

            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogInformation("Cartridge loaded: Mapper {Mapper} ({MapperName}), PRG: {PRGSize}KB, CHR: {CHRSize}KB",
                header.MapperNumber, _mapper.MapperName, prgRom.Length / 1024, chrRom.Length / 1024);
            #pragma warning restore CA1848
        }
        catch (Exception ex)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogError(ex, "Failed to load ROM into cartridge");
            #pragma warning restore CA1848
            _isLoaded = false;
            throw;
        }
    }

    /// <summary>
    /// Unload ROM from cartridge
    /// </summary>
    public void UnloadROM()
    {
        _mapper?.Dispose();
        _mapper = null;
        _prgRom = ReadOnlyMemory<byte>.Empty;
        _chrRom = ReadOnlyMemory<byte>.Empty;
        _isLoaded = false;

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogInformation("Cartridge unloaded");
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Read from cartridge memory
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <returns>Byte value</returns>
    public byte ReadMemory(ushort address)
    {
        if (_mapper == null)
            return 0x00;

        // Use the appropriate read method based on address range
        if (address >= 0x8000) // PRG space
        {
            return ReadPRG(address);
        }
        else // CHR space
        {
            return ReadCHR(address);
        }
    }

    /// <summary>
    /// Write to cartridge memory
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <param name="value">Byte value</param>
    public void WriteMemory(ushort address, byte value)
    {
        if (_mapper == null) return;

        // Use the appropriate write method based on address range
        if (address >= 0x8000) // PRG space
        {
            WritePRG(address, value);
        }
        else // CHR space
        {
            WriteCHR(address, value);
        }
    }

    /// <summary>
    /// Read byte from cartridge (IMemoryMappedComponent interface)
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <returns>Byte value</returns>
    public byte ReadByte(ushort address) => ReadMemory(address);

    /// <summary>
    /// Write byte to cartridge (IMemoryMappedComponent interface)
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <param name="value">Byte value</param>
    public void WriteByte(ushort address, byte value) => WriteMemory(address, value);

    /// <summary>
    /// Check if cartridge handles the specified address
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <returns>True if cartridge handles this address</returns>
    public bool HandlesAddress(ushort address)
    {
        if (_mapper == null) return false;

        foreach (var range in AddressRanges)
        {
            if (range.Contains(address))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Execute one cycle (IComponent interface)
    /// </summary>
    /// <returns>Number of cycles consumed</returns>
    public int ExecuteCycle()
    {
        return _mapper?.ExecuteCycle() ?? 1;
    }

    /// <summary>
    /// Get cartridge state for save/load
    /// </summary>
    /// <returns>Cartridge state</returns>
    public CartridgeState GetState()
    {
        var state = new CartridgeState
        {
            ComponentName = Name,
            Header = _header,
            MapperState = _mapper?.GetState()
        };

        return state;
    }

    /// <summary>
    /// Set cartridge state from save data
    /// </summary>
    /// <param name="state">Cartridge state</param>
    public void SetState(CartridgeState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.MapperState != null)
        {
            _header = state.Header;
            _isLoaded = true; // State exists, so cartridge was loaded

            // Recreate mapper if needed
            if (_mapper == null || _mapper.MapperNumber != _header.MapperNumber)
            {
                _mapper?.Dispose();
                _mapper = CreateMapper(_header.MapperNumber);
            }

            // Restore mapper state
            _mapper.SetState(state.MapperState);
        }
        else
        {
            UnloadROM();
        }

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogDebug("Cartridge state restored");
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Get component state for save/load (IComponent interface)
    /// </summary>
    /// <returns>Component state</returns>
    ComponentState IComponent.GetState() => GetState();

    /// <summary>
    /// Set component state from save data (IComponent interface)
    /// </summary>
    /// <param name="state">Component state</param>
    void IComponent.SetState(ComponentState state)
    {
        if (state is CartridgeState cartridgeState)
        {
            SetState(cartridgeState);
        }
    }

    /// <summary>
    /// Dispose cartridge resources
    /// </summary>
    public void Dispose()
    {
        UnloadROM();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Parse iNES header from ROM data
    /// </summary>
    /// <param name="romData">ROM data</param>
    /// <returns>Parsed cartridge header</returns>
    private static CartridgeHeader ParseINESHeader(ReadOnlySpan<byte> romData)
    {
        var header = new CartridgeHeader();

        // Parse iNES header (16 bytes)
        header.Signature = (uint)(romData[0] | (romData[1] << 8) | (romData[2] << 16) | (romData[3] << 24));
        header.PRGROMBanks = romData[4];
        header.CHRROMBanks = romData[5];
        header.MapperLow = romData[6];
        header.MapperHigh = romData[7];

        // Parse flags
        header.Mirroring = (romData[6] & 0x01) == 0 ? MirroringMode.Horizontal : MirroringMode.Vertical;
        header.HasBattery = (romData[6] & 0x02) != 0;
        header.HasTrainer = (romData[6] & 0x04) != 0;
        header.FourScreenMirroring = (romData[6] & 0x08) != 0;

        // Determine iNES version
        if ((romData[7] & 0x0C) == 0x08)
        {
            header.Version = INESVersion.Version2;
        }
        else
        {
            header.Version = INESVersion.Version1;
        }

        // Parse additional fields for iNES 1.0
        if (header.Version == INESVersion.Version1)
        {
            header.PRGRAMBanks = romData[8] == 0 ? (byte)1 : romData[8]; // Default to 1 if 0
            header.TVSystem = (romData[9] & 0x01) == 0 ? TVSystem.NTSC : TVSystem.PAL;
        }

        return header;
    }

    /// <summary>
    /// Create mapper instance for the specified mapper number
    /// </summary>
    /// <param name="mapperNumber">Mapper number</param>
    /// <returns>Mapper instance</returns>
    private NROM CreateMapper(int mapperNumber)
    {
        return mapperNumber switch
        {
            0 => new NROM(_logger),
            _ => throw new NotSupportedException($"Mapper {mapperNumber} is not supported. Only NROM (Mapper 0) is currently implemented.")
        };
    }
}



/// <summary>
/// Cartridge factory for dependency injection
/// </summary>
public class CartridgeFactory
{
    private readonly ILogger<GameCartridge> _logger;

    public CartridgeFactory(ILogger<GameCartridge> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create cartridge instance
    /// </summary>
    /// <returns>Cartridge instance</returns>
    public GameCartridge CreateCartridge()
    {
        return new GameCartridge(_logger);
    }
}

/// <summary>
/// Cartridge registration extensions
/// </summary>
public static class CartridgeExtensions
{
    /// <summary>
    /// Register cartridge services with dependency injection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddCartridge(
        this Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {
        services.AddTransient<CartridgeFactory>();
        services.AddTransient<GameCartridge>();
        return services;
    }
}


