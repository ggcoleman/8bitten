using System;
using EightBitten.Core.Contracts;
using EightBitten.Core.Memory;
using Microsoft.Extensions.Logging;

namespace EightBitten.Core.CPU;

/// <summary>
/// 6502 CPU emulation core with cycle-accurate timing
/// </summary>
public sealed class CPU6502 : IClockedComponent, IMemoryMappedComponent
{
    private readonly ILogger<CPU6502> _logger;
    private readonly ICPUMemoryMap _memoryMap;
    private CPUState _state;
    private bool _isInitialized;

    /// <summary>
    /// CPU component name
    /// </summary>
    public string Name => "CPU6502";

    /// <summary>
    /// Whether CPU is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// CPU clock frequency (same as master clock)
    /// </summary>
    public double ClockFrequency => 1789773.0; // NTSC frequency

    /// <summary>
    /// Current CPU state
    /// </summary>
    public CPUState CurrentState => _state.Clone();

    /// <summary>
    /// Memory address ranges this CPU responds to (none - CPU initiates all memory access)
    /// </summary>
    public IReadOnlyList<MemoryRange> AddressRanges => Array.Empty<MemoryRange>();

    /// <summary>
    /// Event fired when CPU state changes
    /// </summary>
    public event EventHandler<CPUStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Event fired when an interrupt is requested
    /// </summary>
    public event EventHandler<InterruptRequestEventArgs>? InterruptRequested;

