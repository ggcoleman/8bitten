using System;
using System.Collections.Generic;
using EightBitten.Core.Contracts;
using Microsoft.Extensions.Logging;

namespace EightBitten.Core.Memory;

/// <summary>
/// CPU memory map implementation for NES system
/// </summary>
public sealed class CPUMemoryMap : ICPUMemoryMap, IComponent
{
    private readonly ILogger<CPUMemoryMap> _logger;
    private readonly List<MemoryMappingInfo> _mappings;
    private readonly byte[] _internalRAM;
    private bool _isInitialized;

    /// <summary>
    /// Component name
    /// </summary>
    public string Name => "CPUMemoryMap";

    /// <summary>
    /// Whether memory map is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Internal RAM (2KB, mirrored to $0000-$1FFF)
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Memory interface requires direct array access for performance")]
    public byte[] InternalRAM => _internalRAM;

    /// <summary>
    /// Memory address ranges this component responds to (entire CPU address space)
    /// </summary>
    public static IReadOnlyList<MemoryRange> AddressRanges => new[]
    {
        new MemoryRange(0x0000, 0xFFFF) // Entire CPU address space
    };

    public CPUMemoryMap(ILogger<CPUMemoryMap> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mappings = new List<MemoryMappingInfo>();
        _internalRAM = new byte[0x0800]; // 2KB internal RAM
    }

    /// <summary>
    /// Initialize memory map
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
            return;

        // Clear internal RAM
        Array.Clear(_internalRAM, 0, _internalRAM.Length);

        _isInitialized = true;

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogInformation("CPU memory map initialized with {RAMSize} bytes internal RAM", _internalRAM.Length);
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Reset memory map to initial state
    /// </summary>
    public void Reset()
    {
        // Clear internal RAM but preserve mappings
        Array.Clear(_internalRAM, 0, _internalRAM.Length);

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogDebug("CPU memory map reset");
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Read byte from memory
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <returns>Byte value</returns>
    public byte ReadByte(ushort address)
    {
        // Internal RAM ($0000-$1FFF, mirrored every 2KB)
        if (address < 0x2000)
        {
            return _internalRAM[address & 0x07FF];
        }

        // PPU Registers ($2000-$3FFF, mirrored every 8 bytes)
        if (address < 0x4000)
        {
            var ppuAddress = (ushort)(0x2000 + (address & 0x0007));
            return ReadFromComponent(ppuAddress);
        }

        // APU and I/O Registers ($4000-$4017)
        if (address >= 0x4000 && address <= 0x4017)
        {
            return ReadFromComponent(address);
        }

        // APU and I/O functionality that is normally disabled ($4018-$401F)
        if (address >= 0x4018 && address <= 0x401F)
        {
            return 0x00; // Open bus behavior
        }

        // Cartridge space ($4020-$FFFF)
        if (address >= 0x4020)
        {
            return ReadFromComponent(address);
        }

        // Default open bus behavior
        return 0x00;
    }

    /// <summary>
    /// Write byte to memory
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <param name="value">Byte value</param>
    public void WriteByte(ushort address, byte value)
    {
        // Internal RAM ($0000-$1FFF, mirrored every 2KB)
        if (address < 0x2000)
        {
            _internalRAM[address & 0x07FF] = value;
            return;
        }

        // PPU Registers ($2000-$3FFF, mirrored every 8 bytes)
        if (address < 0x4000)
        {
            var ppuAddress = (ushort)(0x2000 + (address & 0x0007));
            WriteToComponent(ppuAddress, value);
            return;
        }

        // APU and I/O Registers ($4000-$4017)
        if (address >= 0x4000 && address <= 0x4017)
        {
            WriteToComponent(address, value);
            return;
        }

        // APU and I/O functionality that is normally disabled ($4018-$401F)
        if (address >= 0x4018 && address <= 0x401F)
        {
            // Ignore writes to disabled area
            return;
        }

        // Cartridge space ($4020-$FFFF)
        if (address >= 0x4020)
        {
            WriteToComponent(address, value);
            return;
        }
    }

    /// <summary>
    /// Read from zero page (optimized for 6502 zero page addressing)
    /// </summary>
    /// <param name="address">8-bit zero page address</param>
    /// <returns>Byte value</returns>
    public byte ReadZeroPage(byte address)
    {
        return _internalRAM[address];
    }

    /// <summary>
    /// Write to zero page (optimized for 6502 zero page addressing)
    /// </summary>
    /// <param name="address">8-bit zero page address</param>
    /// <param name="value">Byte value</param>
    public void WriteZeroPage(byte address, byte value)
    {
        _internalRAM[address] = value;
    }

    /// <summary>
    /// Read from stack (optimized for 6502 stack operations)
    /// </summary>
    /// <param name="stackPointer">8-bit stack pointer</param>
    /// <returns>Byte value</returns>
    public byte ReadStack(byte stackPointer)
    {
        return _internalRAM[0x0100 + stackPointer];
    }

    /// <summary>
    /// Write to stack (optimized for 6502 stack operations)
    /// </summary>
    /// <param name="stackPointer">8-bit stack pointer</param>
    /// <param name="value">Byte value</param>
    public void WriteStack(byte stackPointer, byte value)
    {
        _internalRAM[0x0100 + stackPointer] = value;
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
    /// Read interrupt vector (ICPUMemoryMap interface)
    /// </summary>
    /// <param name="vector">Interrupt vector type</param>
    /// <returns>16-bit vector address</returns>
    public ushort ReadInterruptVector(InterruptVector vector) => ReadVector(vector);

    /// <summary>
    /// Read interrupt vector
    /// </summary>
    /// <param name="vector">Interrupt vector type</param>
    /// <returns>16-bit vector address</returns>
    public ushort ReadVector(InterruptVector vector)
    {
        var address = (ushort)vector;
        var low = ReadByte(address);
        var high = ReadByte((ushort)(address + 1));
        return (ushort)(low | (high << 8));
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
            InternalRAM = new byte[_internalRAM.Length]
        };

        Array.Copy(_internalRAM, state.InternalRAM, _internalRAM.Length);

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

        if (state.InternalRAM != null)
        {
            var length = Math.Min(_internalRAM.Length, state.InternalRAM.Length);
            Array.Copy(state.InternalRAM, _internalRAM, length);
        }

        // Restore component states
        foreach (var mapping in _mappings)
        {
            if (state.ComponentStates.TryGetValue(mapping.Component.Name, out var componentState))
            {
                mapping.Component.SetState(componentState);
            }
        }

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogDebug("CPU memory map state restored");
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Read from memory-mapped component (CPU memory map doesn't respond to reads)
    /// </summary>
    public static byte ReadMemory(ushort address) => 0x00;

    /// <summary>
    /// Write to memory-mapped component (CPU memory map doesn't respond to writes)
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
