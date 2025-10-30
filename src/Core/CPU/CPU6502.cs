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
    private bool _forceInitDone;
    private bool _loggedMarioVBlankDetected;
    private bool _loggedMarioErrorHandler;
    private int _ppuDataWriteCount;
    private int _ppuCtrlLogCount;
    private int _ppuMaskLogCount;
    private int _nmiHandledCount;

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

        // Force initialization since Reset() might not be called properly
        ForceInitialization();
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

        // Force console output to ensure we see this - use both Console and Logger
        Console.WriteLine($"*** CPU INITIALIZE: Reset vector at $FFFC = ${resetVector:X4}, PC set to ${_state.PC:X4} ***");

        // Also use logger with critical level to ensure it appears
        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogCritical("*** CPU INITIALIZE: Reset vector at $FFFC = ${ResetVector:X4}, PC set to ${PC:X4} ***", resetVector, _state.PC);
        #pragma warning restore CA1848

        // Debug: Check what's at the reset vector address
        var byte1 = ReadByte(resetVector);
        var byte2 = ReadByte((ushort)(resetVector + 1));
        var byte3 = ReadByte((ushort)(resetVector + 2));
        Console.WriteLine($"*** RESET VECTOR DATA: ${resetVector:X4} = ${byte1:X2} ${byte2:X2} ${byte3:X2} ***");

        // Also check what's at common Mario addresses
        try
        {
            var mario800A = ReadByte(0x800A);
            var mario800D = ReadByte(0x800D);
            var mario8057 = ReadByte(0x8057);
            Console.WriteLine($"*** MARIO CODE CHECK: $800A=${mario800A:X2}, $800D=${mario800D:X2}, $8057=${mario8057:X2} ***");
        }
        #pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
        #pragma warning restore CA1031
        {
            Console.WriteLine($"*** MARIO CODE CHECK FAILED: {ex.Message} ***");
        }

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogInformation("CPU Initialize: Reset vector at $FFFC = ${ResetVector:X4}, PC set to ${PC:X4}", resetVector, _state.PC);
        _logger.LogInformation("Reset vector data: ${Address:X4} = ${Byte1:X2} ${Byte2:X2} ${Byte3:X2}", resetVector, byte1, byte2, byte3);
        #pragma warning restore CA1848

        // Clear halted flag on initialization
        _state.IsHalted = false;

        _isInitialized = true;

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogInformation("CPU6502 initialized, PC set to ${PC:X4}, IsHalted={IsHalted}", _state.PC, _state.IsHalted);
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

        // Clear halted flag on reset
        _state.IsHalted = false;

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogInformation("CPU6502 reset, reset vector: 0x{ResetVector:X4}, PC set to ${PC:X4}, IsHalted={IsHalted}", resetVector, _state.PC, _state.IsHalted);
        #pragma warning restore CA1848

        OnStateChanged();
    }

    /// <summary>
    /// Force CPU initialization even if Reset() is not called
    /// </summary>
    private void ForceInitialization()
    {
        #pragma warning disable CA1303 // Do not pass literals as localized parameters
        Console.WriteLine("*** FORCE CPU INITIALIZATION - Reading reset vector ***");
        #pragma warning restore CA1303

        // Wait a bit for memory map to be ready
        System.Threading.Thread.Sleep(100);

        try
        {
            // Try to read reset vector
            var resetVector = ReadWord(0xFFFC);
            _state.PC = resetVector;
            _state.IsHalted = false;

            #pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine($"*** FORCE INIT SUCCESS: Reset vector = ${resetVector:X4}, PC = ${_state.PC:X4} ***");
            #pragma warning restore CA1303

            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogInformation("FORCE CPU INIT: Reset vector at $FFFC = ${ResetVector:X4}, PC set to ${PC:X4}", resetVector, _state.PC);
            #pragma warning restore CA1848
        }
        #pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
        #pragma warning restore CA1031
        {
            #pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine($"*** FORCE INIT FAILED: {ex.Message} - Using default PC=$8000 ***");
            #pragma warning restore CA1303

            // Fallback to common NES start address
            _state.PC = 0x8000;
            _state.IsHalted = false;

            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogWarning("FORCE CPU INIT FAILED: {Error} - Using fallback PC=${PC:X4}", ex.Message, _state.PC);
            #pragma warning restore CA1848
        }
    }

    /// <summary>
    /// Force CPU initialization on first execution
    /// </summary>
    private void ForceInitializationOnFirstExecution()
    {
        _forceInitDone = true; // Set flag first to prevent recursion

        #pragma warning disable CA1303 // Do not pass literals as localized parameters
        Console.WriteLine("*** FORCE CPU INIT ON FIRST EXECUTION - Reading reset vector ***");
        #pragma warning restore CA1303

        try
        {
            // Try to read reset vector
            var resetVector = ReadWord(0xFFFC);
            if (resetVector != 0x0000 && resetVector != 0xFFFF)
            {
                _state.PC = resetVector;
                _state.IsHalted = false;

                #pragma warning disable CA1303 // Do not pass literals as localized parameters
                Console.WriteLine($"*** FORCE INIT SUCCESS: Reset vector = ${resetVector:X4}, PC = ${_state.PC:X4} ***");
                #pragma warning restore CA1303

                #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                _logger.LogInformation("FORCE CPU INIT ON EXECUTION: Reset vector at $FFFC = ${ResetVector:X4}, PC set to ${PC:X4}", resetVector, _state.PC);
                #pragma warning restore CA1848
                return;
            }
        }
        #pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
        #pragma warning restore CA1031
        {
            #pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine($"*** FORCE INIT FAILED: {ex.Message} ***");
            #pragma warning restore CA1303
        }

        // Fallback to common NES start address
        #pragma warning disable CA1303 // Do not pass literals as localized parameters
        Console.WriteLine("*** FORCE INIT FALLBACK: Using default PC=$8000 ***");
        #pragma warning restore CA1303

        _state.PC = 0x8000;
        _state.IsHalted = false;

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogWarning("FORCE CPU INIT ON EXECUTION FALLBACK: Using fallback PC=${PC:X4}", _state.PC);
        #pragma warning restore CA1848
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
        // Force initialization on first execution if not done
        if (!_forceInitDone)
        {
            ForceInitializationOnFirstExecution();
        }

        // Debug: Log first few calls to ExecuteCycle
        if (_state.TotalCycles < 5)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogInformation("CPU ExecuteCycle called: Enabled={Enabled}, Initialized={Initialized}, TotalCycles={Cycles}",
                IsEnabled, _isInitialized, _state.TotalCycles);
            #pragma warning restore CA1848
        }

        if (!IsEnabled || !_isInitialized || _state.IsHalted)
        {
            // Debug: Log why CPU is not executing every 1000 cycles
            if (_state.TotalCycles % 1000 == 0)
            {
                #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                _logger.LogInformation("CPU ExecuteCycle skipped: Enabled={Enabled}, Initialized={Initialized}, Halted={Halted}",
                    IsEnabled, _isInitialized, _state.IsHalted);
                #pragma warning restore CA1848
            }
            return 1;
        }

        try
        {
            // Debug: Log pending interrupts every 1000 cycles
            if (_state.TotalCycles % 1000 == 0 && _state.PendingInterrupts != InterruptType.None)
            {
                #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                _logger.LogInformation("CPU ExecuteCycle: TotalCycles={Cycles}, PendingInterrupts={Pending}",
                    _state.TotalCycles, _state.PendingInterrupts);
                #pragma warning restore CA1848
            }

            // Handle pending interrupts
            if (_state.PendingInterrupts != InterruptType.None)
            {
                HandleInterrupts();
                return 1;
            }

            // Fetch instruction if starting new instruction
            if (_state.InstructionCycle == 0)
            {
                // EMERGENCY FORCED INITIALIZATION - Check if we're in the interrupt vector area
                if (_state.PC >= 0xFFF0)
                {
                    #pragma warning disable CA1303, CA1848 // Do not pass literals as localized parameters; allow non-LoggerMessage logging
                    _logger.LogDebug("EMERGENCY CPU INIT - Detected execution in interrupt vector area");
                    #pragma warning restore CA1303, CA1848

                    // Force initialization without checking flag
                    try
                    {
                        // Try to read reset vector
                        var resetVector = ReadWord(0xFFFC);
                        if (resetVector != 0x0000 && resetVector != 0xFFFF)
                        {
                            _state.PC = resetVector;
                            _state.IsHalted = false;

                            #pragma warning disable CA1303, CA1848 // Do not pass literals as localized parameters; allow non-LoggerMessage logging
                            _logger.LogDebug("EMERGENCY INIT SUCCESS: Reset vector = ${ResetVector:X4}, PC = ${PC:X4}", resetVector, _state.PC);
                            #pragma warning restore CA1303, CA1848

                            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                            _logger.LogDebug("EMERGENCY CPU INIT: Reset vector at $FFFC = ${ResetVector:X4}, PC set to ${PC:X4}", resetVector, _state.PC);
                            #pragma warning restore CA1848
                            return 1;
                        }
                    }
                    #pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex)
                    #pragma warning restore CA1031
                    {
                        #pragma warning disable CA1303, CA1848 // Do not pass literals as localized parameters; allow non-LoggerMessage logging
                        _logger.LogDebug("EMERGENCY INIT FAILED: {Message}", ex.Message);
                        #pragma warning restore CA1303, CA1848
                    }

                    // Fallback to common NES start address
                    #pragma warning disable CA1303, CA1848 // Do not pass literals as localized parameters; allow non-LoggerMessage logging
                    _logger.LogDebug("EMERGENCY INIT FALLBACK: Using default PC=$8000");
                    #pragma warning restore CA1303, CA1848

                    _state.PC = 0x8000;
                    _state.IsHalted = false;

                    #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                    _logger.LogDebug("EMERGENCY CPU INIT FALLBACK: Using fallback PC=${PC:X4}", _state.PC);
                    #pragma warning restore CA1848
                    return 1;
                }

                _state.CurrentInstruction = ReadByte(_state.PC);
                _state.PC++;

                // Log CPU execution to see what Mario does after VBlank
                var instruction = InstructionSet.GetInstruction(_state.CurrentInstruction);
                var currentPC = (ushort)(_state.PC - 1);

                // Log first 20 cycles to see Mario's initial execution, then reduce frequency
                bool shouldLog = (_state.TotalCycles <= 20) ||
                                (_state.TotalCycles % 1000 == 0) ||
                                (currentPC >= 0x8000 && currentPC < 0x8100 && _state.TotalCycles < 200000);

                if (shouldLog)
                {
                    #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                    _logger.LogInformation("CPU executing: PC=${PC:X4} Opcode=0x{Opcode:X2} ({Mnemonic}) - Cycle #{Cycle}",
                        currentPC, _state.CurrentInstruction, instruction.Mnemonic, _state.TotalCycles);
                    #pragma warning restore CA1848
                }

                // Special logging for Mario's critical addresses and transitions
                if (currentPC == 0x800F || currentPC == 0x8010 || currentPC == 0x8020 || currentPC == 0x8030 ||
                    currentPC == 0x8040 || currentPC == 0x8050 || currentPC == 0x8057 || currentPC == 0x8000 ||
                    currentPC == 0x800A || currentPC == 0x800D)
                {
                    #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                    _logger.LogDebug("*** MARIO CRITICAL ADDRESS: PC=${PC:X4} Opcode=0x{Opcode:X2} ({Mnemonic}) - Cycle #{Count} ***",
                        currentPC, _state.CurrentInstruction, instruction.Mnemonic, _state.TotalCycles);
                    #pragma warning restore CA1848
                }

                // Special logging for when Mario leaves the polling loop or reaches error handler
                if (currentPC != 0x800A && currentPC != 0x800D &&
                    (currentPC == 0x800F || currentPC == 0x8000 || (currentPC >= 0x8010 && currentPC <= 0x8020)))
                {
                    #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                    _logger.LogDebug("*** MARIO LEFT POLLING LOOP: PC=${PC:X4} Opcode=0x{Opcode:X2} ({Mnemonic}) - Cycle #{Count} ***",
                        currentPC, _state.CurrentInstruction, instruction.Mnemonic, _state.TotalCycles);
                    #pragma warning restore CA1848
                }

                // Special logging for when Mario first reaches the error handler
                if (currentPC == 0x8057 && !_loggedMarioErrorHandler)
                {
                    #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                    _logger.LogInformation("*** MARIO REACHED ERROR HANDLER: PC=${PC:X4} Opcode=0x{Opcode:X2} ({Mnemonic}) - Cycle #{Count} - MARIO TIMED OUT! ***",
                        currentPC, _state.CurrentInstruction, instruction.Mnemonic, _state.TotalCycles);
                    #pragma warning restore CA1848
                    _loggedMarioErrorHandler = true;
                }
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

        // Log PPU register reads to see if Mario is polling for PPU warmup
        if (address >= 0x2000 && address <= 0x2007)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogDebug("CPU: Reading from PPU register ${Address:X4} = ${Value:X2} at PC=${PC:X4}, Cycle #{Cycle}",
                address, value, _state.PC - 1, _state.TotalCycles);
            #pragma warning restore CA1848

            // Special logging for PPU $2002 reads - throttle noise
            if (address == 0x2002)
            {
                #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                var vblank = (value & 0x80) != 0;
                if (vblank)
                {
                    _logger.LogDebug("*** MARIO PPU STATUS READ: $2002 = ${Value:X2} (VBlank: SET) at PC=${PC:X4}, Cycle #{Cycle} ***",
                        value, _state.PC - 1, _state.TotalCycles);
                    if (!_loggedMarioVBlankDetected)
                    {
                        _logger.LogInformation("*** MARIO VBLANK DETECTED: PPU $2002 = ${Value:X2} (VBlank flag SET) - Mario should proceed! ***", value);
                        _loggedMarioVBlankDetected = true;
                    }
                }
                else
                {
                    _logger.LogDebug("*** MARIO PPU STATUS READ: $2002 = ${Value:X2} (VBlank: CLEAR) at PC=${PC:X4}, Cycle #{Cycle} ***",
                        value, _state.PC - 1, _state.TotalCycles);
                }
                #pragma warning restore CA1848
            }
        }

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
        // Log PPU register writes (throttled)
        if (address >= 0x2000 && address <= 0x2007)
        {
            switch (address)
            {
                case 0x2000: // PPUCTRL
                    _ppuCtrlLogCount++;
                    if (_ppuCtrlLogCount <= 4)
                    {
                        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                        _logger.LogInformation("CPU: Writing to PPUCTRL ($2000) = ${Value:X2}", value);
                        #pragma warning restore CA1848
                    }
                    else
                    {
                        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                        _logger.LogDebug("CPU: Writing to PPUCTRL ($2000) = ${Value:X2}", value);
                        #pragma warning restore CA1848
                    }
                    break;

                case 0x2001: // PPUMASK
                    _ppuMaskLogCount++;
                    if (_ppuMaskLogCount <= 4)
                    {
                        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                        _logger.LogInformation("CPU: Writing to PPUMASK ($2001) = ${Value:X2}", value);
                        #pragma warning restore CA1848
                    }
                    else
                    {
                        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                        _logger.LogDebug("CPU: Writing to PPUMASK ($2001) = ${Value:X2}", value);
                        #pragma warning restore CA1848
                    }
                    break;

                case 0x2007: // PPUDATA (very frequent)
                    _ppuDataWriteCount++;
                    if ((_ppuDataWriteCount & 0x3F) == 0) // every 64th write
                    {
                        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                        _logger.LogDebug("CPU: Writing to PPUDATA ($2007) = ${Value:X2} (write #{Count})", value, _ppuDataWriteCount);
                        #pragma warning restore CA1848
                    }
                    break;

                default:
                    #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                    _logger.LogDebug("CPU: Writing to PPU register ${Address:X4} = ${Value:X2}", address, value);
                    #pragma warning restore CA1848
                    break;
            }
        }

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

        // NMI interrupts should wake up a halted CPU
        if (interruptType == InterruptType.NMI && _state.IsHalted)
        {
            _state.IsHalted = false;
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogInformation("CPU: NMI interrupt waking up halted CPU");
            #pragma warning restore CA1848
        }

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogDebug("CPU: Interrupt requested - Type: {InterruptType}, Pending: {Pending}, Halted: {Halted}",
            interruptType, _state.PendingInterrupts, _state.IsHalted);
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



            case InstructionType.LDA:
                // Load Accumulator
                var ldaValue = GetOperandValue(instruction);
                _state.A = ldaValue;
                SetZeroNegativeFlags(ldaValue);
                break;

            case InstructionType.STA:
                // Store Accumulator
                var staAddress = GetOperandAddress(instruction);
                WriteByte(staAddress, _state.A);
                break;

            case InstructionType.LDX:
                // Load X Register
                var ldxValue = GetOperandValue(instruction);
                _state.X = ldxValue;
                SetZeroNegativeFlags(ldxValue);
                break;

            case InstructionType.TXS:
                // Transfer X to Stack Pointer
                _state.SP = _state.X;
                break;

            case InstructionType.JMP:
                // Jump - read target address manually for debugging
                ushort jmpAddress;
                if (instruction.AddressingMode == AddressingMode.Absolute)
                {
                    // Read 16-bit address from PC and PC+1
                    var lowByte = ReadByte(_state.PC);
                    var highByte = ReadByte((ushort)(_state.PC + 1));
                    jmpAddress = (ushort)(lowByte | (highByte << 8));
                    _state.PC += 2; // Skip the address bytes
                }
                else
                {
                    // Use existing method for other addressing modes
                    jmpAddress = GetOperandAddress(instruction);
                }

                // Always log JMP for debugging
                #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                _logger.LogDebug("JMP from ${FromPC:X4} to ${ToPC:X4}",
                    (ushort)(_state.PC - 2), jmpAddress);
                #pragma warning restore CA1848

                // Perform the jump
                _state.PC = jmpAddress;
                break;

            case InstructionType.AND:
                // Logical AND
                var andValue = GetOperandValue(instruction);
                _state.A &= andValue;
                SetZeroNegativeFlags(_state.A);
                break;

            case InstructionType.EOR:
                // Exclusive OR
                var eorValue = GetOperandValue(instruction);
                _state.A ^= eorValue;
                SetZeroNegativeFlags(_state.A);
                break;

            case InstructionType.INX:
                // Increment X
                _state.X++;
                SetZeroNegativeFlags(_state.X);
                break;

            case InstructionType.CPX:
                // Compare X Register
                var cpxValue = GetOperandValue(instruction);
                var cpxResult = _state.X - cpxValue;
                SetCarryFlag(cpxResult >= 0);
                SetZeroNegativeFlags((byte)cpxResult);
                break;

            case InstructionType.BNE:
                // Branch if Not Equal (Zero flag clear)
                // For branch instructions, read the offset directly without using GetOperandValue
                var bneOffset = (sbyte)ReadByte(_state.PC++);
                if (!_state.P.HasFlag(ProcessorStatus.Zero))
                {
                    // Branch taken - add offset to current PC
                    _state.PC = (ushort)(_state.PC + bneOffset);
                }
                // Branch not taken - PC already incremented above
                break;

            case InstructionType.BPL:
                // Branch if Positive (Negative flag clear)
                // For branch instructions, read the offset directly without using GetOperandValue
                var bplOffset = (sbyte)ReadByte(_state.PC++);
                if (!_state.P.HasFlag(ProcessorStatus.Negative))
                {
                    // Branch taken - add offset to current PC
                    _state.PC = (ushort)(_state.PC + bplOffset);

                    // Debug log for first few branches
                    if (_state.TotalCycles < 100)
                    {
                        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                        _logger.LogInformation("BPL branch taken: offset={Offset}, new PC=${PC:X4}", bplOffset, _state.PC);
                        #pragma warning restore CA1848
                    }
                }
                else
                {
                    // Branch not taken - PC already incremented above
                    if (_state.TotalCycles < 100)
                    {
                        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                        _logger.LogInformation("BPL branch not taken: offset={Offset}, PC=${PC:X4}", bplOffset, _state.PC);
                        #pragma warning restore CA1848
                    }
                }
                break;

            case InstructionType.SEI:
                // Set Interrupt Disable
                _state.P |= ProcessorStatus.Interrupt;
                break;

            case InstructionType.CLD:
                // Clear Decimal Mode
                _state.P &= ~ProcessorStatus.DecimalMode;
                break;

            case InstructionType.LDY:
                // Load Y Register
                var ldyValue = GetOperandValue(instruction);
                _state.Y = ldyValue;
                SetZeroNegativeFlags(ldyValue);
                break;

            case InstructionType.BCS:
                // Branch if Carry Set
                // For branch instructions, read the offset directly without using GetOperandValue
                var bcsOffset = (sbyte)ReadByte(_state.PC++);
                if (_state.P.HasFlag(ProcessorStatus.Carry))
                {
                    // Branch taken - add offset to current PC
                    _state.PC = (ushort)(_state.PC + bcsOffset);
                }
                // Branch not taken - PC already incremented above
                break;

            case InstructionType.CMP:
                // Compare with Accumulator
                var cmpValue = GetOperandValue(instruction);
                var cmpResult = _state.A - cmpValue;
                SetCarryFlag(cmpResult >= 0);
                SetZeroNegativeFlags((byte)cmpResult);
                break;

            case InstructionType.ASL:
                // Arithmetic Shift Left
                if (instruction.AddressingMode == AddressingMode.Accumulator)
                {
                    // ASL A - shift accumulator
                    SetCarryFlag((_state.A & 0x80) != 0);
                    _state.A = (byte)(_state.A << 1);
                    SetZeroNegativeFlags(_state.A);
                }
                else
                {
                    // ASL memory - shift memory location
                    var address = GetOperandAddress(instruction);
                    var value = ReadByte(address);
                    SetCarryFlag((value & 0x80) != 0);
                    value = (byte)(value << 1);
                    WriteByte(address, value);
                    SetZeroNegativeFlags(value);
                }
                break;



            case InstructionType.DEX:
                // Decrement X Register
                _state.X--;
                SetZeroNegativeFlags(_state.X);
                break;

            case InstructionType.DEY:
                // Decrement Y Register
                _state.Y--;
                SetZeroNegativeFlags(_state.Y);
                break;

            case InstructionType.INY:
                // Increment Y Register
                _state.Y++;
                SetZeroNegativeFlags(_state.Y);
                break;

            case InstructionType.LSR:
                // Logical Shift Right
                if (instruction.AddressingMode == AddressingMode.Accumulator)
                {
                    // LSR A - shift accumulator
                    SetCarryFlag((_state.A & 0x01) != 0);
                    _state.A = (byte)(_state.A >> 1);
                    SetZeroNegativeFlags(_state.A);
                }
                else
                {
                    // LSR memory - shift memory location
                    var address = GetOperandAddress(instruction);
                    var value = ReadByte(address);
                    SetCarryFlag((value & 0x01) != 0);
                    value = (byte)(value >> 1);
                    WriteByte(address, value);
                    SetZeroNegativeFlags(value);
                }
                break;

            case InstructionType.BRK:
                // Break - software interrupt (like IRQ)
                _state.PC++; // Skip the break mark byte

                #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                _logger.LogDebug("BRK instruction executed at PC=${PC:X4} - Triggering IRQ interrupt", _state.PC - 1);
                #pragma warning restore CA1848

                // BRK triggers an IRQ interrupt, not a halt
                // Push PC and status to stack, then jump to IRQ vector
                PushWord((ushort)(_state.PC + 1));
                PushByte((byte)(_state.P | ProcessorStatus.Break | ProcessorStatus.Unused));
                _state.P |= ProcessorStatus.Interrupt;

                // Jump to IRQ vector at $FFFE-$FFFF
                ushort irqVector = ReadWord(0xFFFE);
                _state.PC = irqVector;

                #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                _logger.LogDebug("BRK: Jumped to IRQ vector ${Vector:X4}", irqVector);
                #pragma warning restore CA1848
                break;


            case InstructionType.BIT:
                // Test bits in memory with accumulator
                // Z flag = 1 if (A & M) == 0
                // N flag = M bit 7, V flag = M bit 6
                var bitValue = GetOperandValue(instruction);
                _state.P = _state.P.SetFlag(ProcessorStatus.Zero, (_state.A & bitValue) == 0);
                _state.P = _state.P.SetFlag(ProcessorStatus.Negative, (bitValue & 0x80) != 0);
                _state.P = _state.P.SetFlag(ProcessorStatus.Overflow, (bitValue & 0x40) != 0);
                break;

            case InstructionType.ORA:
                // Inclusive OR with accumulator
                var oraValue = GetOperandValue(instruction);
                _state.A |= oraValue;
                SetZeroNegativeFlags(_state.A);
                break;

            case InstructionType.JSR:
                // Jump to subroutine
                // Push (PC+1) to stack, then set PC to target address
                var jsrTarget = ReadWord(_state.PC);
                _state.PC += 2;
                PushWord((ushort)(_state.PC - 1));
                _state.PC = jsrTarget;
                break;

            case InstructionType.RTS:
                // Return from subroutine
                var rtsAddr = PopWord();
                _state.PC = (ushort)((rtsAddr + 1) & 0xFFFF);
                break;

            case InstructionType.RTI:
                // Return from interrupt
                _state.P = ((ProcessorStatus)PopByte() | ProcessorStatus.Unused);
                _state.PC = PopWord();
                break;

            case InstructionType.PHA:
                // Push accumulator
                PushByte(_state.A);
                break;

            case InstructionType.PLA:
                // Pull accumulator
                _state.A = PopByte();
                SetZeroNegativeFlags(_state.A);
                break;

            case InstructionType.PHP:
                // Push processor status (with Break and Unused set on push semantics)
                PushByte((byte)(_state.P | ProcessorStatus.Break | ProcessorStatus.Unused));
                break;

            case InstructionType.PLP:
                // Pull processor status (ensure Unused bit remains set)
                _state.P = ((ProcessorStatus)PopByte() | ProcessorStatus.Unused);
                break;

            // Simple status flag changes
            case InstructionType.CLC:
                _state.P &= ~ProcessorStatus.Carry;
                break;

            case InstructionType.CLI:
                _state.P &= ~ProcessorStatus.Interrupt;
                break;

            case InstructionType.CLV:
                _state.P &= ~ProcessorStatus.Overflow;
                break;

            case InstructionType.SEC:
                _state.P |= ProcessorStatus.Carry;
                break;

            case InstructionType.SED:
                _state.P |= ProcessorStatus.DecimalMode;
                break;


	            // Register transfers
	            case InstructionType.TAX:
	                _state.X = _state.A;
	                SetZeroNegativeFlags(_state.X);
	                break;
	            case InstructionType.TAY:
	                _state.Y = _state.A;
	                SetZeroNegativeFlags(_state.Y);
	                break;
	            case InstructionType.TXA:
	                _state.A = _state.X;
	                SetZeroNegativeFlags(_state.A);
	                break;
	            case InstructionType.TYA:
	                _state.A = _state.Y;
	                SetZeroNegativeFlags(_state.A);
	                break;
	            case InstructionType.TSX:
	                _state.X = _state.SP;
	                SetZeroNegativeFlags(_state.X);
	                break;

	            // Stores
	            case InstructionType.STX:
	            {
	                var addr = GetOperandAddress(instruction);
	                WriteByte(addr, _state.X);
	                break;
	            }
	            case InstructionType.STY:
	            {
	                var addr = GetOperandAddress(instruction);
	                WriteByte(addr, _state.Y);
	                break;
	            }

	            // Arithmetic
	            case InstructionType.ADC:
	            {
	                var value = GetOperandValue(instruction);
	                int carryIn = _state.P.HasFlag(ProcessorStatus.Carry) ? 1 : 0;
	                int sum = _state.A + value + carryIn;
	                bool overflow = (~(_state.A ^ value) & (_state.A ^ (byte)sum) & 0x80) != 0;
	                SetCarryFlag(sum > 0xFF);
	                _state.P = _state.P.SetFlag(ProcessorStatus.Overflow, overflow);
	                _state.A = (byte)sum;
	                SetZeroNegativeFlags(_state.A);
	                break;
	            }
	            case InstructionType.SBC:
	            {
	                var value = GetOperandValue(instruction);
	                int carryIn = _state.P.HasFlag(ProcessorStatus.Carry) ? 1 : 0;
	                int diff = _state.A - value - (1 - carryIn);
	                bool overflow = (((_state.A ^ (byte)diff) & (_state.A ^ value)) & 0x80) != 0;
	                SetCarryFlag(diff >= 0);
	                _state.P = _state.P.SetFlag(ProcessorStatus.Overflow, overflow);
	                _state.A = (byte)diff;
	                SetZeroNegativeFlags(_state.A);
	                break;
	            }

	            // Increments/Decrements on memory
	            case InstructionType.INC:
	            {
	                var addr = GetOperandAddress(instruction);
	                var v = (byte)(ReadByte(addr) + 1);
	                WriteByte(addr, v);
	                SetZeroNegativeFlags(v);
	                break;
	            }
	            case InstructionType.DEC:
	            {
	                var addr = GetOperandAddress(instruction);
	                var v = (byte)(ReadByte(addr) - 1);
	                WriteByte(addr, v);
	                SetZeroNegativeFlags(v);
	                break;
	            }

	            // Rotates
	            case InstructionType.ROL:
	            {
	                bool oldCarry = _state.P.HasFlag(ProcessorStatus.Carry);
	                if (instruction.AddressingMode == AddressingMode.Accumulator)
	                {
	                    bool newCarry = (_state.A & 0x80) != 0;
	                    _state.A = (byte)((_state.A << 1) | (oldCarry ? 1 : 0));
	                    SetZeroNegativeFlags(_state.A);
	                    _state.P = _state.P.SetFlag(ProcessorStatus.Carry, newCarry);
	                }
	                else
	                {
	                    var addr = GetOperandAddress(instruction);
	                    byte v = ReadByte(addr);
	                    bool newCarry = (v & 0x80) != 0;
	                    v = (byte)((v << 1) | (oldCarry ? 1 : 0));
	                    WriteByte(addr, v);
	                    SetZeroNegativeFlags(v);
	                    _state.P = _state.P.SetFlag(ProcessorStatus.Carry, newCarry);
	                }
	                break;
	            }
	            case InstructionType.ROR:
	            {
	                bool oldCarry = _state.P.HasFlag(ProcessorStatus.Carry);
	                if (instruction.AddressingMode == AddressingMode.Accumulator)
	                {
	                    bool newCarry = (_state.A & 0x01) != 0;
	                    _state.A = (byte)((_state.A >> 1) | (oldCarry ? 0x80 : 0));
	                    SetZeroNegativeFlags(_state.A);
	                    _state.P = _state.P.SetFlag(ProcessorStatus.Carry, newCarry);
	                }
	                else
	                {
	                    var addr = GetOperandAddress(instruction);
	                    byte v = ReadByte(addr);
	                    bool newCarry = (v & 0x01) != 0;
	                    v = (byte)((v >> 1) | (oldCarry ? 0x80 : 0));
	                    WriteByte(addr, v);
	                    SetZeroNegativeFlags(v);
	                    _state.P = _state.P.SetFlag(ProcessorStatus.Carry, newCarry);
	                }
	                break;
	            }

	            // Compare Y
	            case InstructionType.CPY:
	            {
	                var value = GetOperandValue(instruction);
	                int res = _state.Y - value;
	                SetCarryFlag(_state.Y >= value);
	                SetZeroNegativeFlags((byte)res);
	                break;
	            }

	            // Branches
	            case InstructionType.BCC:
	            {
	                var off = (sbyte)ReadByte(_state.PC++);
	                if (!_state.P.HasFlag(ProcessorStatus.Carry)) _state.PC = (ushort)(_state.PC + off);
	                break;
	            }
	            case InstructionType.BEQ:
	            {
	                var off = (sbyte)ReadByte(_state.PC++);
	                if (_state.P.HasFlag(ProcessorStatus.Zero)) _state.PC = (ushort)(_state.PC + off);
	                break;
	            }
	            case InstructionType.BMI:
	            {
	                var off = (sbyte)ReadByte(_state.PC++);
	                if (_state.P.HasFlag(ProcessorStatus.Negative)) _state.PC = (ushort)(_state.PC + off);
	                break;
	            }
	            case InstructionType.BVC:
	            {
	                var off = (sbyte)ReadByte(_state.PC++);
	                if (!_state.P.HasFlag(ProcessorStatus.Overflow)) _state.PC = (ushort)(_state.PC + off);
	                break;
	            }
	            case InstructionType.BVS:
	            {
	                var off = (sbyte)ReadByte(_state.PC++);
	                if (_state.P.HasFlag(ProcessorStatus.Overflow)) _state.PC = (ushort)(_state.PC + off);
	                break;
	            }

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
        // Debug: Log when HandleInterrupts is called with pending interrupts
        if (_state.PendingInterrupts != InterruptType.None)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogDebug("CPU: HandleInterrupts called with pending: {Pending}", _state.PendingInterrupts);
            #pragma warning restore CA1848
        }

        // Handle NMI (highest priority)
        if (_state.PendingInterrupts.HasFlag(InterruptType.NMI))
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogDebug("CPU: Processing NMI interrupt");
            #pragma warning restore CA1848

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
        _nmiHandledCount++;
        if (_nmiHandledCount <= 3)
        {
            _logger.LogInformation("CPU: NMI handled, PC set to ${PC:X4}", _state.PC);
        }
        else
        {
            _logger.LogDebug("CPU: NMI handled, PC set to ${PC:X4}", _state.PC);
        }
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
    /// Get operand value based on addressing mode
    /// </summary>
    private byte GetOperandValue(InstructionDefinition instruction)
    {
        switch (instruction.AddressingMode)
        {
            case AddressingMode.Immediate:
                return ReadByte(_state.PC++);

            case AddressingMode.ZeroPage:
                return ReadByte(ReadByte(_state.PC++));

            case AddressingMode.Absolute:
                var addr = ReadWord(_state.PC);
                _state.PC += 2;
                return ReadByte(addr);

            case AddressingMode.ZeroPageX:
                return ReadByte((byte)(ReadByte(_state.PC++) + _state.X));

            case AddressingMode.AbsoluteX:
                var addrX = (ushort)(ReadWord(_state.PC) + _state.X);
                _state.PC += 2;
                return ReadByte(addrX);

            case AddressingMode.AbsoluteY:
                var addrY = (ushort)(ReadWord(_state.PC) + _state.Y);
                _state.PC += 2;
                return ReadByte(addrY);


	            case AddressingMode.ZeroPageY:
	                return ReadByte((byte)(ReadByte(_state.PC++) + _state.Y));

	            case AddressingMode.IndexedIndirect: // ($nn,X)
	            {
	                byte zp = (byte)(ReadByte(_state.PC++) + _state.X);
	                ushort ptr = (ushort)(ReadByte(zp) | (ReadByte((byte)(zp + 1)) << 8));
	                return ReadByte(ptr);
	            }

	            case AddressingMode.IndirectIndexed: // ($nn),Y
	            {
	                byte zp = ReadByte(_state.PC++);
	                ushort baseAddr = (ushort)(ReadByte(zp) | (ReadByte((byte)(zp + 1)) << 8));
	                ushort eff = (ushort)(baseAddr + _state.Y);
	                return ReadByte(eff);
	            }

            default:
                return 0x00;
        }
    }

    /// <summary>
    /// Get operand address based on addressing mode
    /// </summary>
    private ushort GetOperandAddress(InstructionDefinition instruction)
    {
        switch (instruction.AddressingMode)
        {
            case AddressingMode.ZeroPage:
                return ReadByte(_state.PC++);

	            case AddressingMode.ZeroPageY:
	                return (byte)(ReadByte(_state.PC++) + _state.Y);

	            case AddressingMode.IndexedIndirect: // ($nn,X)
	            {
	                byte zp = (byte)(ReadByte(_state.PC++) + _state.X);
	                ushort addrPtr = (ushort)(ReadByte(zp) | (ReadByte((byte)(zp + 1)) << 8));
	                return addrPtr;
	            }

	            case AddressingMode.IndirectIndexed: // ($nn),Y
	            {
	                byte zp = ReadByte(_state.PC++);
	                ushort baseAddr = (ushort)(ReadByte(zp) | (ReadByte((byte)(zp + 1)) << 8));
	                return (ushort)(baseAddr + _state.Y);
	            }

	            case AddressingMode.Indirect: // Used by JMP ($nnnn) with 6502 wrap bug
	            {
	                ushort ptr = ReadWord(_state.PC);
	                _state.PC += 2;
	                byte lo = ReadByte(ptr);
	                ushort hiAddr = (ushort)((ptr & 0xFF00) | ((ptr + 1) & 0x00FF));
	                byte hi = ReadByte(hiAddr);
	                return (ushort)(lo | (hi << 8));
	            }


            case AddressingMode.Absolute:
                var addr = ReadWord(_state.PC);
                _state.PC += 2;
                return addr;

            case AddressingMode.ZeroPageX:
                return (byte)(ReadByte(_state.PC++) + _state.X);

            case AddressingMode.AbsoluteX:
                var addrX = (ushort)(ReadWord(_state.PC) + _state.X);
                _state.PC += 2;
                return addrX;

            case AddressingMode.AbsoluteY:
                var addrY = (ushort)(ReadWord(_state.PC) + _state.Y);
                _state.PC += 2;
                return addrY;

            default:
                return 0x0000;
        }
    }

    /// <summary>
    /// Set Zero and Negative flags based on value
    /// </summary>
    private void SetZeroNegativeFlags(byte value)
    {
        if (value == 0)
            _state.P |= ProcessorStatus.Zero;
        else
            _state.P &= ~ProcessorStatus.Zero;

        if ((value & 0x80) != 0)
            _state.P |= ProcessorStatus.Negative;
        else
            _state.P &= ~ProcessorStatus.Negative;
    }

    /// <summary>
    /// Set Carry flag
    /// </summary>
    private void SetCarryFlag(bool carry)
    {
        if (carry)
            _state.P |= ProcessorStatus.Carry;
        else
            _state.P &= ~ProcessorStatus.Carry;
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
