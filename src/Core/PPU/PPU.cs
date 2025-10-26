using System;
using System.Collections.Generic;
using EightBitten.Core.Contracts;
using EightBitten.Core.Memory;
using Microsoft.Extensions.Logging;

namespace EightBitten.Core.PPU;

/// <summary>
/// NES PPU (Picture Processing Unit) emulation core
/// Headless mode - accurate timing and memory access without rendering
/// </summary>
public sealed class PictureProcessingUnit : IClockedComponent, IMemoryMappedComponent
{
    private readonly ILogger<PictureProcessingUnit> _logger;
    private readonly IPPUMemoryMap _memoryMap;
    private PPUState _state;
    private bool _isInitialized;
    private bool _isHeadless;

    // PPU timing constants
    private const int SCANLINES_PER_FRAME = 262; // NTSC
    private const int CYCLES_PER_SCANLINE = 341;
    private const int VISIBLE_SCANLINES = 240;
    private const int VBLANK_SCANLINE = 241;
    private const int PRE_RENDER_SCANLINE = 261;

    /// <summary>
    /// Component name
    /// </summary>
    public string Name => "PPU";

    /// <summary>
    /// Whether PPU is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// PPU clock frequency (3x CPU frequency)
    /// </summary>
    public double ClockFrequency => 5369319.0; // NTSC PPU frequency

    /// <summary>
    /// Current PPU state
    /// </summary>
    public PPUState CurrentState => _state.Clone();

    /// <summary>
    /// Whether PPU is in headless mode (no rendering)
    /// </summary>
    public bool IsHeadless => _isHeadless;

    /// <summary>
    /// Memory address ranges this PPU responds to (PPU registers)
    /// </summary>
    public IReadOnlyList<MemoryRange> AddressRanges => new[]
    {
        new MemoryRange(0x2000, 0x2007), // PPU registers
        new MemoryRange(0x4014, 0x4014)  // OAM DMA register
    };

    /// <summary>
    /// Event fired when VBlank starts
    /// </summary>
    public event EventHandler<VBlankEventArgs>? VBlankStarted;

    /// <summary>
    /// Event fired when VBlank ends
    /// </summary>
    public event EventHandler<VBlankEventArgs>? VBlankEnded;

    /// <summary>
    /// Event fired when a frame is completed
    /// </summary>
    public event EventHandler<FrameCompletedEventArgs>? FrameCompleted;

