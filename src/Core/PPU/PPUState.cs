using System;
using EightBitten.Core.Contracts;

namespace EightBitten.Core.PPU;

/// <summary>
/// Complete state of the Picture Processing Unit (PPU) for save/load operations
/// </summary>
public class PPUState : ComponentState
{
    /// <summary>
    /// PPU control register ($2000)
    /// </summary>
    public PPUControl Control { get; set; }

    /// <summary>
    /// PPU mask register ($2001)
    /// </summary>
    public PPUMask Mask { get; set; }

    /// <summary>
    /// PPU status register ($2002)
    /// </summary>
    public PPUStatus Status { get; set; }

    /// <summary>
    /// OAM (Object Attribute Memory) address register ($2003)
    /// </summary>
    public byte OAMAddress { get; set; }

    /// <summary>
    /// PPU scroll position X
    /// </summary>
    public byte ScrollX { get; set; }

    /// <summary>
    /// PPU scroll position Y
    /// </summary>
    public byte ScrollY { get; set; }

    /// <summary>
    /// PPU address register (VRAM address)
    /// </summary>
    public ushort VRAMAddress { get; set; }

    /// <summary>
    /// Temporary VRAM address (used during address writes)
    /// </summary>
    public ushort TempVRAMAddress { get; set; }

    /// <summary>
    /// Fine X scroll (3 bits)
    /// </summary>
    public byte FineX { get; set; }

    /// <summary>
    /// Write toggle for address/scroll registers
    /// </summary>
    public bool WriteToggle { get; set; }

    /// <summary>
    /// PPU data buffer (for buffered reads)
    /// </summary>
    public byte DataBuffer { get; set; }

    /// <summary>
    /// Current scanline (0-261, where 261 is pre-render)
    /// </summary>
    public int Scanline { get; set; }

    /// <summary>
    /// Current cycle within scanline (0-340)
    /// </summary>
    public int Cycle { get; set; }

    /// <summary>
    /// Current frame number
    /// </summary>
    public long Frame { get; set; }

    /// <summary>
    /// Whether this is an odd frame (affects timing)
    /// </summary>
    public bool OddFrame { get; set; }

    /// <summary>
    /// VRAM (Video RAM) - 2KB mirrored to 4KB
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "State class requires arrays for serialization")]
    public byte[] VRAM { get; set; } = new byte[0x1000]; // 4KB

    /// <summary>
    /// OAM (Object Attribute Memory) - 256 bytes for sprites
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "State class requires arrays for serialization")]
    public byte[] OAM { get; set; } = new byte[0x100]; // 256 bytes

    /// <summary>
    /// Palette RAM - 32 bytes
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "State class requires arrays for serialization")]
    public byte[] PaletteRAM { get; set; } = new byte[0x20]; // 32 bytes

    /// <summary>
    /// Secondary OAM for sprite evaluation (32 bytes)
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "State class requires arrays for serialization")]
    public byte[] SecondaryOAM { get; set; } = new byte[0x20]; // 32 bytes

    /// <summary>
    /// Sprite evaluation state
    /// </summary>
    public SpriteEvaluationState SpriteEvaluation { get; set; } = new();

    /// <summary>
    /// Background rendering state
    /// </summary>
    public BackgroundRenderState BackgroundRender { get; set; } = new();

    /// <summary>
    /// NMI (Non-Maskable Interrupt) occurred flag
    /// </summary>
    public bool NMIOccurred { get; set; }

    /// <summary>
    /// NMI output signal state
    /// </summary>
    public bool NMIOutput { get; set; }

    /// <summary>
    /// Previous NMI output state (for edge detection)
    /// </summary>
    public bool PreviousNMIOutput { get; set; }

    public PPUState()
    {
        ComponentName = "PPU";
        Reset();
    }

    /// <summary>
    /// Reset PPU state to power-on values
    /// </summary>
    public void Reset()
    {
        Control = 0;
        Mask = 0;
        Status = 0;
        OAMAddress = 0;
        ScrollX = 0;
        ScrollY = 0;
        VRAMAddress = 0;
        TempVRAMAddress = 0;
        FineX = 0;
        WriteToggle = false;
        DataBuffer = 0;
        
        Scanline = 241; // Start in VBlank
        Cycle = 0;
        Frame = 0;
        OddFrame = false;
        
        // Clear memory
        Array.Clear(VRAM);
        Array.Clear(OAM);
        Array.Clear(PaletteRAM);
        Array.Clear(SecondaryOAM);
        
        SpriteEvaluation.Reset();
        BackgroundRender.Reset();
        
        NMIOccurred = false;
        NMIOutput = false;
        PreviousNMIOutput = false;
    }

    /// <summary>
    /// Create a deep copy of the PPU state
    /// </summary>
    public PPUState Clone()
    {
        var clone = new PPUState
        {
            ComponentName = ComponentName,
            Timestamp = Timestamp,
            CycleCount = CycleCount,
            Control = Control,
            Mask = Mask,
            Status = Status,
            OAMAddress = OAMAddress,
            ScrollX = ScrollX,
            ScrollY = ScrollY,
            VRAMAddress = VRAMAddress,
            TempVRAMAddress = TempVRAMAddress,
            FineX = FineX,
            WriteToggle = WriteToggle,
            DataBuffer = DataBuffer,
            Scanline = Scanline,
            Cycle = Cycle,
            Frame = Frame,
            OddFrame = OddFrame,
            NMIOccurred = NMIOccurred,
            NMIOutput = NMIOutput,
            PreviousNMIOutput = PreviousNMIOutput
        };

        // Deep copy arrays
        Array.Copy(VRAM, clone.VRAM, VRAM.Length);
        Array.Copy(OAM, clone.OAM, OAM.Length);
        Array.Copy(PaletteRAM, clone.PaletteRAM, PaletteRAM.Length);
        Array.Copy(SecondaryOAM, clone.SecondaryOAM, SecondaryOAM.Length);

        clone.SpriteEvaluation = SpriteEvaluation.Clone();
        clone.BackgroundRender = BackgroundRender.Clone();

        return clone;
    }

    public override string ToString()
    {
        return $"PPU Frame:{Frame} Scanline:{Scanline} Cycle:{Cycle} CTRL:${Control:X2} MASK:${Mask:X2} STATUS:${Status:X2}";
    }
}

