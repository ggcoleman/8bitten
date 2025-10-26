using System;
using System.Collections.Generic;

namespace EightBitten.Core.Contracts;

/// <summary>
/// Base interface for all emulated hardware components
/// </summary>
public interface IComponent : IDisposable
{
    /// <summary>
    /// Component name for identification and logging
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Whether the component is currently enabled
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Initialize the component to power-on state
    /// </summary>
    void Initialize();

    /// <summary>
    /// Reset the component to initial state
    /// </summary>
    void Reset();

    /// <summary>
    /// Execute one cycle of component operation
    /// </summary>
    /// <returns>Number of cycles consumed</returns>
    int ExecuteCycle();

    /// <summary>
    /// Get current component state for save/load operations
    /// </summary>
    /// <returns>Serializable component state</returns>
    ComponentState GetState();

    /// <summary>
    /// Restore component state from saved data
    /// </summary>
    /// <param name="state">Component state to restore</param>
    void SetState(ComponentState state);
}

/// <summary>
/// Base class for component state serialization
/// </summary>
public abstract class ComponentState
{
    /// <summary>
    /// Component name this state belongs to
    /// </summary>
    public string ComponentName { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when state was captured
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Cycle count when state was captured
    /// </summary>
    public long CycleCount { get; set; }
}

/// <summary>
/// Interface for components that can be clocked by the timing system
/// </summary>
public interface IClockedComponent : IComponent
{
    /// <summary>
    /// Clock frequency in Hz
    /// </summary>
    double ClockFrequency { get; }

    /// <summary>
    /// Execute cycles for the specified time period
    /// </summary>
    /// <param name="cycles">Number of cycles to execute</param>
    /// <returns>Actual cycles executed</returns>
    int ExecuteCycles(int cycles);

    /// <summary>
    /// Synchronize component timing with master clock
    /// </summary>
    /// <param name="masterCycles">Master clock cycle count</param>
    void SynchronizeClock(long masterCycles);
}

/// <summary>
/// Interface for components that handle memory-mapped I/O
/// </summary>
public interface IMemoryMappedComponent : IComponent
{
    /// <summary>
    /// Memory address ranges this component responds to
    /// </summary>
    IReadOnlyList<MemoryRange> AddressRanges { get; }

    /// <summary>
    /// Read a byte from the specified address
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <returns>Byte value at address</returns>
    byte ReadByte(ushort address);

    /// <summary>
    /// Write a byte to the specified address
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <param name="value">Byte value to write</param>
    void WriteByte(ushort address, byte value);

    /// <summary>
    /// Check if this component handles the specified address
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <returns>True if component handles this address</returns>
    bool HandlesAddress(ushort address);
}

/// <summary>
/// Represents a memory address range
/// </summary>
public readonly struct MemoryRange : IEquatable<MemoryRange>
{
    /// <summary>
    /// Start address (inclusive)
    /// </summary>
    public ushort Start { get; }

    /// <summary>
    /// End address (inclusive)
    /// </summary>
    public ushort End { get; }

    /// <summary>
    /// Size of the memory range in bytes
    /// </summary>
    public int Size => End - Start + 1;

    public MemoryRange(ushort start, ushort end)
    {
        if (end < start)
            throw new ArgumentException("End address must be greater than or equal to start address");

        Start = start;
        End = end;
    }

    /// <summary>
    /// Check if an address falls within this range
    /// </summary>
    /// <param name="address">Address to check</param>
    /// <returns>True if address is within range</returns>
    public bool Contains(ushort address) => address >= Start && address <= End;

    /// <summary>
    /// Check if this range overlaps with another range
    /// </summary>
    /// <param name="other">Other range to check</param>
    /// <returns>True if ranges overlap</returns>
    public bool Overlaps(MemoryRange other) => Start <= other.End && End >= other.Start;

    public override string ToString() => $"${Start:X4}-${End:X4} ({Size} bytes)";

    public override bool Equals(object? obj) => obj is MemoryRange other && Equals(other);

    public bool Equals(MemoryRange other) => Start == other.Start && End == other.End;

    public override int GetHashCode() => HashCode.Combine(Start, End);

    public static bool operator ==(MemoryRange left, MemoryRange right) => left.Equals(right);

    public static bool operator !=(MemoryRange left, MemoryRange right) => !left.Equals(right);
}