    public PictureProcessingUnit(ILogger<PictureProcessingUnit> logger, IPPUMemoryMap memoryMap, bool headless = true)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _memoryMap = memoryMap ?? throw new ArgumentNullException(nameof(memoryMap));
        _state = new PPUState();
        _isHeadless = headless;
    }

    /// <summary>
    /// Initialize PPU component
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
            return;

        _state.Reset();
        _isInitialized = true;

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogInformation("PPU initialized in {Mode} mode", _isHeadless ? "headless" : "rendering");
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Reset PPU to power-on state
    /// </summary>
    public void Reset()
    {
        _state.Reset();

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogDebug("PPU reset");
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Execute multiple PPU cycles
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
    /// Execute one PPU cycle
    /// </summary>
    /// <returns>Number of cycles consumed (always 1)</returns>
    public int ExecuteCycle()
    {
        if (!IsEnabled || !_isInitialized)
            return 1;

        try
        {
            // Execute PPU cycle based on current scanline and cycle
            ExecutePPUCycle();

            // Advance timing
            _state.Cycle++;
            if (_state.Cycle >= CYCLES_PER_SCANLINE)
            {
                _state.Cycle = 0;
                _state.Scanline++;

                if (_state.Scanline >= SCANLINES_PER_FRAME)
                {
                    _state.Scanline = 0;
                    _state.Frame++;
                    _state.OddFrame = !_state.OddFrame;

                    // Fire frame completed event
                    OnFrameCompleted();
                }
            }

            return 1;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogError(ex, "Error executing PPU cycle at scanline {Scanline}, cycle {Cycle}",
                _state.Scanline, _state.Cycle);
            #pragma warning restore CA1848
            return 1;
        }
    }

    /// <summary>
    /// Synchronize with master clock
    /// </summary>
    /// <param name="masterCycles">Master clock cycles</param>
    public void SynchronizeClock(long masterCycles)
    {
        // PPU runs at 3x CPU frequency, so execute 3 cycles for each master cycle
        for (int i = 0; i < 3; i++)
        {
            ExecuteCycle();
        }
    }

    /// <summary>
    /// Read from PPU register
    /// </summary>
    /// <param name="address">Register address</param>
    /// <returns>Register value</returns>
    public byte ReadMemory(ushort address)
    {
        return address switch
        {
            0x2000 => 0x00, // PPUCTRL - write-only
            0x2001 => 0x00, // PPUMASK - write-only
            0x2002 => ReadPPUSTATUS(),
            0x2003 => 0x00, // OAMADDR - write-only
            0x2004 => ReadOAMDATA(),
            0x2005 => 0x00, // PPUSCROLL - write-only
            0x2006 => 0x00, // PPUADDR - write-only
            0x2007 => ReadPPUDATA(),
            0x4014 => 0x00, // OAMDMA - write-only
            _ => 0x00
        };
    }

    /// <summary>
    /// Read byte from PPU (IMemoryMappedComponent interface)
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <returns>Byte value</returns>
    public byte ReadByte(ushort address) => ReadMemory(address);

    /// <summary>
    /// Write byte to PPU (IMemoryMappedComponent interface)
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <param name="value">Byte value</param>
    public void WriteByte(ushort address, byte value) => WriteMemory(address, value);

    /// <summary>
    /// Check if PPU handles the specified address
    /// </summary>
    /// <param name="address">Memory address</param>
    /// <returns>True if PPU handles this address</returns>
    public bool HandlesAddress(ushort address)
    {
        return (address >= 0x2000 && address <= 0x2007) || address == 0x4014;
    }

    /// <summary>
    /// Write to PPU register
    /// </summary>
    /// <param name="address">Register address</param>
    /// <param name="value">Value to write</param>
    public void WriteMemory(ushort address, byte value)
    {
        switch (address)
        {
            case 0x2000: WritePPUCTRL(value); break;
            case 0x2001: WritePPUMASK(value); break;
            case 0x2002: break; // PPUSTATUS - read-only
            case 0x2003: WriteOAMADDR(value); break;
            case 0x2004: WriteOAMDATA(value); break;
            case 0x2005: WritePPUSCROLL(value); break;
            case 0x2006: WritePPUADDR(value); break;
            case 0x2007: WritePPUDATA(value); break;
            case 0x4014: WriteOAMDMA(value); break;
        }
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
        if (state is PPUState ppuState)
        {
            _state = ppuState.Clone();
        }
    }

    /// <summary>
    /// Dispose PPU resources
    /// </summary>
    public void Dispose()
    {
        // No unmanaged resources to dispose
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Execute one PPU cycle based on current timing
    /// </summary>
    private void ExecutePPUCycle()
    {
        // Handle different scanline types
        if (_state.Scanline < VISIBLE_SCANLINES)
        {
            // Visible scanlines (0-239)
            ExecuteVisibleScanline();
        }
        else if (_state.Scanline == VBLANK_SCANLINE)
        {
            // VBlank start (scanline 241)
            ExecuteVBlankStart();
        }
        else if (_state.Scanline > VBLANK_SCANLINE && _state.Scanline < PRE_RENDER_SCANLINE)
        {
            // VBlank scanlines (242-260)
            ExecuteVBlankScanline();
        }
        else if (_state.Scanline == PRE_RENDER_SCANLINE)
        {
            // Pre-render scanline (261)
            ExecutePreRenderScanline();
        }
    }

    /// <summary>
    /// Execute visible scanline (0-239)
    /// </summary>
    private void ExecuteVisibleScanline()
    {
        // In headless mode, we don't actually render but maintain timing
        // This would be where pixel rendering logic goes in full mode
        
        // Handle sprite evaluation at specific cycles
        if (_state.Cycle == 64)
        {
            // Clear secondary OAM
            Array.Clear(_state.SecondaryOAM, 0, _state.SecondaryOAM.Length);
        }
        else if (_state.Cycle >= 65 && _state.Cycle <= 256)
        {
            // Sprite evaluation happens here
            // In headless mode, we skip the actual evaluation
        }
    }

    /// <summary>
    /// Execute VBlank start (scanline 241)
    /// </summary>
    private void ExecuteVBlankStart()
    {
        if (_state.Cycle == 1)
        {
            // Set VBlank flag
            _state.Status |= PPUStatus.VBlank;
            
            // Generate NMI if enabled
            if (_state.Control.HasFlag(PPUControl.NMIEnable))
            {
                // Request NMI interrupt (would be handled by CPU)
                OnVBlankStarted();
            }
        }
    }

    /// <summary>
    /// Execute VBlank scanline (242-260)
    /// </summary>
    private static void ExecuteVBlankScanline()
    {
        // VBlank period - PPU is idle
        // This is when CPU typically updates PPU memory
    }

    /// <summary>
    /// Execute pre-render scanline (261)
    /// </summary>
    private void ExecutePreRenderScanline()
    {
        if (_state.Cycle == 1)
        {
            // Clear VBlank and sprite 0 hit flags
            _state.Status &= ~(PPUStatus.VBlank | PPUStatus.SpriteZeroHit | PPUStatus.SpriteOverflow);
            OnVBlankEnded();
        }

        // Skip cycle 339 on odd frames when rendering is enabled
        if (_state.OddFrame && _state.Cycle == 339 && IsRenderingEnabled())
        {
            _state.Cycle = 340; // Skip to end of scanline
        }
    }

    /// <summary>
    /// Check if rendering is enabled
    /// </summary>
    /// <returns>True if background or sprite rendering is enabled</returns>
    private bool IsRenderingEnabled()
    {
        return _state.Mask.HasFlag(PPUMask.ShowBackground) || _state.Mask.HasFlag(PPUMask.ShowSprites);
    }

    // PPU Register implementations
    private byte ReadPPUSTATUS()
    {
        var status = (byte)_state.Status;
        
        // Clear VBlank flag after reading
        _state.Status &= ~PPUStatus.VBlank;
        
        // Reset address latch
        _state.WriteToggle = false;
        
        return status;
    }

    private byte ReadOAMDATA()
    {
        return _state.OAM[_state.OAMAddress];
    }

    private byte ReadPPUDATA()
    {
        var value = _memoryMap.ReadByte(_state.VRAMAddress);
        
        // Increment VRAM address
        _state.VRAMAddress += (ushort)(_state.Control.HasFlag(PPUControl.VRAMIncrement) ? 32 : 1);
        
        return value;
    }

    private void WritePPUCTRL(byte value)
    {
        _state.Control = (PPUControl)value;
    }

    private void WritePPUMASK(byte value)
    {
        _state.Mask = (PPUMask)value;
    }

    private void WriteOAMADDR(byte value)
    {
        _state.OAMAddress = value;
    }

    private void WriteOAMDATA(byte value)
    {
        _state.OAM[_state.OAMAddress] = value;
        _state.OAMAddress++;
    }

    private void WritePPUSCROLL(byte value)
    {
        if (!_state.WriteToggle)
        {
            _state.ScrollX = value;
            _state.WriteToggle = true;
        }
        else
        {
            _state.ScrollY = value;
            _state.WriteToggle = false;
        }
    }

    private void WritePPUADDR(byte value)
    {
        if (!_state.WriteToggle)
        {
            _state.VRAMAddress = (ushort)((_state.VRAMAddress & 0x00FF) | (value << 8));
            _state.WriteToggle = true;
        }
        else
        {
            _state.VRAMAddress = (ushort)((_state.VRAMAddress & 0xFF00) | value);
            _state.WriteToggle = false;
        }
    }

    private void WritePPUDATA(byte value)
    {
        _memoryMap.WriteByte(_state.VRAMAddress, value);
        
        // Increment VRAM address
        _state.VRAMAddress += (ushort)(_state.Control.HasFlag(PPUControl.VRAMIncrement) ? 32 : 1);
    }

    private void WriteOAMDMA(byte value)
    {
        // OAM DMA - copy 256 bytes from CPU memory to OAM
        // In a full implementation, this would halt the CPU for 513-514 cycles
        var sourceAddress = (ushort)(value << 8);
        
        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogTrace("OAM DMA from ${Address:X4}", sourceAddress);
        #pragma warning restore CA1848
        
        // For headless mode, we just note the DMA operation
        // Full implementation would copy from CPU memory map
    }

    private void OnVBlankStarted()
    {
        VBlankStarted?.Invoke(this, new VBlankEventArgs(_state.Frame, _state.Scanline));
    }

    private void OnVBlankEnded()
    {
        VBlankEnded?.Invoke(this, new VBlankEventArgs(_state.Frame, _state.Scanline));
    }

    private void OnFrameCompleted()
    {
        FrameCompleted?.Invoke(this, new FrameCompletedEventArgs(_state.Frame, 0, TimeSpan.Zero));
    }
}

/// <summary>
/// Event arguments for VBlank events
/// </summary>
public class VBlankEventArgs : EventArgs
{
    public long Frame { get; }
    public int Scanline { get; }

    public VBlankEventArgs(long frame, int scanline)
    {
        Frame = frame;
        Scanline = scanline;
    }
}
