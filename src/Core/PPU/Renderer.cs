using System;
using Microsoft.Extensions.Logging;

namespace EightBitten.Core.PPU;

/// <summary>
/// PPU graphics renderer for generating NES video output
/// Handles scanline rendering, sprite processing, and frame buffer generation
/// </summary>
public sealed class Renderer : IDisposable
{
    private readonly ILogger<Renderer> _logger;
    private readonly PictureProcessingUnit _ppu;
    private readonly byte[] _frameBuffer;
    private readonly uint[] _palette;
    private bool _disposed;
    private bool _firstVRAMProbeLogged;


    /// <summary>
    /// NES screen width in pixels
    /// </summary>
    public const int ScreenWidth = 256;

    /// <summary>
    /// NES screen height in pixels
    /// </summary>
    public const int ScreenHeight = 240;

    /// <summary>
    /// Gets whether the renderer is initialized
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Gets the current frame number
    /// </summary>
    public long FrameNumber { get; private set; }

    /// <summary>
    /// Initializes a new instance of the Renderer class
    /// </summary>
    /// <param name="ppu">PPU instance to render from</param>
    /// <param name="logger">Logger for diagnostic output</param>
    /// <exception cref="ArgumentNullException">Thrown when ppu or logger is null</exception>
    public Renderer(PictureProcessingUnit ppu, ILogger<Renderer> logger)
    {
        _ppu = ppu ?? throw new ArgumentNullException(nameof(ppu));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _frameBuffer = new byte[ScreenWidth * ScreenHeight * 4]; // RGBA format
        _palette = new uint[64]; // NES palette colors

        InitializePalette();
        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogDebug("PPU Renderer created");
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Initializes the renderer
    /// </summary>
    /// <returns>True if initialization succeeded, false otherwise</returns>
    public bool Initialize()
    {
        if (_disposed)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogError("Cannot initialize disposed renderer");
            #pragma warning restore CA1848
            return false;
        }

        try
        {
            // Clear frame buffer
            Array.Clear(_frameBuffer, 0, _frameBuffer.Length);
            FrameNumber = 0;

            IsInitialized = true;
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogInformation("PPU Renderer initialized");
            #pragma warning restore CA1848
            return true;
        }
        #pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
        #pragma warning restore CA1031
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogError(ex, "Failed to initialize PPU renderer");
            #pragma warning restore CA1848
            return false;
        }
    }

    /// <summary>
    /// Renders a complete frame from PPU state
    /// </summary>
    /// <returns>Frame buffer data in RGBA format</returns>
    public byte[] RenderFrame()
    {
        if (!IsInitialized || _disposed)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogWarning("Renderer not initialized or disposed");
            #pragma warning restore CA1848
            return new byte[ScreenWidth * ScreenHeight * 4];
        }

        try
        {
            // Clear frame buffer with background color
            var backgroundColor = GetBackgroundColor();
            ClearFrameBuffer(backgroundColor);

            // Get PPU state for rendering
            var ppuState = (PPUState)_ppu.GetState();

            // Check if PPU rendering is enabled (treat left-edge enable as enabled for debug)
            // Debug mode: treat left-edge bits as enabling too so we can see BG early
            bool ppuRenderingEnabled = (ppuState.Mask & (PPUMask.ShowBackground | PPUMask.ShowSprites | PPUMask.ShowBackgroundLeft | PPUMask.ShowSpritesLeft)) != 0;
            // One-time VRAM probe to verify content is present and palettes initialized
            if (!_firstVRAMProbeLogged && FrameNumber >= 6)
            {
                byte nt2000 = _ppu.PeekMemory(0x2000);
                byte nt2400 = _ppu.PeekMemory(0x2400);
                byte nt2800 = _ppu.PeekMemory(0x2800);
                byte nt2C00 = _ppu.PeekMemory(0x2C00);

                byte p0 = _ppu.PeekMemory(0x3F00);
                byte p1 = _ppu.PeekMemory(0x3F01);
                byte p2 = _ppu.PeekMemory(0x3F02);
                byte p3 = _ppu.PeekMemory(0x3F03);

                // Sample CHR for tile at $2400 using current BG pattern table
                ushort patternBaseProbe = (ushort)(((ppuState.Control & PPUControl.BackgroundTable) != 0) ? 0x1000 : 0x0000);
                byte tileIdxProbe = nt2400;
                ushort patAddrProbe = (ushort)(patternBaseProbe + tileIdxProbe * 16);
                byte chrLo = _ppu.PeekMemory(patAddrProbe);
                byte chrHi = _ppu.PeekMemory((ushort)(patAddrProbe + 8));

                #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                _logger.LogInformation("Renderer VRAM probe: Mask=0x{Mask:X2} Ctrl=0x{Ctrl:X2} NT[$2000]=$\n{NT2000:X2} $2400=$\n{NT2400:X2} $2800=$\n{NT2800:X2} $2C00=$\n{NT2C00:X2} Pal0..3=[${P0:X2},${P1:X2},${P2:X2},${P3:X2}] Tile@2400=$\n{T:X2} CHR(lo,hi)=[${Lo:X2},${Hi:X2}]",
                    (byte)ppuState.Mask, (byte)ppuState.Control, nt2000, nt2400, nt2800, nt2C00, p0, p1, p2, p3, tileIdxProbe, chrLo, chrHi);
                #pragma warning restore CA1848

                // Also probe opposite pattern table for the same tile index (row 0)
                ushort altBaseProbe = (ushort)(patternBaseProbe == 0x0000 ? 0x1000 : 0x0000);
                ushort altAddrProbe = (ushort)(altBaseProbe + tileIdxProbe * 16);
                byte chrLoAlt = _ppu.PeekMemory(altAddrProbe);
                byte chrHiAlt = _ppu.PeekMemory((ushort)(altAddrProbe + 8));
                #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                _logger.LogInformation("Renderer VRAM probe (alt table): AltBase=0x{AltBase:X4} Tile@2400=$\n{T:X2} CHR(lo,hi)=[${LoA:X2},${HiA:X2}]",
                    altBaseProbe, tileIdxProbe, chrLoAlt, chrHiAlt);
                #pragma warning restore CA1848


                _firstVRAMProbeLogged = true;
            }

                // Debug fallback: if VRAM looks initialized, allow rendering even if PPUMASK bits are unset
                if (!ppuRenderingEnabled && FrameNumber >= 6)
                {
                    byte ntAny = (byte)(_ppu.PeekMemory(0x2000) | _ppu.PeekMemory(0x2400) | _ppu.PeekMemory(0x2800) | _ppu.PeekMemory(0x2C00));
                    byte palAny = (byte)(_ppu.PeekMemory(0x3F00) | _ppu.PeekMemory(0x3F01) | _ppu.PeekMemory(0x3F02) | _ppu.PeekMemory(0x3F03));
                    if ((ntAny | palAny) != 0)
                    {
                        ppuRenderingEnabled = true; // surface real tiles instead of test pattern to aid debug
                    }
                }



            // Debug: Log PPU state every 10 frames to see what Mario is doing (reduced from 60)
            if (FrameNumber % 10 == 0)
            {
                #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                _logger.LogDebug("Renderer: Frame {Frame} - PPU Mask: 0x{Mask:X2}, Control: 0x{Control:X2}, Status: 0x{Status:X2}, Rendering Enabled: {Enabled}",
                    FrameNumber, (byte)ppuState.Mask, (byte)ppuState.Control, (byte)ppuState.Status, ppuRenderingEnabled);
                #pragma warning restore CA1848
            }

            if (ppuRenderingEnabled && FrameNumber > 5) // Give Mario time to initialize (reduced from 60)
            {
                // Render actual game graphics when PPU rendering is enabled
            }
            else if (FrameNumber <= 5)
            {
                // Show initialization pattern for first few frames
                RenderInitializationPattern();
            }
            else if (!ppuRenderingEnabled)
            {
                // Show test pattern when PPU rendering is disabled (Mario hasn't enabled it yet)
                RenderTestPattern();
            }
            else
            {
                // Show test pattern when PPU rendering is not enabled
                DrawLargeTestPattern();

                // Debug: Log why we're showing test pattern
                if (FrameNumber % 60 == 0)
                {
                    #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                    _logger.LogDebug("Renderer: Frame {Frame} - Showing test pattern. PPU Enabled: {Enabled}, Mask: 0x{Mask:X2}, Control: 0x{Control:X2}",
                        FrameNumber, ppuRenderingEnabled, (byte)ppuState.Mask, (byte)ppuState.Control);
                    #pragma warning restore CA1848
                }
            }

            // The PPU state was already retrieved above, now render based on it
            if (ppuRenderingEnabled && FrameNumber > 5)
            {
                // Treat left-edge bits as enabled for minimal debug rendering
                bool bgEnabled = (ppuState.Mask & (PPUMask.ShowBackground | PPUMask.ShowBackgroundLeft)) != 0;
                bool sprEnabled = (ppuState.Mask & (PPUMask.ShowSprites | PPUMask.ShowSpritesLeft)) != 0;

                if (bgEnabled)
                {
                    RenderBackground(ppuState);
                }

                if (sprEnabled)
                {
                    RenderSprites(ppuState);
                }
            }

            // Temporary: draw a small test overlay for first ~3 seconds to verify rendering pipeline
            if (FrameNumber < 180)
            {
                CreateTestPattern();
            }

            FrameNumber++;

            // Return copy of frame buffer
            var result = new byte[_frameBuffer.Length];
            Array.Copy(_frameBuffer, result, _frameBuffer.Length);
            return result;
        }
        #pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
        #pragma warning restore CA1031
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogError(ex, "Error rendering frame {FrameNumber}", FrameNumber);
            #pragma warning restore CA1848
            return new byte[ScreenWidth * ScreenHeight * 4];
        }
    }

    /// <summary>
    /// Renders a single scanline
    /// </summary>
    /// <param name="scanline">Scanline number (0-239)</param>
    public void RenderScanline(int scanline)
    {
        if (!IsInitialized || _disposed || scanline < 0 || scanline >= ScreenHeight)
        {
            return;
        }

        try
        {
            // Get PPU state for rendering
            var ppuState = (PPUState)_ppu.GetState();

            // Render background for this scanline
            if ((ppuState.Mask & PPUMask.ShowBackground) != 0)
            {
                RenderBackgroundScanline(scanline, ppuState);
            }

            // Render sprites for this scanline
            if ((ppuState.Mask & PPUMask.ShowSprites) != 0)
            {
                RenderSpritesScanline(scanline, ppuState);
            }
        }
        #pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
        #pragma warning restore CA1031
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogError(ex, "Error rendering scanline {Scanline}", scanline);
            #pragma warning restore CA1848
        }
    }

    /// <summary>
    /// Gets the current frame buffer
    /// </summary>
    /// <returns>Copy of the current frame buffer</returns>
    public byte[] GetFrameBuffer()
    {
        var result = new byte[_frameBuffer.Length];
        Array.Copy(_frameBuffer, result, _frameBuffer.Length);
        return result;
    }

    /// <summary>
    /// Initializes the NES color palette
    /// </summary>
    private void InitializePalette()
    {
        // Standard NES palette (NTSC)
        var nesColors = new uint[]
        {
            0xFF545454, 0xFF001E74, 0xFF081090, 0xFF300088, 0xFF440064, 0xFF5C0030, 0xFF540400, 0xFF3C1800,
            0xFF202A00, 0xFF083A00, 0xFF004000, 0xFF003C00, 0xFF00323C, 0xFF000000, 0xFF000000, 0xFF000000,
            0xFF989698, 0xFF084CC4, 0xFF3032EC, 0xFF5C1EE4, 0xFF8814B0, 0xFFA01464, 0xFF982220, 0xFF783C00,
            0xFF545A00, 0xFF287200, 0xFF087C00, 0xFF007628, 0xFF006678, 0xFF000000, 0xFF000000, 0xFF000000,
            0xFFECEEEC, 0xFF4C9AEC, 0xFF787CEC, 0xFFB062EC, 0xFFE454EC, 0xFFEC58B4, 0xFFEC6A64, 0xFFD48820,
            0xFFA0AA00, 0xFF74C400, 0xFF4CD020, 0xFF38CC6C, 0xFF38B4CC, 0xFF3C3C3C, 0xFF000000, 0xFF000000,
            0xFFECEEEC, 0xFFA8CCEC, 0xFFBCBCEC, 0xFFD4B2EC, 0xFFECAEEC, 0xFFECAED4, 0xFFECB4B0, 0xFFE4C490,
            0xFFCCD278, 0xFFB4DE78, 0xFFA8E290, 0xFF98E2B4, 0xFF98D8D8, 0xFFA0A2A0, 0xFF000000, 0xFF000000
        };

        Array.Copy(nesColors, _palette, Math.Min(nesColors.Length, _palette.Length));
    }

    /// <summary>
    /// Gets the background color from PPU state
    /// </summary>
    /// <returns>Background color as RGBA uint</returns>
    private uint GetBackgroundColor()
    {
        // Use palette index 0 as background color for now
        // In a full implementation, this would read from PPU palette memory
        return _palette[0];
    }

    /// <summary>
    /// Clears the frame buffer with specified color
    /// </summary>
    /// <param name="color">Color to clear with (RGBA)</param>
    private void ClearFrameBuffer(uint color)
    {
        var r = (byte)(color >> 16);
        var g = (byte)(color >> 8);
        var b = (byte)color;
        var a = (byte)(color >> 24);

        for (int i = 0; i < _frameBuffer.Length; i += 4)
        {
            _frameBuffer[i] = r;     // R
            _frameBuffer[i + 1] = g; // G
            _frameBuffer[i + 2] = b; // B
            _frameBuffer[i + 3] = a; // A
        }
    }

    /// <summary>
    /// Renders the background layer
    /// </summary>
    /// <param name="ppuState">Current PPU state</param>
    private void RenderBackground(PPUState ppuState)
    {
        for (int y = 0; y < ScreenHeight; y++)
        {
            RenderBackgroundScanline(y, ppuState);
        }
    }

    /// <summary>
    /// Renders background for a single scanline
    /// </summary>
    /// <param name="scanline">Scanline to render</param>
    /// <param name="ppuState">Current PPU state</param>
    private void RenderBackgroundScanline(int scanline, PPUState ppuState)
    {
        // Minimal background renderer: decode nametable + CHR bitplanes.
        // Adds: attribute coloring and nametable fallback ($2000/$2400/$2800/$2C00).
        int scrollX = ppuState.ScrollX;
        int scrollY = ppuState.ScrollY;

        // Select preferred base nametable from PPUCTRL
        int nameTableIndex = 0;
        if ((ppuState.Control & PPUControl.NameTableX) != 0) nameTableIndex |= 1;
        if ((ppuState.Control & PPUControl.NameTableY) != 0) nameTableIndex |= 2;
        ushort preferredBase = (ushort)(0x2000 + (nameTableIndex * 0x400));

        // Other bases to try if preferred has blank tile index
        ushort[] ntBases = preferredBase switch
        {
            0x2000 => new ushort[] { 0x2000, 0x2400, 0x2800, 0x2C00 },
            0x2400 => new ushort[] { 0x2400, 0x2000, 0x2800, 0x2C00 },
            0x2800 => new ushort[] { 0x2800, 0x2C00, 0x2000, 0x2400 },
            _       => new ushort[] { 0x2C00, 0x2800, 0x2400, 0x2000 }
        };

        // Select background pattern table from PPUCTRL
        ushort patternBase = (ushort)(((ppuState.Control & PPUControl.BackgroundTable) != 0) ? 0x1000 : 0x0000);

        for (int x = 0; x < ScreenWidth; x++)
        {
            int sx = (x + scrollX) & 0xFF;      // wrap horizontally
            int sy = (scanline + scrollY) % 240; // visible scanlines only

            int tileX = sx >> 3; // /8
            int tileY = sy >> 3; // /8
            int fineX = sx & 0x7;
            int fineY = sy & 0x7;

            byte tileIndex = 0;
            ushort baseUsed = preferredBase;

            // Try preferred base, then fallbacks if tile index is 0
            for (int i = 0; i < ntBases.Length; i++)
            {
                ushort ntBase = ntBases[i];
                ushort nameAddrTry = (ushort)(ntBase + tileY * 32 + tileX);
                byte idx = _ppu.PeekMemory(nameAddrTry);
                if (idx != 0 || i == ntBases.Length - 1)
                {
                    tileIndex = idx;
                    baseUsed = ntBase;
                    break;
                }
            }

            // Fetch pattern bitplanes for this tile row
            ushort patternAddr = (ushort)(patternBase + (tileIndex * 16) + fineY);
            byte plane0 = _ppu.PeekMemory(patternAddr);
            byte plane1 = _ppu.PeekMemory((ushort)(patternAddr + 8));
            // Debug fallback: if this tile row decodes to all zero and tile index is non-zero,
            // try the opposite pattern table (some ROMs may expect BG table opposite early on)
            if (plane0 == 0 && plane1 == 0 && tileIndex != 0)
            {
                ushort altBase = (ushort)(patternBase == 0x0000 ? 0x1000 : 0x0000);
                ushort altAddr = (ushort)(altBase + (tileIndex * 16) + fineY);
                byte altLo = _ppu.PeekMemory(altAddr);
                byte altHi = _ppu.PeekMemory((ushort)(altAddr + 8));
                if ((altLo | altHi) != 0)
                {
                    patternBase = altBase;
                    patternAddr = altAddr;
                    plane0 = altLo;
                    plane1 = altHi;
                }
            }

            int bit = 7 - fineX;
            int b0 = (plane0 >> bit) & 1;
            int b1 = (plane1 >> bit) & 1;
            int pix = (b1 << 1) | b0; // 0..3

            // Attribute coloring (2-bit palette select per 4x4 tile quadrant)
            ushort attrAddr = (ushort)(baseUsed + 0x3C0 + ((tileY >> 2) * 8) + (tileX >> 2));
            byte attrByte = _ppu.PeekMemory(attrAddr);
            int shift = ((tileY & 0x02) << 1) | (tileX & 0x02); // 0,2,4,6
            int palSel = (attrByte >> shift) & 0x03;

            // Palette index: background palettes at $3F00 + (palSel*4) + pix
            ushort palAddr = (ushort)(0x3F00 + (palSel << 2) + pix);
            byte colorIndex = _ppu.PeekMemory(palAddr);
            byte effectiveIndex = (byte)(colorIndex & 0x3F);
            if (effectiveIndex == 0 && pix != 0)
            {
                effectiveIndex = (byte)pix; // fallback: show 1..3 distinctly before palette init
            }
            uint rgba = _palette[effectiveIndex];

            SetPixel(x, scanline, rgba);
        }
    }

    /// <summary>
    /// Renders the sprite layer
    /// </summary>
    /// <param name="ppuState">Current PPU state</param>
    private static void RenderSprites(PPUState ppuState)
    {
        for (int y = 0; y < ScreenHeight; y++)
        {
            RenderSpritesScanline(y, ppuState);
        }
    }

    /// <summary>
    /// Renders sprites for a single scanline
    /// </summary>
    /// <param name="scanline">Scanline to render</param>
    /// <param name="ppuState">Current PPU state</param>
    private static void RenderSpritesScanline(int scanline, PPUState ppuState)
    {
        // For now, skip sprite rendering as it requires OAM data access
        // In a full implementation, this would read from OAM and render sprites
        // var sprites = GetSpritesForScanline(scanline, ppuState);
        // foreach (var sprite in sprites)
        // {
        //     RenderSprite(sprite, scanline);
        // }
    }

    /// <summary>
    /// Renders a single sprite
    /// </summary>
    /// <param name="sprite">Sprite data</param>
    /// <param name="scanline">Current scanline</param>
    private void RenderSprite(SpriteData sprite, int scanline)
    {
        var spriteY = scanline - sprite.Y;
        if (spriteY < 0 || spriteY >= sprite.Height)
        {
            return;
        }

        for (int x = 0; x < sprite.Width; x++)
        {
            var screenX = sprite.X + x;
            if (screenX < 0 || screenX >= ScreenWidth)
            {
                continue;
            }

            var pixelColor = sprite.GetPixel(x, spriteY);
            if (pixelColor != 0) // Transparent pixel
            {
                var paletteColor = _palette[pixelColor & 0x3F];
                SetPixel(screenX, scanline, paletteColor);
            }
        }
    }

    /// <summary>
    /// Sets a pixel in the frame buffer
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="color">Color as RGBA uint</param>
    private void SetPixel(int x, int y, uint color)
    {
        if (x < 0 || x >= ScreenWidth || y < 0 || y >= ScreenHeight)
        {
            return;
        }

        var index = (y * ScreenWidth + x) * 4;
        _frameBuffer[index] = (byte)(color >> 16);     // R
        _frameBuffer[index + 1] = (byte)(color >> 8); // G
        _frameBuffer[index + 2] = (byte)color;        // B
        _frameBuffer[index + 3] = (byte)(color >> 24); // A
    }

    /// <summary>
    /// Draws a large, obvious test pattern to verify rendering
    /// </summary>
    private void DrawLargeTestPattern()
    {
        // Draw large colored rectangles that are impossible to miss

        // Top third - Red
        for (int y = 0; y < ScreenHeight / 3; y++)
        {
            for (int x = 0; x < ScreenWidth; x++)
            {
                DrawTestPixel(x, y, 0xFF0000FF); // Red
            }
        }

        // Middle third - Green
        for (int y = ScreenHeight / 3; y < 2 * ScreenHeight / 3; y++)
        {
            for (int x = 0; x < ScreenWidth; x++)
            {
                DrawTestPixel(x, y, 0xFF00FF00); // Green
            }
        }

        // Bottom third - Blue
        for (int y = 2 * ScreenHeight / 3; y < ScreenHeight; y++)
        {
            for (int x = 0; x < ScreenWidth; x++)
            {
                DrawTestPixel(x, y, 0xFFFF0000); // Blue
            }
        }

        // Add animated white border
        if (FrameNumber % 60 < 30) // Blink every second
        {
            // Top and bottom borders
            for (int x = 0; x < ScreenWidth; x++)
            {
                DrawTestPixel(x, 0, 0xFFFFFFFF);              // Top border
                DrawTestPixel(x, ScreenHeight - 1, 0xFFFFFFFF); // Bottom border
            }

            // Left and right borders
            for (int y = 0; y < ScreenHeight; y++)
            {
                DrawTestPixel(0, y, 0xFFFFFFFF);             // Left border
                DrawTestPixel(ScreenWidth - 1, y, 0xFFFFFFFF); // Right border
            }
        }
    }

    /// <summary>
    /// Creates a test pattern to verify rendering pipeline
    /// </summary>
    private void CreateTestPattern()
    {
        // Small test pattern in top-left corner only
        for (int y = 5; y < 25; y += 5)
        {
            for (int x = 5; x < 25; x += 5)
            {
                var color = ((x / 5) + (y / 5)) % 2 == 0 ? 0xFF0000FF : 0xFF00FF00; // Red and Green squares
                for (int dy = 0; dy < 4; dy++)
                {
                    for (int dx = 0; dx < 4; dx++)
                    {
                        DrawTestPixel(x + dx, y + dy, color);
                    }
                }
            }
        }

        // Add frame counter display (small)
        var frameColor = (uint)(0xFF000000 | ((FrameNumber * 4) & 0xFF) << 16); // Animated red
        for (int i = 0; i < 10; i++)
        {
            DrawTestPixel(30 + i, 5, frameColor);
            DrawTestPixel(30, 5 + i, frameColor);
        }

        // Show PPU state as colored pixels
        var ppuState = (PPUState)_ppu.GetState();
        var maskColor = (byte)ppuState.Mask != 0 ? 0xFF00FFFF : 0xFF808080; // Cyan if mask enabled, grey if not
        var controlColor = (byte)ppuState.Control != 0 ? 0xFFFFFF00 : 0xFF808080; // Yellow if control enabled, grey if not

        for (int i = 0; i < 5; i++)
        {
            DrawTestPixel(45 + i, 5, maskColor);    // PPU Mask status
            DrawTestPixel(45 + i, 10, controlColor); // PPU Control status
        }
    }

    /// <summary>
    /// Draws a single test pixel at the specified coordinates
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="color">RGBA color</param>
    private void DrawTestPixel(int x, int y, uint color)
    {
        if (x >= 0 && x < ScreenWidth && y >= 0 && y < ScreenHeight)
        {
            int index = (y * ScreenWidth + x) * 4;
            _frameBuffer[index] = (byte)((color >> 16) & 0xFF);     // R
            _frameBuffer[index + 1] = (byte)((color >> 8) & 0xFF); // G
            _frameBuffer[index + 2] = (byte)(color & 0xFF);         // B
            _frameBuffer[index + 3] = (byte)((color >> 24) & 0xFF); // A
        }
    }

    /// <summary>
    /// Disposes of renderer resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            IsInitialized = false;
            _disposed = true;
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogDebug("PPU Renderer disposed");
            #pragma warning restore CA1848
        }
        #pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
        #pragma warning restore CA1031
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            _logger.LogError(ex, "Error during renderer disposal");
            #pragma warning restore CA1848
        }
    }

    /// <summary>
    /// Renders initialization pattern for first few frames
    /// </summary>
    private void RenderInitializationPattern()
    {
        // Show a blue screen with "INIT" pattern
        for (int y = 0; y < ScreenHeight; y++)
        {
            for (int x = 0; x < ScreenWidth; x++)
            {
                var color = 0xFF0000FF; // Blue background

                // Add some pattern to show it's working
                if ((x + y) % 16 < 8)
                {
                    color = 0xFF000080; // Darker blue
                }

                SetPixel(x, y, color);
            }
        }
    }

    /// <summary>
    /// Renders test pattern when PPU rendering is disabled
    /// </summary>
    private void RenderTestPattern()
    {
        // Show a red screen with "WAITING FOR PPU" pattern
        for (int y = 0; y < ScreenHeight; y++)
        {
            for (int x = 0; x < ScreenWidth; x++)
            {
                var color = 0xFFFF0000; // Red background

                // Add checkerboard pattern
                if (((x / 8) + (y / 8)) % 2 == 0)
                {
                    color = 0xFF800000; // Darker red
                }

                SetPixel(x, y, color);
            }
        }
    }
}