/// <summary>
/// PPU Control register ($2000) flags
/// </summary>
[Flags]
public enum PPUControl : int
{
    NameTableX = 0x01,      // Base nametable address bit 0
    NameTableY = 0x02,      // Base nametable address bit 1
    VRAMIncrement = 0x04,   // VRAM address increment (0: +1, 1: +32)
    SpriteTable = 0x08,     // Sprite pattern table address
    BackgroundTable = 0x10, // Background pattern table address
    SpriteSize = 0x20,      // Sprite size (0: 8x8, 1: 8x16)
    MasterSlave = 0x40,     // PPU master/slave select (not used on NES)
    NMIEnable = 0x80        // Generate NMI at start of VBlank
}

/// <summary>
/// PPU Mask register ($2001) flags
/// </summary>
[Flags]
public enum PPUMask : int
{
    Grayscale = 0x01,           // Grayscale mode
    ShowBackgroundLeft = 0x02,  // Show background in leftmost 8 pixels
    ShowSpritesLeft = 0x04,     // Show sprites in leftmost 8 pixels
    ShowBackground = 0x08,      // Show background
    ShowSprites = 0x10,         // Show sprites
    EmphasizeRed = 0x20,        // Emphasize red
    EmphasizeGreen = 0x40,      // Emphasize green
    EmphasizeBlue = 0x80        // Emphasize blue
}

/// <summary>
/// PPU Status register ($2002) flags
/// </summary>
[Flags]
public enum PPUStatus : int
{
    SpriteOverflow = 0x20,  // Sprite overflow flag
    SpriteZeroHit = 0x40,   // Sprite 0 hit flag
    VBlank = 0x80           // VBlank flag
}

/// <summary>
/// Sprite evaluation state for cycle-accurate emulation
/// </summary>
public class SpriteEvaluationState
{
    public int SpriteIndex { get; set; }
    public int SecondaryOAMIndex { get; set; }
    public int SpritesFound { get; set; }
    public bool SpriteZeroInRange { get; set; }
    public bool OverflowDetection { get; set; }

    public void Reset()
    {
        SpriteIndex = 0;
        SecondaryOAMIndex = 0;
        SpritesFound = 0;
        SpriteZeroInRange = false;
        OverflowDetection = false;
    }

    public SpriteEvaluationState Clone()
    {
        return new SpriteEvaluationState
        {
            SpriteIndex = SpriteIndex,
            SecondaryOAMIndex = SecondaryOAMIndex,
            SpritesFound = SpritesFound,
            SpriteZeroInRange = SpriteZeroInRange,
            OverflowDetection = OverflowDetection
        };
    }
}

/// <summary>
/// Background rendering state for cycle-accurate emulation
/// </summary>
public class BackgroundRenderState
{
    public ushort NameTableByte { get; set; }
    public ushort AttributeByte { get; set; }
    public ushort PatternLow { get; set; }
    public ushort PatternHigh { get; set; }
    public ulong ShiftRegisterLow { get; set; }
    public ulong ShiftRegisterHigh { get; set; }
    public byte AttributeLatch { get; set; }

    public void Reset()
    {
        NameTableByte = 0;
        AttributeByte = 0;
        PatternLow = 0;
        PatternHigh = 0;
        ShiftRegisterLow = 0;
        ShiftRegisterHigh = 0;
        AttributeLatch = 0;
    }

    public BackgroundRenderState Clone()
    {
        return new BackgroundRenderState
        {
            NameTableByte = NameTableByte,
            AttributeByte = AttributeByte,
            PatternLow = PatternLow,
            PatternHigh = PatternHigh,
            ShiftRegisterLow = ShiftRegisterLow,
            ShiftRegisterHigh = ShiftRegisterHigh,
            AttributeLatch = AttributeLatch
        };
    }
}
