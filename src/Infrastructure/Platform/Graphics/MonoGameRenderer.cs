using System;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EightBitten.Infrastructure.Platform.Graphics;

/// <summary>
/// MonoGame-based graphics renderer for NES emulator
/// Handles frame buffer rendering, scaling, and display output
/// </summary>
public sealed class MonoGameRenderer : IDisposable
{
    private readonly ILogger<MonoGameRenderer> _logger;
    private GraphicsDevice? _graphicsDevice;
    private Texture2D? _frameTexture;
    private SpriteBatch? _spriteBatch;
    private byte[]? _frameBuffer;
    private bool _isInitialized;
    private bool _disposed;

    /// <summary>
    /// Gets whether the renderer is initialized
    /// </summary>
    public bool IsInitialized => _isInitialized && !_disposed;

    /// <summary>
    /// Gets the screen width in pixels
    /// </summary>
    public int ScreenWidth { get; private set; }

    /// <summary>
    /// Gets the screen height in pixels
    /// </summary>
    public int ScreenHeight { get; private set; }

    /// <summary>
    /// Gets or sets the render scale factor
    /// </summary>
    public float RenderScale { get; private set; } = 1.0f;

    /// <summary>
    /// Gets whether VSync is enabled
    /// </summary>
    public bool IsVSyncEnabled { get; private set; }

    /// <summary>
    /// Initializes a new instance of the MonoGameRenderer class
    /// </summary>
    /// <param name="logger">Logger for diagnostic output</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null</exception>
    public MonoGameRenderer(ILogger<MonoGameRenderer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogDebug("MonoGameRenderer created");
    }

    /// <summary>
    /// Initializes the graphics renderer with specified dimensions
    /// </summary>
    /// <param name="width">Screen width in pixels</param>
    /// <param name="height">Screen height in pixels</param>
    /// <returns>True if initialization succeeded, false otherwise</returns>
    public bool Initialize(int width, int height)
    {
        if (_disposed)
        {
            _logger.LogError("Cannot initialize disposed renderer");
            return false;
        }

        if (width <= 0 || height <= 0)
        {
            _logger.LogError("Invalid screen dimensions: {Width}x{Height}", width, height);
            return false;
        }

        try
        {
            ScreenWidth = width;
            ScreenHeight = height;
            
            // Initialize frame buffer
            _frameBuffer = new byte[width * height * 4]; // RGBA format
            
            _logger.LogInformation("Graphics renderer initialized: {Width}x{Height}", width, height);
            _isInitialized = true;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize graphics renderer");
            return false;
        }
    }

    /// <summary>
    /// Sets the graphics device for rendering
    /// </summary>
    /// <param name="graphicsDevice">MonoGame graphics device</param>
    /// <param name="spriteBatch">Sprite batch for rendering</param>
    public void SetGraphicsDevice(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentNullException.ThrowIfNull(spriteBatch);

        _graphicsDevice = graphicsDevice;
        _spriteBatch = spriteBatch;

        if (_isInitialized)
        {
            CreateFrameTexture();
        }

        _logger.LogDebug("Graphics device set");
    }

    /// <summary>
    /// Renders a frame from pixel data
    /// </summary>
    /// <param name="frameData">RGBA pixel data</param>
    /// <returns>True if rendering succeeded, false otherwise</returns>
    public bool RenderFrame(byte[] frameData)
    {
        ArgumentNullException.ThrowIfNull(frameData);

        if (!_isInitialized || _disposed)
        {
            _logger.LogWarning("Renderer not initialized or disposed");
            return false;
        }

        var expectedSize = ScreenWidth * ScreenHeight * 4;
        if (frameData.Length != expectedSize)
        {
            _logger.LogError("Invalid frame data size: {Actual}, expected: {Expected}", 
                frameData.Length, expectedSize);
            return false;
        }

        try
        {
            // Copy frame data to internal buffer
            Array.Copy(frameData, _frameBuffer!, frameData.Length);

            // Update texture if graphics device is available
            if (_frameTexture != null && _graphicsDevice != null)
            {
                _frameTexture.SetData(frameData);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render frame");
            return false;
        }
    }

    /// <summary>
    /// Draws the current frame to the screen
    /// </summary>
    /// <param name="destinationRectangle">Target rectangle for rendering</param>
    public void DrawFrame(Rectangle destinationRectangle)
    {
        if (!_isInitialized || _disposed || _frameTexture == null || _spriteBatch == null)
        {
            return;
        }

        try
        {
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp);
            _spriteBatch.Draw(_frameTexture, destinationRectangle, Color.White);
            _spriteBatch.End();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to draw frame");
        }
    }

    /// <summary>
    /// Sets the render scaling factor
    /// </summary>
    /// <param name="scale">Scale factor (must be positive)</param>
    /// <exception cref="ArgumentException">Thrown when scale is invalid</exception>
    public void SetScaling(float scale)
    {
        if (scale <= 0 || float.IsNaN(scale) || float.IsInfinity(scale))
        {
            throw new ArgumentException("Scale must be a positive finite number", nameof(scale));
        }

        RenderScale = scale;
        _logger.LogDebug("Render scale set to {Scale}", scale);
    }

    /// <summary>
    /// Enables or disables VSync
    /// </summary>
    /// <param name="enabled">True to enable VSync, false to disable</param>
    public void EnableVSync(bool enabled)
    {
        IsVSyncEnabled = enabled;
        _logger.LogDebug("VSync {Status}", enabled ? "enabled" : "disabled");
    }

    /// <summary>
    /// Gets the current frame buffer data
    /// </summary>
    /// <returns>Copy of the current frame buffer</returns>
    public byte[] GetFrameBuffer()
    {
        if (!_isInitialized || _frameBuffer == null)
        {
            return new byte[ScreenWidth * ScreenHeight * 4];
        }

        var buffer = new byte[_frameBuffer.Length];
        Array.Copy(_frameBuffer, buffer, _frameBuffer.Length);
        return buffer;
    }

    /// <summary>
    /// Creates the frame texture for rendering
    /// </summary>
    private void CreateFrameTexture()
    {
        if (_graphicsDevice == null || !_isInitialized)
        {
            return;
        }

        try
        {
            _frameTexture?.Dispose();
            _frameTexture = new Texture2D(_graphicsDevice, ScreenWidth, ScreenHeight);
            _logger.LogDebug("Frame texture created: {Width}x{Height}", ScreenWidth, ScreenHeight);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create frame texture");
        }
    }

    /// <summary>
    /// Disposes of graphics resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _frameTexture?.Dispose();
            _frameTexture = null;
            _spriteBatch = null;
            _graphicsDevice = null;
            _frameBuffer = null;
            _isInitialized = false;
            _disposed = true;

            _logger.LogDebug("MonoGameRenderer disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during renderer disposal");
        }
    }
}
