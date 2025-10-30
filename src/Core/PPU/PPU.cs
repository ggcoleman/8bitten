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

    private bool _renderingEnabledLogged;
    // Frame buffer for rendering (256x240 RGBA)
    private bool _ppumaskFirstWriteLogged;
    private int _ppudataInfoLogs;
    private int _paletteWriteLogs;
    private int _nametableWriteLogs;

    private int _ppuaddrInfoLogs;
    private readonly uint[] _frameBuffer = new uint[256 * 240];

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
        {
            // Debug: Log why PPU is not executing
            if (_state.Frame < 3)
            {
                #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                _logger.LogWarning("PPU ExecuteCycle SKIPPED: Enabled={Enabled}, Initialized={Initialized}", IsEnabled, _isInitialized);
                #pragma warning restore CA1848
            }
            return 1;
        }

        // Debug: Log PPU execution for first few frames
        if (_state.Frame < 3 && _state.Cycle % 100 == 0)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogDebug("PPU ExecuteCycle: Frame {Frame}, Scanline {Scanline}, Cycle {Cycle}",
                _state.Frame, _state.Scanline, _state.Cycle);
            #pragma warning restore CA1848
        }

        // Debug: Log when approaching VBlank - only once per scanline
        if (_state.Cycle == 0 && _state.Scanline >= 240 && _state.Scanline <= 245)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogDebug("*** PPU APPROACHING VBLANK: Frame {Frame}, Scanline {Scanline}, Cycle {Cycle}, CYCLES_PER_SCANLINE={MaxCycle} ***",
                _state.Frame, _state.Scanline, _state.Cycle, CYCLES_PER_SCANLINE);
            #pragma warning restore CA1848
        }

        // Debug: Log every scanline transition for first few frames
        if (_state.Frame < 3 && _state.Cycle == 0)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogDebug("PPU Scanline transition: Frame {Frame}, Scanline {Scanline}",
                _state.Frame, _state.Scanline);
            #pragma warning restore CA1848
        }

        try
        {
            // Execute PPU cycle based on current scanline and cycle
            ExecutePPUCycle();

            // Advance timing
            _state.Cycle++;

            // Debug: Log cycle advancement for scanline 245
            if (_state.Scanline == 245 && (_state.Cycle >= 335 || _state.Cycle % 50 == 0))
            {
                #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                _logger.LogDebug("*** PPU CYCLE ADVANCE: Scanline 245, Cycle {Cycle}, CYCLES_PER_SCANLINE={MaxCycle}, Will advance? {WillAdvance} ***",
                    _state.Cycle, CYCLES_PER_SCANLINE, _state.Cycle >= CYCLES_PER_SCANLINE);
                #pragma warning restore CA1848
            }

            if (_state.Cycle >= CYCLES_PER_SCANLINE)
            {
                _state.Cycle = 0;
                var oldScanline = _state.Scanline;
                _state.Scanline++;

                // Debug: Log scanline advancement for scanline 245
                if (oldScanline == 245)
                {
                    #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                    _logger.LogDebug("*** PPU SCANLINE ADVANCE: Scanline {OldScanline} -> {NewScanline}, Cycle reset to 0 ***",
                        oldScanline, _state.Scanline);
                    #pragma warning restore CA1848
                }

                // Debug: Log critical scanline transitions
                if (oldScanline == 240 || oldScanline == 241 || _state.Scanline == 241)
                {
                    #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                    _logger.LogDebug("*** PPU SCANLINE ADVANCE: Frame {Frame}, {OldScanline} -> {NewScanline} ***",
                        _state.Frame, oldScanline, _state.Scanline);
                    #pragma warning restore CA1848
                }

                if (_state.Scanline >= SCANLINES_PER_FRAME)
                {
                    _state.Scanline = 0;
                    _state.Frame++;
                    _state.OddFrame = !_state.OddFrame;

                    // Log first few frames to verify rendering
                    if (_state.Frame <= 5)
                    {
                        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                        _logger.LogInformation("PPU Frame {Frame} completed - BG Enabled: {BG}, Sprites Enabled: {Sprites}",
                            _state.Frame, _state.Mask.HasFlag(PPUMask.ShowBackground), _state.Mask.HasFlag(PPUMask.ShowSprites));
                        #pragma warning restore CA1848
                    }

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
        // Debug: Log PPU synchronization for first few cycles
        if (masterCycles <= 10)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogDebug("PPU SynchronizeClock called: MasterCycle={Cycle}, Enabled={Enabled}, Initialized={Initialized}",
                masterCycles, IsEnabled, _isInitialized);
            #pragma warning restore CA1848
        }

        // Debug: Log PPU synchronization - first 10 calls and then every 1000 master cycles
        if (masterCycles <= 10 || masterCycles % 1000 == 0)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogDebug("*** PPU SynchronizeClock: MasterCycle={Cycle}, PPU Frame={Frame}, Scanline={Scanline}, Cycle={PPUCycle}, Enabled={Enabled}, Initialized={Initialized} ***",
                masterCycles, _state.Frame, _state.Scanline, _state.Cycle, IsEnabled, _isInitialized);
            #pragma warning restore CA1848
        }

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
    /// Peek a byte from PPU address space without side effects (for rendering)
    /// </summary>
    /// <param name="address">PPU address ($0000-$3FFF)</param>
    /// <returns>Byte value at address</returns>
    internal byte PeekMemory(ushort address) => _memoryMap.ReadByte(address);


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
        // Debug: Log every scanline transition to see if we're missing scanline 241
        if (_state.Cycle == 0 && (_state.Scanline >= 240 && _state.Scanline <= 245))
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogDebug("*** PPU SCANLINE TRANSITION: Frame {Frame}, Scanline {Scanline}, Cycle {Cycle} ***",
                _state.Frame, _state.Scanline, _state.Cycle);
            #pragma warning restore CA1848
        }

        // Debug: Log which branch we're taking for scanlines around VBlank
        if (_state.Cycle == 0 && _state.Scanline >= 240 && _state.Scanline <= 245)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogDebug("*** PPU SCANLINE BRANCH CHECK: Scanline={Scanline}, VBLANK_SCANLINE={VBlank}, Condition: Scanline==VBlank? {Equal} ***",
                _state.Scanline, VBLANK_SCANLINE, _state.Scanline == VBLANK_SCANLINE);
            #pragma warning restore CA1848
        }

        // Handle different scanline types
        if (_state.Scanline < VISIBLE_SCANLINES)
        {
            // Visible scanlines (0-239)
            ExecuteVisibleScanline();
        }
        else if (_state.Scanline == VISIBLE_SCANLINES)
        {
            // Post-render scanline (240) - idle
            // No rendering; just idle timing
        }
        else if (_state.Scanline == VBLANK_SCANLINE)
        {
            // VBlank start (scanline 241)
            if (_state.Cycle == 1)
            {
                #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                _logger.LogDebug("*** PPU EXECUTING VBLANK START: Frame {Frame}, Scanline {Scanline}, Cycle {Cycle} ***",
                    _state.Frame, _state.Scanline, _state.Cycle);
                #pragma warning restore CA1848
            }
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
        else
        {
            // Debug: Log unexpected scanline values
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogWarning("*** PPU UNEXPECTED SCANLINE: Frame {Frame}, Scanline {Scanline}, Cycle {Cycle} ***",
                _state.Frame, _state.Scanline, _state.Cycle);
            #pragma warning restore CA1848
        }
    }

    /// <summary>
    /// Execute visible scanline (0-239)
    /// </summary>
    private void ExecuteVisibleScanline()
    {
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

        // BASIC PIXEL RENDERING - Generate test pattern if not headless
        if (!_isHeadless && _state.Cycle >= 1 && _state.Cycle <= 256)
        {
            RenderPixel(_state.Cycle - 1, _state.Scanline);
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

            // Log for VBlank flag setting (throttled)
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            if (_state.Frame <= 3 || (_state.Frame % 60) == 0)
            {
                _logger.LogInformation("*** PPU VBLANK FLAG SET: Frame {Frame}, Status = 0x{Status:X2}, VBlank bit = {VBlank} - MARIO SHOULD DETECT THIS! ***",
                    _state.Frame, (byte)_state.Status, (_state.Status & PPUStatus.VBlank) != 0 ? "SET" : "CLEAR");
            }
            else
            {
                _logger.LogDebug("*** PPU VBLANK FLAG SET: Frame {Frame}, Status = 0x{Status:X2}, VBlank bit = {VBlank} - MARIO SHOULD DETECT THIS! ***",
                    _state.Frame, (byte)_state.Status, (_state.Status & PPUStatus.VBlank) != 0 ? "SET" : "CLEAR");
            }
            #pragma warning restore CA1848

            // Debug log for first few VBlanks
            if (_state.Frame < 5)
            {
                #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                _logger.LogDebug("PPU VBlank started: Frame {Frame}, Status now 0x{Status:X2}, NMI Enabled: {NMI}",
                    _state.Frame, (byte)_state.Status, _state.Control.HasFlag(PPUControl.NMIEnable));
                #pragma warning restore CA1848
            }

            // Generate NMI if enabled OR for the first few frames to help games initialize
            bool shouldTriggerNMI = _state.Control.HasFlag(PPUControl.NMIEnable) || _state.Frame < 3;

            if (shouldTriggerNMI)
            {
                if (_state.Control.HasFlag(PPUControl.NMIEnable))
                {
                    #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                    _logger.LogDebug("PPU triggering NMI for frame {Frame} (NMI enabled by game)", _state.Frame);
                    #pragma warning restore CA1848
                }
                else
                {
                    #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                    _logger.LogDebug("PPU triggering NMI for frame {Frame} (STARTUP - NMI disabled but forcing for initialization)", _state.Frame);
                    #pragma warning restore CA1848
                }

                // Request NMI interrupt
                OnVBlankStarted();
            }
            else
            {
                #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                _logger.LogDebug("PPU VBlank started but NMI disabled - no interrupt for frame {Frame}", _state.Frame);
                #pragma warning restore CA1848
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
        var vblankSet = _state.Status.HasFlag(PPUStatus.VBlank);


        // Special logging when VBlank flag is read as SET
        if (vblankSet)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogDebug("*** PPU STATUS READ WITH VBLANK SET: Mario should proceed! Status = 0x{Status:X2}, Frame = {Frame} ***",
                status, _state.Frame);
            #pragma warning restore CA1848
        }

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
        var previous = _state.Control;
        _state.Control = (PPUControl)value;

        // If NMI just became enabled while VBlank is already set, trigger an immediate NMI
        if (!previous.HasFlag(PPUControl.NMIEnable) &&
            _state.Control.HasFlag(PPUControl.NMIEnable) &&
            _state.Status.HasFlag(PPUStatus.VBlank))
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogDebug("PPU: NMI enabled mid-VBlank; requesting immediate NMI (Frame {Frame})", _state.Frame);
            #pragma warning restore CA1848
            OnVBlankStarted();
        }

        // Reduce logging spam - only log first few writes
        if (_state.Frame < 3)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogInformation("PPU CTRL written: 0x{Value:X2} (Control: {Control})", value, _state.Control);
            #pragma warning restore CA1848
        }
    }

    private void WritePPUMASK(byte value)
    {
        var previousMask = _state.Mask;
        _state.Mask = (PPUMask)value;

        // Tiny one-time Info when rendering becomes enabled via PPUMASK
        if (!_renderingEnabledLogged)
        {
            bool wasEnabled = previousMask.HasFlag(PPUMask.ShowBackground) || previousMask.HasFlag(PPUMask.ShowSprites);
            bool isEnabled = _state.Mask.HasFlag(PPUMask.ShowBackground) || _state.Mask.HasFlag(PPUMask.ShowSprites);
            if (!wasEnabled && isEnabled)
            {
                #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                _logger.LogInformation("PPU rendering enabled via PPUMASK (BG={BG}, Sprites={Sprites}, Mask=0x{Mask:X2})",
                    _state.Mask.HasFlag(PPUMask.ShowBackground), _state.Mask.HasFlag(PPUMask.ShowSprites), (byte)_state.Mask);
                #pragma warning restore CA1848
                _renderingEnabledLogged = true;
            }
        }

        // Log first PPUMASK write regardless of frame, then stay quiet
        if (!_ppumaskFirstWriteLogged)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogInformation("PPU MASK written: 0x{Value:X2} (Mask: {Mask})", value, _state.Mask);
            #pragma warning restore CA1848
            _ppumaskFirstWriteLogged = true;
        }
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

        // Tiny info for first few address writes to help debug nametable/palette writes
        if (_ppuaddrInfoLogs < 2)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogInformation("PPU ADDR set to ${Address:X4}", _state.VRAMAddress);
            #pragma warning restore CA1848
            _ppuaddrInfoLogs++;
        }
    }

    private void WritePPUDATA(byte value)
    {
        // Log first couple of writes regardless of frame to confirm VRAM activity
        if (_ppudataInfoLogs < 2)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogInformation("PPU DATA written: 0x{Value:X2} to address 0x{Address:X4}", value, _state.VRAMAddress);
            #pragma warning restore CA1848
            _ppudataInfoLogs++;
        }

        // Additional targeted logs: palette and nametable writes (first few)
        if (_state.VRAMAddress >= 0x3F00 && _state.VRAMAddress <= 0x3F1F && _paletteWriteLogs < 16)
        {
            #pragma warning disable CA1848
            _logger.LogInformation("PPU PALETTE write: 0x{Value:X2} -> ${Addr:X4}", value, _state.VRAMAddress);
            #pragma warning restore CA1848
            _paletteWriteLogs++;
        }
        else if (_state.VRAMAddress >= 0x2000 && _state.VRAMAddress <= 0x2FFF && _nametableWriteLogs < 16)
        {
            #pragma warning disable CA1848
            _logger.LogInformation("PPU NAMETABLE write: 0x{Value:X2} -> ${Addr:X4}", value, _state.VRAMAddress);
            #pragma warning restore CA1848
            _nametableWriteLogs++;
        }

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
        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogDebug("PPU OnVBlankStarted called - Frame {Frame}, Subscribers: {Count}",
            _state.Frame, VBlankStarted?.GetInvocationList()?.Length ?? 0);
        #pragma warning restore CA1848

        VBlankStarted?.Invoke(this, new VBlankEventArgs(_state.Frame, _state.Scanline));
    }

    private void OnVBlankEnded()
    {
        VBlankEnded?.Invoke(this, new VBlankEventArgs(_state.Frame, _state.Scanline));
    }

    private void OnFrameCompleted()
    {
        // Use the correct FrameCompletedEventArgs constructor
        // (long frameNumber, long cycleCount, TimeSpan frameTime)
        var frameTime = TimeSpan.FromMilliseconds(16.67); // ~60 FPS
        var cycleCount = _state.Frame * 29780; // Approximate cycles per frame

        var eventArgs = new FrameCompletedEventArgs(_state.Frame, cycleCount, frameTime);
        FrameCompleted?.Invoke(this, eventArgs);

        // Log frame completion with rendering info
        if (!_isHeadless)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogDebug("PPU Frame {Frame} completed with rendering enabled", _state.Frame);
            #pragma warning restore CA1848
        }
    }

    /// <summary>
    /// Generate a test frame with basic pattern for debugging
    /// </summary>
    private static byte[] GenerateTestFrame()
    {
        const int width = 256;
        const int height = 240;
        var frameData = new byte[width * height * 4]; // RGBA

        // Generate a simple test pattern
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var index = (y * width + x) * 4;

                // Create a simple gradient pattern
                var r = (byte)(x % 256);
                var g = (byte)(y % 256);
                var b = (byte)((x + y) % 256);
                var a = (byte)255;

                frameData[index] = r;     // Red
                frameData[index + 1] = g; // Green
                frameData[index + 2] = b; // Blue
                frameData[index + 3] = a; // Alpha
            }
        }

        return frameData;
    }

    /// <summary>
    /// Render a single pixel to the frame buffer
    /// </summary>
    /// <param name="x">X coordinate (0-255)</param>
    /// <param name="y">Y coordinate (0-239)</param>
    private void RenderPixel(int x, int y)
    {
        if (x < 0 || x >= 256 || y < 0 || y >= 240)
            return;

        var index = y * 256 + x;

        // Generate a simple test pattern for now
        // This will be replaced with actual NES rendering logic
        uint color;

        if (IsRenderingEnabled())
        {
            // Create a colorful test pattern when rendering is enabled
            var r = (byte)((x * 255) / 256);
            var g = (byte)((y * 255) / 240);
            var b = (byte)(((x + y) * 255) / (256 + 240));
            color = 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | b; // ARGB format
        }
        else
        {
            // Black when rendering is disabled
            color = 0xFF000000; // Black
        }

        _frameBuffer[index] = color;
    }

    /// <summary>
    /// Get the current frame buffer
    /// </summary>
    /// <returns>Frame buffer as ARGB pixel data</returns>
    public ReadOnlySpan<uint> GetFrameBuffer()
    {
        return _frameBuffer.AsSpan();
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
