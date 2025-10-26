using System;
using EightBitten.Core.Contracts;
using EightBitten.Core.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EightBitten.Core.Cartridge.Mappers;

/// <summary>
/// NROM mapper (Mapper 0) - the simplest NES mapper
/// Supports 16KB or 32KB PRG-ROM and 8KB CHR-ROM/RAM
/// No bank switching - direct memory mapping
/// </summary>
public class NROM : MapperBase
{
    private readonly ILogger _logger;

    /// <summary>
    /// Mapper number (0 for NROM)
    /// </summary>
    public override int MapperNumber => 0;

    /// <summary>
    /// Mapper name
    /// </summary>
    public override string MapperName => "NROM";

    /// <summary>
    /// Memory address ranges this mapper responds to
    /// </summary>
    public static IReadOnlyList<MemoryRange> AddressRanges => new[]
    {
        new MemoryRange(0x8000, 0xFFFF), // PRG-ROM space
        new MemoryRange(0x0000, 0x1FFF)  // CHR-ROM/RAM space (PPU)
    };

    public NROM(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initialize mapper with cartridge data
    /// </summary>
    public override void Initialize(CartridgeHeader header, ReadOnlyMemory<byte> prgRom, ReadOnlyMemory<byte> chrRom)
    {
        ArgumentNullException.ThrowIfNull(header);
        base.Initialize(header, prgRom, chrRom);

        // Validate NROM constraints
        if (header.PRGROMBanks < 1 || header.PRGROMBanks > 2)
        {
            throw new InvalidOperationException($"NROM supports 1-2 PRG-ROM banks, got {header.PRGROMBanks}");
        }

        if (header.CHRROMBanks > 1)
        {
            throw new InvalidOperationException($"NROM supports 0-1 CHR-ROM banks, got {header.CHRROMBanks}");
        }

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogInformation("NROM mapper initialized: {PRGBanks} PRG banks ({PRGSize}KB), {CHRBanks} CHR banks ({CHRSize}KB), mirroring: {Mirroring}",
            header.PRGROMBanks, header.PRGROMBanks * 16,
            header.CHRROMBanks, header.CHRROMBanks * 8,
            header.Mirroring);
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Map CPU address to PRG-ROM address
    /// </summary>
    /// <param name="address">CPU address ($8000-$FFFF)</param>
    /// <returns>Mapped address in PRG-ROM, or null if not mapped</returns>
    public override int? MapPRGAddress(ushort address)
    {
        // NROM only responds to $8000-$FFFF
        if (address < 0x8000)
            return null;

        // Calculate offset within PRG-ROM space
        var offset = address - 0x8000;

        // Handle 16KB vs 32KB PRG-ROM
        if (Header.PRGROMBanks == 1)
        {
            // 16KB PRG-ROM: mirror $8000-$BFFF to $C000-$FFFF
            return offset & 0x3FFF; // 16KB mask
        }
        else
        {
            // 32KB PRG-ROM: direct mapping
            return offset & 0x7FFF; // 32KB mask
        }
    }

    /// <summary>
    /// Map PPU address to CHR-ROM/RAM address
    /// </summary>
    /// <param name="address">PPU address ($0000-$1FFF)</param>
    /// <returns>Mapped address in CHR space, or null if not mapped</returns>
    public override int? MapCHRAddress(ushort address)
    {
        // NROM only responds to $0000-$1FFF (pattern tables)
        if (address >= 0x2000)
            return null;

        // Direct mapping for CHR-ROM/RAM
        return address & 0x1FFF; // 8KB mask
    }

    /// <summary>
    /// Handle CPU write to mapper registers
    /// NROM has no registers - all writes are ignored
    /// </summary>
    /// <param name="address">CPU address</param>
    /// <param name="value">Value written</param>
    public override void WritePRG(ushort address, byte value)
    {
        // NROM has no writable registers - ignore all writes
        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogTrace("NROM: Ignored write to ${Address:X4} = ${Value:X2}", address, value);
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Handle PPU write to CHR space
    /// </summary>
    /// <param name="address">PPU address</param>
    /// <param name="value">Value written</param>
    public override void WriteCHR(ushort address, byte value)
    {
        // Only allow writes if using CHR-RAM
        if (SupportsCHRRAM)
        {
            base.WriteCHR(address, value);
        }
        else
        {
            // CHR-ROM is read-only - ignore writes
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogTrace("NROM: Ignored CHR-ROM write to ${Address:X4} = ${Value:X2}", address, value);
            #pragma warning restore CA1848
        }
    }

    /// <summary>
    /// Read from memory-mapped component
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <returns>Byte value</returns>
    public byte ReadMemory(ushort address)
    {
        // Handle PRG-ROM reads
        if (address >= 0x8000)
        {
            var prgAddress = MapPRGAddress(address);
            if (prgAddress.HasValue && prgAddress.Value < PRGROMData.Length)
            {
                return PRGROMData.Span[prgAddress.Value];
            }
        }
        // Handle CHR-ROM/RAM reads (PPU address space)
        else if (address < 0x2000)
        {
            var chrAddress = MapCHRAddress(address);
            if (chrAddress.HasValue)
            {
                if (SupportsCHRRAM && chrAddress.Value < CHRRAMData.Length)
                {
                    return CHRRAMData.Span[chrAddress.Value];
                }
                else if (!SupportsCHRRAM && chrAddress.Value < CHRROMData.Length)
                {
                    return CHRROMData.Span[chrAddress.Value];
                }
            }
        }

        // Address not handled by this mapper
        return 0x00;
    }

    /// <summary>
    /// Write to memory-mapped component
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <param name="value">Byte value</param>
    public void WriteMemory(ushort address, byte value)
    {
        // Handle PRG writes (register writes)
        if (address >= 0x8000)
        {
            WritePRG(address, value);
        }
        // Handle CHR writes
        else if (address < 0x2000)
        {
            WriteCHR(address, value);
        }
    }

    /// <summary>
    /// Reset mapper to initial state
    /// </summary>
    public override void Reset()
    {
        base.Reset();

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogDebug("NROM mapper reset");
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Get mapper state for save/load
    /// </summary>
    /// <returns>Mapper state</returns>
    public override ComponentState GetState()
    {
        var state = (MapperState)base.GetState();
        
        // NROM has no additional state beyond the base mapper state
        return state;
    }

    /// <summary>
    /// Set mapper state from save data
    /// </summary>
    /// <param name="state">Component state</param>
    public override void SetState(ComponentState state)
    {
        base.SetState(state);

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogDebug("NROM mapper state restored");
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Execute one cycle (NROM has no timing-sensitive features)
    /// </summary>
    /// <returns>Number of cycles consumed (always 1)</returns>
    public override int ExecuteCycle()
    {
        // NROM has no timing-sensitive features
        return 1;
    }

    /// <summary>
    /// Mapper-specific initialization
    /// </summary>
    protected override void OnInitialize()
    {
        // NROM requires no special initialization
    }

    /// <summary>
    /// Mapper-specific reset
    /// </summary>
    protected override void OnReset()
    {
        // NROM requires no special reset behavior
    }

    /// <summary>
    /// Mapper-specific state restoration
    /// </summary>
    /// <param name="state">Mapper state</param>
    protected override void OnStateRestored(MapperState state)
    {
        // NROM has no additional state to restore
    }
}

/// <summary>
/// NROM mapper factory for dependency injection
/// </summary>
public class NROMFactory
{
    private readonly ILogger<NROM> _logger;

    public NROMFactory(ILogger<NROM> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create NROM mapper instance
    /// </summary>
    /// <returns>NROM mapper</returns>
    public NROM CreateMapper()
    {
        return new NROM(_logger);
    }
}

/// <summary>
/// NROM mapper registration extensions
/// </summary>
public static class NROMExtensions
{
    /// <summary>
    /// Register NROM mapper with dependency injection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddNROMMapper(
        this Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {
        services.AddTransient<NROMFactory>();
        services.AddTransient<NROM>();
        return services;
    }
}