/// <summary>
/// Represents sprite data for rendering
/// </summary>
public sealed class SpriteData
{
    /// <summary>
    /// Gets the sprite X position
    /// </summary>
    public int X { get; init; }

    /// <summary>
    /// Gets the sprite Y position
    /// </summary>
    public int Y { get; init; }

    /// <summary>
    /// Gets the sprite width
    /// </summary>
    public int Width { get; init; } = 8;

    /// <summary>
    /// Gets the sprite height
    /// </summary>
    public int Height { get; init; } = 8;

    /// <summary>
    /// Gets the sprite pattern data
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Sprite data requires array for pattern storage")]
    public byte[] PatternData { get; init; } = Array.Empty<byte>();

    /// <summary>
    /// Gets the sprite palette index
    /// </summary>
    public byte PaletteIndex { get; init; }

    /// <summary>
    /// Gets whether the sprite is flipped horizontally
    /// </summary>
    public bool FlipHorizontal { get; init; }

    /// <summary>
    /// Gets whether the sprite is flipped vertically
    /// </summary>
    public bool FlipVertical { get; init; }

    /// <summary>
    /// Gets the pixel color at specified coordinates
    /// </summary>
    /// <param name="x">X coordinate within sprite</param>
    /// <param name="y">Y coordinate within sprite</param>
    /// <returns>Palette color index</returns>
    public byte GetPixel(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height || PatternData.Length == 0)
        {
            return 0;
        }

        // Apply flipping
        var pixelX = FlipHorizontal ? (Width - 1 - x) : x;
        var pixelY = FlipVertical ? (Height - 1 - y) : y;

        // Extract pixel from pattern data (2 bits per pixel)
        var byteIndex = pixelY * (Width / 4) + (pixelX / 4);
        var bitIndex = (pixelX % 4) * 2;

        if (byteIndex >= PatternData.Length)
        {
            return 0;
        }

        var pixelData = (PatternData[byteIndex] >> bitIndex) & 0x03;
        return pixelData == 0 ? (byte)0 : (byte)(PaletteIndex * 4 + pixelData);
    }
}
