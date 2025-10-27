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

            // Render background if enabled
            if ((ppuState.Mask & PPUMask.ShowBackground) != 0)
            {
                RenderBackground(ppuState);
            }

            // Render sprites if enabled
            if ((ppuState.Mask & PPUMask.ShowSprites) != 0)
            {
                RenderSprites(ppuState);
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
        for (int x = 0; x < ScreenWidth; x++)
        {
            // For now, render a simple test pattern
            // In a full implementation, this would read from nametables and pattern tables
            var pixelColor = (byte)((x + scanline) % 4);
            var paletteColor = _palette[pixelColor & 0x3F];
            SetPixel(x, scanline, paletteColor);
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
