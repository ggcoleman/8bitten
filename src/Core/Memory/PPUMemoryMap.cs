using System;
using System.Collections.Generic;
using EightBitten.Core.Contracts;
using Microsoft.Extensions.Logging;

namespace EightBitten.Core.Memory;

/// <summary>
/// PPU memory map implementation for NES system
/// </summary>
public sealed class PPUMemoryMap : IPPUMemoryMap, IComponent
{
    private readonly ILogger<PPUMemoryMap> _logger;
    private readonly List<MemoryMappingInfo> _mappings;
    private readonly byte[] _vram;
    private readonly byte[] _paletteRAM;
    private MirroringMode _mirroringMode;
    private bool _isInitialized;

    /// <summary>
    /// Component name
    /// </summary>
    public string Name => "PPUMemoryMap";

    /// <summary>
    /// Whether memory map is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Current nametable mirroring mode
    /// </summary>
    public MirroringMode MirroringMode
    {
        get => _mirroringMode;
        set => SetMirroring(value);
    }

    /// <summary>
    /// Memory address ranges this component responds to (entire PPU address space)
    /// </summary>
    public static IReadOnlyList<MemoryRange> AddressRanges => new[]
    {
        new MemoryRange(0x0000, 0x3FFF) // Entire PPU address space
    };

    public PPUMemoryMap(ILogger<PPUMemoryMap> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mappings = new List<MemoryMappingInfo>();
        _vram = new byte[0x1000]; // 4KB VRAM (2KB physical, mirrored)
        _paletteRAM = new byte[0x20]; // 32 bytes palette RAM
        _mirroringMode = MirroringMode.Horizontal; // Default mirroring
    }

    /// <summary>
    /// Initialize memory map
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
            return;

        // Clear VRAM and palette RAM
        Array.Clear(_vram, 0, _vram.Length);
        Array.Clear(_paletteRAM, 0, _paletteRAM.Length);