    public CPU6502(ILogger<CPU6502> logger, ICPUMemoryMap memoryMap)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _memoryMap = memoryMap ?? throw new ArgumentNullException(nameof(memoryMap));
        _state = new CPUState();
    }

    /// <summary>
    /// Initialize CPU component
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
            return;

        _state.Reset();
        
        // Load reset vector
        var resetVector = ReadWord(0xFFFC);
        _state.PC = resetVector;
        
        _isInitialized = true;
        
        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogInformation("CPU6502 initialized, PC set to ${PC:X4}", _state.PC);
        #pragma warning restore CA1848
        
        OnStateChanged();
    }

    /// <summary>
    /// Reset CPU to power-on state
    /// </summary>
    public void Reset()
    {
        _state.Reset();
        
        // Load reset vector
        var resetVector = ReadWord(0xFFFC);
        _state.PC = resetVector;
        
        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogDebug("CPU6502 reset, PC set to ${PC:X4}", _state.PC);
        #pragma warning restore CA1848
        
        OnStateChanged();
    }

    /// <summary>
    /// Execute multiple CPU cycles
    /// </summary>
    /// <param name="cycles">Number of cycles to execute</param>
    /// <returns>Number of cycles actually executed</returns>
    public int ExecuteCycles(int cycles)
    {
        int executed = 0;
        for (int i = 0; i < cycles && IsEnabled && _isInitialized; i++)
        {
            executed += ExecuteCycle();
        }
        return executed;
    }

    /// <summary>
    /// Execute one CPU cycle
    /// </summary>
    /// <returns>Number of cycles consumed (always 1 for CPU)</returns>
    public int ExecuteCycle()
    {
        if (!IsEnabled || !_isInitialized)
            return 1;

        try
        {
            // Handle pending interrupts
            if (_state.PendingInterrupts != InterruptType.None)
            {
                HandleInterrupts();
                return 1;
            }

            // Fetch instruction if starting new instruction
            if (_state.InstructionCycle == 0)
            {
                _state.CurrentInstruction = ReadByte(_state.PC);
                _state.PC++;
                
                #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                _logger.LogTrace("Fetched instruction ${Instruction:X2} at PC ${PC:X4}", 
                    _state.CurrentInstruction, (ushort)(_state.PC - 1));
                #pragma warning restore CA1848
            }

            // Execute instruction cycle
            var cyclesRemaining = ExecuteInstructionCycle();
            
            _state.InstructionCycle++;
            _state.TotalCycles++;

            // Check if instruction is complete
            if (cyclesRemaining <= 0)
            {
                _state.InstructionCycle = 0;
                OnStateChanged();
            }

            return 1;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or IndexOutOfRangeException)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogError(ex, "Error executing CPU cycle at PC ${PC:X4}", _state.PC);
            #pragma warning restore CA1848

            _state.IsHalted = true;
            return 1;
        }
    }

    /// <summary>
    /// Synchronize with master clock
    /// </summary>
    /// <param name="masterCycles">Master clock cycles</param>
    public void SynchronizeClock(long masterCycles)
    {
        // CPU runs at master clock frequency, so execute one cycle
        ExecuteCycle();
    }

    /// <summary>
    /// Read byte from memory
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <returns>Byte value</returns>
    public byte ReadByte(ushort address)
    {
        var value = _memoryMap.ReadByte(address);
        
        _state.LastMemoryAddress = address;
        _state.LastMemoryValue = value;
        _state.LastMemoryWasWrite = false;
        
        return value;
    }

    /// <summary>
    /// Write byte to memory
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <param name="value">Byte value</param>
    public void WriteByte(ushort address, byte value)
    {
        _memoryMap.WriteByte(address, value);
        
        _state.LastMemoryAddress = address;
        _state.LastMemoryValue = value;
        _state.LastMemoryWasWrite = true;
    }

    /// <summary>
    /// Read word (16-bit) from memory (little-endian)
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
    /// Write word (16-bit) to memory (little-endian)
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <param name="value">Word value</param>
    public void WriteWord(ushort address, ushort value)
    {
        WriteByte(address, (byte)(value & 0xFF));
        WriteByte((ushort)(address + 1), (byte)(value >> 8));
    }

    /// <summary>
    /// Request interrupt
    /// </summary>
    /// <param name="interruptType">Type of interrupt</param>
    public void RequestInterrupt(InterruptType interruptType)
    {
        _state.PendingInterrupts |= interruptType;
        
        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogDebug("Interrupt requested: {InterruptType}", interruptType);
        #pragma warning restore CA1848
        
        InterruptRequested?.Invoke(this, new InterruptRequestEventArgs(interruptType));
    }

    /// <summary>
    /// Get component state for save/load
    /// </summary>
    /// <returns>Component state</returns>
    public ComponentState GetState()
    {
        return _state.Clone();
    }

    /// <summary>
    /// Set component state from save data
    /// </summary>
    /// <param name="state">Component state</param>
    public void SetState(ComponentState state)
    {
        if (state is CPUState cpuState)
        {
            _state = cpuState.Clone();
            OnStateChanged();
        }
    }

    /// <summary>
    /// Read from memory-mapped component (CPU doesn't respond to reads)
    /// </summary>
    public static byte ReadMemory(ushort address) => 0x00;

    /// <summary>
    /// Write to memory-mapped component (CPU doesn't respond to writes)
    /// </summary>
    public static void WriteMemory(ushort address, byte value) { }

    /// <summary>
    /// Check if CPU handles the specified address (CPU doesn't handle any addresses)
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <returns>False - CPU doesn't handle any addresses</returns>
    public bool HandlesAddress(ushort address) => false;

    /// <summary>
    /// Execute one cycle of the current instruction
    /// </summary>
    /// <returns>Remaining cycles for instruction (0 when complete)</returns>
    private int ExecuteInstructionCycle()
    {
        var instruction = InstructionSet.GetInstruction(_state.CurrentInstruction);

        // For now, implement basic NOP behavior for all instructions
        // T021 will implement the full instruction execution

        // Calculate cycles needed
        var cyclesNeeded = instruction.Cycles;

        // Check if this is the final cycle of the instruction
        if (_state.InstructionCycle >= cyclesNeeded - 1)
        {
            // Execute the instruction effect (placeholder)
            ExecuteInstructionEffect(instruction);
            return 0; // Instruction complete
        }

        return cyclesNeeded - _state.InstructionCycle - 1; // Remaining cycles
    }

    /// <summary>
    /// Execute the effect of an instruction (placeholder for T021)
    /// </summary>
    /// <param name="instruction">Instruction definition</param>
    private void ExecuteInstructionEffect(InstructionDefinition instruction)
    {
        // Placeholder implementation - just handle NOP for now
        // T021 will implement all instruction effects

        switch (instruction.Type)
        {
            case InstructionType.NOP:
                // No operation - do nothing
                break;

            case InstructionType.BRK:
                // Break - halt CPU for now
                _state.IsHalted = true;
                break;

            default:
                // For now, treat all other instructions as NOP
                #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                _logger.LogTrace("Instruction {Instruction} not yet implemented, treating as NOP",
                    instruction.Mnemonic);
                #pragma warning restore CA1848
                break;
        }
    }

    /// <summary>
    /// Handle pending interrupts
    /// </summary>
    private void HandleInterrupts()
    {
        // Handle NMI (highest priority)
        if (_state.PendingInterrupts.HasFlag(InterruptType.NMI))
        {
            HandleNMI();
            _state.PendingInterrupts &= ~InterruptType.NMI;
            return;
        }

        // Handle IRQ (if not masked)
        if (_state.PendingInterrupts.HasFlag(InterruptType.IRQ) && 
            !_state.P.HasFlag(ProcessorStatus.Interrupt))
        {
            HandleIRQ();
            _state.PendingInterrupts &= ~InterruptType.IRQ;
            return;
        }

        // Handle Reset
        if (_state.PendingInterrupts.HasFlag(InterruptType.Reset))
        {
            Reset();
            _state.PendingInterrupts &= ~InterruptType.Reset;
        }
    }

    /// <summary>
    /// Handle Non-Maskable Interrupt
    /// </summary>
    private void HandleNMI()
    {
        // Push PC and status to stack
        PushWord(_state.PC);
        PushByte((byte)_state.P);
        
        // Set interrupt flag and jump to NMI vector
        _state.P |= ProcessorStatus.Interrupt;
        _state.PC = ReadWord(0xFFFA);
        
        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogDebug("NMI handled, PC set to ${PC:X4}", _state.PC);
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Handle Interrupt Request
    /// </summary>
    private void HandleIRQ()
    {
        // Push PC and status to stack
        PushWord(_state.PC);
        PushByte((byte)_state.P);
        
        // Set interrupt flag and jump to IRQ vector
        _state.P |= ProcessorStatus.Interrupt;
        _state.PC = ReadWord(0xFFFE);
        
        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogDebug("IRQ handled, PC set to ${PC:X4}", _state.PC);
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Push byte to stack
    /// </summary>
    private void PushByte(byte value)
    {
        WriteByte((ushort)(0x0100 + _state.SP), value);
        _state.SP--;
    }

    /// <summary>
    /// Push word to stack
    /// </summary>
    private void PushWord(ushort value)
    {
        PushByte((byte)(value >> 8));   // High byte first
        PushByte((byte)(value & 0xFF)); // Low byte second
    }

    /// <summary>
    /// Pop byte from stack
    /// </summary>
    private byte PopByte()
    {
        _state.SP++;
        return ReadByte((ushort)(0x0100 + _state.SP));
    }

    /// <summary>
    /// Pop word from stack
    /// </summary>
    private ushort PopWord()
    {
        var low = PopByte();  // Low byte first
        var high = PopByte(); // High byte second
        return (ushort)(low | (high << 8));
    }

    /// <summary>
    /// Fire state changed event
    /// </summary>
    private void OnStateChanged()
    {
        StateChanged?.Invoke(this, new CPUStateChangedEventArgs(_state.Clone()));
    }

    /// <summary>
    /// Dispose CPU resources
    /// </summary>
    public void Dispose()
    {
        // No unmanaged resources to dispose
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Event arguments for CPU state changes
/// </summary>
public class CPUStateChangedEventArgs : EventArgs
{
    public CPUState State { get; }

    public CPUStateChangedEventArgs(CPUState state)
    {
        State = state;
    }
}

/// <summary>
/// Event arguments for interrupt requests
/// </summary>
public class InterruptRequestEventArgs : EventArgs
{
    public InterruptType InterruptType { get; }

    public InterruptRequestEventArgs(InterruptType interruptType)
    {
        InterruptType = interruptType;
    }
}