        _isInitialized = true;

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogInformation("PPU memory map initialized with {VRAMSize} bytes VRAM and {PaletteSize} bytes palette RAM",
            _vram.Length, _paletteRAM.Length);
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Reset memory map to initial state
    /// </summary>
    public void Reset()
    {
        // Clear VRAM and palette RAM but preserve mappings
        Array.Clear(_vram, 0, _vram.Length);
        Array.Clear(_paletteRAM, 0, _paletteRAM.Length);

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogDebug("PPU memory map reset");
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Read byte from PPU memory
    /// </summary>
    /// <param name="address">PPU memory address</param>
    /// <returns>Byte value</returns>
    public byte ReadByte(ushort address)
    {
        // Mask to 14-bit address space
        address &= 0x3FFF;

        // Pattern Tables ($0000-$1FFF) - handled by cartridge CHR-ROM/RAM
        if (address < 0x2000)
        {
            return ReadFromComponent(address);
        }

        // Nametables ($2000-$2FFF, mirrored to $3000-$3EFF)
        if (address < 0x3F00)
        {
            var nametableAddress = GetMirroredNametableAddress(address);
            return _vram[nametableAddress];
        }

        // Palette RAM ($3F00-$3F1F, mirrored to $3F20-$3FFF)
        if (address >= 0x3F00)
        {
            var paletteAddress = GetMirroredPaletteAddress(address);
            return _paletteRAM[paletteAddress];
        }

        return 0x00;
    }

    /// <summary>
    /// Write byte to PPU memory
    /// </summary>
    /// <param name="address">PPU memory address</param>
    /// <param name="value">Byte value</param>
    public void WriteByte(ushort address, byte value)
    {
        // Mask to 14-bit address space
        address &= 0x3FFF;

        // Pattern Tables ($0000-$1FFF) - handled by cartridge CHR-ROM/RAM
        if (address < 0x2000)
        {
            WriteToComponent(address, value);
            return;
        }

        // Nametables ($2000-$2FFF, mirrored to $3000-$3EFF)
        if (address < 0x3F00)
        {
            var nametableAddress = GetMirroredNametableAddress(address);
            _vram[nametableAddress] = value;
            return;
        }

        // Palette RAM ($3F00-$3F1F, mirrored to $3F20-$3FFF)
        if (address >= 0x3F00)
        {
            var paletteAddress = GetMirroredPaletteAddress(address);
            _paletteRAM[paletteAddress] = value;
            return;
        }
    }

    /// <summary>
    /// Read from pattern table (CHR-ROM/RAM)
    /// </summary>
    /// <param name="address">Pattern table address ($0000-$1FFF)</param>
    /// <returns>Byte value</returns>
    public byte ReadPattern(ushort address)
    {
        return ReadByte((ushort)(address & 0x1FFF));
    }

    /// <summary>
    /// Write to pattern table (CHR-RAM only)
    /// </summary>
    /// <param name="address">Pattern table address ($0000-$1FFF)</param>
    /// <param name="value">Byte value</param>
    public void WritePattern(ushort address, byte value)
    {
        WriteByte((ushort)(address & 0x1FFF), value);
    }

    /// <summary>
    /// Read from nametable (IPPUMemoryMap interface)
    /// </summary>
    /// <param name="address">Nametable address ($2000-$2FFF)</param>
    /// <returns>Byte value</returns>
    public byte ReadNameTable(ushort address) => ReadNametableInternal(address);

    /// <summary>
    /// Write to nametable (IPPUMemoryMap interface)
    /// </summary>
    /// <param name="address">Nametable address ($2000-$2FFF)</param>
    /// <param name="value">Byte value</param>
    public void WriteNameTable(ushort address, byte value) => WriteNametableInternal(address, value);

    /// <summary>
    /// Read from nametable
    /// </summary>
    /// <param name="address">Nametable address ($2000-$2FFF)</param>
    /// <returns>Byte value</returns>
    public byte ReadNametableInternal(ushort address)
    {
        return ReadByte(address);
    }

    /// <summary>
    /// Write to nametable
    /// </summary>
    /// <param name="address">Nametable address ($2000-$2FFF)</param>
    /// <param name="value">Byte value</param>
    public void WriteNametableInternal(ushort address, byte value)
    {
        WriteByte(address, value);
    }

    /// <summary>
    /// Read from palette RAM
    /// </summary>
    /// <param name="address">Palette address ($3F00-$3F1F)</param>
    /// <returns>Byte value</returns>
    public byte ReadPalette(ushort address)
    {
        return ReadByte(address);
    }

    /// <summary>
    /// Write to palette RAM
    /// </summary>
    /// <param name="address">Palette address ($3F00-$3F1F)</param>
    /// <param name="value">Byte value</param>
    public void WritePalette(ushort address, byte value)
    {
        WriteByte(address, value);
    }

    /// <summary>
    /// Read word (16-bit) from memory (IMemoryMap interface)
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <returns>Word value</returns>
    public ushort ReadWord(ushort address)
    {
        var low = ReadByte(address);
        var high = ReadByte((ushort)(address + 1));
        return (ushort)(low | (high << 8));
    }

    /// <summary>
    /// Write word (16-bit) to memory (IMemoryMap interface)
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <param name="value">Word value</param>
    public void WriteWord(ushort address, ushort value)
    {
        WriteByte(address, (byte)(value & 0xFF));
        WriteByte((ushort)(address + 1), (byte)(value >> 8));
    }

    /// <summary>
    /// Set nametable mirroring mode
    /// </summary>
    /// <param name="mode">Mirroring mode</param>
    public void SetMirroring(MirroringMode mode)
    {
        if (_mirroringMode != mode)
        {
            var previousMode = _mirroringMode;
            _mirroringMode = mode;

            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogDebug("PPU mirroring changed from {PreviousMode} to {NewMode}", previousMode, mode);
            #pragma warning restore CA1848
        }
    }

    /// <summary>
    /// Register memory-mapped component (IMemoryMap interface)
    /// </summary>
    /// <param name="component">Component to register</param>
    /// <param name="priority">Priority for overlapping ranges</param>
    public void RegisterComponent(IMemoryMappedComponent component, int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(component);
        RegisterComponent(component, component.AddressRanges, priority);
    }

    /// <summary>
    /// Get all memory mappings (IMemoryMap interface)
    /// </summary>
    /// <returns>List of memory mappings</returns>
    public IReadOnlyList<MemoryMappingInfo> GetMappings() => _mappings.AsReadOnly();

    /// <summary>
    /// Register memory-mapped component
    /// </summary>
    /// <param name="component">Component to register</param>
    /// <param name="ranges">Address ranges the component handles</param>
    /// <param name="priority">Priority for overlapping ranges (higher = higher priority)</param>
    public void RegisterComponent(IMemoryMappedComponent component, IReadOnlyList<MemoryRange> ranges, int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(ranges);

        var mapping = new MemoryMappingInfo(component, ranges, priority);
        _mappings.Add(mapping);

        // Sort by priority (highest first)
        _mappings.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogDebug("Registered component {ComponentName} with {RangeCount} address ranges at priority {Priority}",
            component.Name, ranges.Count, priority);
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Unregister memory-mapped component
    /// </summary>
    /// <param name="component">Component to unregister</param>
    public void UnregisterComponent(IMemoryMappedComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);

        var removed = _mappings.RemoveAll(m => m.Component == component);

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogDebug("Unregistered component {ComponentName}, removed {Count} mappings",
            component.Name, removed);
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Get memory map state for save/load
    /// </summary>
    /// <returns>Memory map state</returns>
    public MemoryMapState GetState()
    {
        var state = new MemoryMapState
        {
            ComponentName = Name,
            VRAM = new byte[_vram.Length],
            PaletteRAM = new byte[_paletteRAM.Length],
            MirroringMode = _mirroringMode
        };

        Array.Copy(_vram, state.VRAM, _vram.Length);
        Array.Copy(_paletteRAM, state.PaletteRAM, _paletteRAM.Length);

        // Collect component states
        foreach (var mapping in _mappings)
        {
            var componentState = mapping.Component.GetState();
            state.ComponentStates[mapping.Component.Name] = componentState;
        }

        return state;
    }

    /// <summary>
    /// Restore memory state from saved data
    /// </summary>
    /// <param name="state">Memory state to restore</param>
    public void SetState(MemoryMapState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.VRAM != null)
        {
            var length = Math.Min(_vram.Length, state.VRAM.Length);
            Array.Copy(state.VRAM, _vram, length);
        }

        if (state.PaletteRAM != null)
        {
            var length = Math.Min(_paletteRAM.Length, state.PaletteRAM.Length);
            Array.Copy(state.PaletteRAM, _paletteRAM, length);
        }

        _mirroringMode = state.MirroringMode;

        // Restore component states
        foreach (var mapping in _mappings)
        {
            if (state.ComponentStates.TryGetValue(mapping.Component.Name, out var componentState))
            {
                mapping.Component.SetState(componentState);
            }
        }

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogDebug("PPU memory map state restored");
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Read from memory-mapped component (PPU memory map doesn't respond to reads)
    /// </summary>
    public static byte ReadMemory(ushort address) => 0x00;

    /// <summary>
    /// Write to memory-mapped component (PPU memory map doesn't respond to writes)
    /// </summary>
    public static void WriteMemory(ushort address, byte value) { }

    /// <summary>
    /// Get component state for save/load
    /// </summary>
    /// <returns>Component state</returns>
    ComponentState IComponent.GetState() => GetState();

    /// <summary>
    /// Set component state from save data
    /// </summary>
    /// <param name="state">Component state</param>
    void IComponent.SetState(ComponentState state)
    {
        if (state is MemoryMapState memoryState)
        {
            SetState(memoryState);
        }
    }

    /// <summary>
    /// Execute one cycle (IComponent interface)
    /// </summary>
    /// <returns>Number of cycles consumed (always 1)</returns>
    public int ExecuteCycle()
    {
        // Memory maps don't have timing-sensitive operations
        return 1;
    }

    /// <summary>
    /// Dispose memory map resources
    /// </summary>
    public void Dispose()
    {
        _mappings.Clear();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Get mirrored nametable address based on current mirroring mode
    /// </summary>
    /// <param name="address">Original nametable address</param>
    /// <returns>Mirrored address in VRAM</returns>
    private ushort GetMirroredNametableAddress(ushort address)
    {
        // Remove mirroring ($3000-$3EFF mirrors $2000-$2EFF)
        address = (ushort)((address - 0x2000) & 0x0EFF);

        return _mirroringMode switch
        {
            MirroringMode.Horizontal => (ushort)(address & 0x03FF | ((address & 0x0800) >> 1)),
            MirroringMode.Vertical => (ushort)(address & 0x07FF),
            MirroringMode.SingleScreenLower => (ushort)(address & 0x03FF),
            MirroringMode.SingleScreenUpper => (ushort)((address & 0x03FF) | 0x0400),
            MirroringMode.FourScreen => address, // No mirroring for four-screen
            _ => (ushort)(address & 0x03FF) // Default to single screen lower
        };
    }

    /// <summary>
    /// Get mirrored palette address
    /// </summary>
    /// <param name="address">Original palette address</param>
    /// <returns>Mirrored address in palette RAM</returns>
    private static ushort GetMirroredPaletteAddress(ushort address)
    {
        // Palette RAM is mirrored every 32 bytes
        var paletteAddress = (ushort)((address - 0x3F00) & 0x1F);

        // Handle sprite palette mirroring to background palette
        // $3F10, $3F14, $3F18, $3F1C mirror $3F00, $3F04, $3F08, $3F0C
        if ((paletteAddress & 0x13) == 0x10)
        {
            paletteAddress &= 0x0F;
        }

        return paletteAddress;
    }

    /// <summary>
    /// Read from registered component
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <returns>Byte value</returns>
    private byte ReadFromComponent(ushort address)
    {
        foreach (var mapping in _mappings)
        {
            foreach (var range in mapping.Ranges)
            {
                if (range.Contains(address))
                {
                    return mapping.Component.ReadByte(address);
                }
            }
        }

        // No component handles this address - return open bus
        return 0x00;
    }

    /// <summary>
    /// Write to registered component
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <param name="value">Byte value</param>
    private void WriteToComponent(ushort address, byte value)
    {
        foreach (var mapping in _mappings)
        {
            foreach (var range in mapping.Ranges)
            {
                if (range.Contains(address))
                {
                    mapping.Component.WriteByte(address, value);
                    return;
                }
            }
        }

        // No component handles this address - ignore write
    }
}
