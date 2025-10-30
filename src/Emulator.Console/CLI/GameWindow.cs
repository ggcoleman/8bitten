using System;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EightBitten.Core.Contracts;
using EightBitten.Core.Emulator;
using EightBitten.Core.PPU;
using EightBitten.Infrastructure.Platform.Graphics;
using EightBitten.Infrastructure.Platform.Audio;
using EightBitten.Infrastructure.Platform.Input;

namespace EightBitten.Emulator.Console.CLI;

/// <summary>
/// MonoGame-based game window for CLI gaming mode
/// Handles window creation, graphics rendering, and input processing
/// </summary>
public sealed class GameWindow : Game, IGameWindow
{
    private readonly ILogger<GameWindow> _logger;
    private readonly MonoGameRenderer _renderer;
    private readonly NAudioRenderer _audioRenderer;
    private readonly InputManager _inputManager;
    private readonly GameWindowSettings _settings;
    
    private GraphicsDeviceManager? _graphics;
    private SpriteBatch? _spriteBatch;
    private bool _isInitialized;
    private bool _disposed;

    private IEmulator? _emulator;
    private Renderer? _coreRenderer;

    /// <summary>
    /// Gets whether the game window is initialized
    /// </summary>
    public bool IsInitialized => _isInitialized && !_disposed;

    /// <summary>
    /// Gets the current window settings
    /// </summary>
    public GameWindowSettings Settings => _settings;

    /// <summary>
    /// Event raised when the window is closing
    /// </summary>
    public event EventHandler? WindowClosing;

    /// <summary>
    /// Event raised when input state changes
    /// </summary>
    public event EventHandler<InputStateChangedEventArgs>? InputChanged;

    /// <summary>
    /// Initializes a new instance of the GameWindow class
    /// </summary>
    /// <param name="renderer">Graphics renderer</param>
    /// <param name="audioRenderer">Audio renderer</param>
    /// <param name="inputManager">Input manager</param>
    /// <param name="settings">Window settings</param>
    /// <param name="logger">Logger for diagnostic output</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
    public GameWindow(
        MonoGameRenderer renderer,
        NAudioRenderer audioRenderer,
        InputManager inputManager,
        GameWindowSettings settings,
        ILogger<GameWindow> logger)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _audioRenderer = audioRenderer ?? throw new ArgumentNullException(nameof(audioRenderer));
        _inputManager = inputManager ?? throw new ArgumentNullException(nameof(inputManager));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Subscribe to input events
        _inputManager.InputStateChanged += OnInputStateChanged;

        // Initialize graphics device manager (must be done in constructor for MonoGame)
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = _settings.WindowWidth;
        _graphics.PreferredBackBufferHeight = _settings.WindowHeight;
        _graphics.IsFullScreen = _settings.IsFullScreen;
        _graphics.SynchronizeWithVerticalRetrace = _settings.VSync;

        #pragma warning disable CA1303 // Do not pass literals as localized parameters
        System.Console.WriteLine("*** MONOGAME: GameWindow constructor completed ***");
        #pragma warning restore CA1303
        _logger.LogDebug("GameWindow created");
    }

    /// <summary>
    /// Initializes the game window
    /// </summary>
    protected override void Initialize()
    {
        try
        {
            #pragma warning disable CA1303 // Do not pass literals as localized parameters
            System.Console.WriteLine("*** MONOGAME: Initialize() called ***");
            #pragma warning restore CA1303
            _logger.LogInformation("Initializing game window");

            // Apply graphics settings (graphics device manager already created in constructor)
            _graphics?.ApplyChanges();

            // Set window title
            Window.Title = _settings.WindowTitle;
            Window.AllowUserResizing = _settings.AllowResize;

            // Initialize input manager
            if (!_inputManager.Initialize())
            {
                throw new InvalidOperationException("Failed to initialize input manager");
            }

            base.Initialize();
            _isInitialized = true;
            _logger.LogInformation("Game window initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize game window");
            throw;
        }
    }

    /// <summary>
    /// Loads game content
    /// </summary>
    protected override void LoadContent()
    {
        try
        {
            #pragma warning disable CA1303 // Do not pass literals as localized parameters
            System.Console.WriteLine("*** MONOGAME: LoadContent() called ***");
            #pragma warning restore CA1303
            _logger.LogDebug("Loading game content");

            // Create sprite batch
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Initialize renderer with graphics device
            if (!_renderer.Initialize(256, 240))
            {
                throw new InvalidOperationException("Failed to initialize graphics renderer");
            }

            _renderer.SetGraphicsDevice(GraphicsDevice, _spriteBatch);
            _renderer.SetScaling(_settings.RenderScale);
            _renderer.EnableVSync(_settings.VSync);

            // Initialize audio only if enabled
            if (_settings.AudioEnabled)
            {
                if (!_audioRenderer.Initialize(44100, 2, 1024))
                {
                    throw new InvalidOperationException("Failed to initialize audio renderer");
                }

                _audioRenderer.SetVolume(_settings.AudioVolume);
                _audioRenderer.StartPlayback();
            }
            else
            {
                _logger.LogInformation("Audio disabled; skipping audio initialization");
            }

            _logger.LogInformation("Game content loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load game content");
            throw;
        }
    }

    /// <summary>
    /// Updates game logic
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    protected override void Update(GameTime gameTime)
    {
        // Debug: Log first few Update calls
        if (gameTime != null && gameTime.TotalGameTime.TotalSeconds < 2)
        {
            #pragma warning disable CA1303 // Do not pass literals as localized parameters
            System.Console.WriteLine($"*** MONOGAME: Update() called - TotalTime={gameTime.TotalGameTime.TotalSeconds:F2}s ***");
            #pragma warning restore CA1303
        }

        if (!_isInitialized || _disposed || gameTime == null)
        {
            if (gameTime != null && gameTime.TotalGameTime.TotalSeconds < 2)
            {
                #pragma warning disable CA1303 // Do not pass literals as localized parameters
                #pragma warning disable CA1508 // Avoid dead code
                System.Console.WriteLine($"*** MONOGAME: Update() early return - Initialized={_isInitialized}, Disposed={_disposed}, GameTime={gameTime != null} ***");
                #pragma warning restore CA1508
                #pragma warning restore CA1303
            }
            return;
        }

        try
        {
            // Update input safely
            _inputManager?.Update();

            // Check for exit conditions
            if (_inputManager?.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Escape) == true)
            {
                WindowClosing?.Invoke(this, EventArgs.Empty);
                Exit();
                return;
            }

            // Execute emulator frame if available
            if (_emulator != null && _coreRenderer != null && !_disposed)
            {
                try
                {
                    // Debug: Log emulator execution attempt
                    if (_emulator.FrameNumber < 5)
                    {
                        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                        _logger.LogDebug("GameWindow: About to call StepFrame() - Frame: {Frame}, State: {State}",
                            _emulator.FrameNumber, _emulator.State);
                        #pragma warning restore CA1848
                    }

                    var stepResult = _emulator.StepFrame();

                    // Debug: Log step result
                    if (_emulator.FrameNumber < 5)
                    {
                        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                        _logger.LogDebug("GameWindow: StepFrame() returned {Result} - Frame: {Frame}",
                            stepResult, _emulator.FrameNumber);
                        #pragma warning restore CA1848
                    }

                    var frameData = _coreRenderer.RenderFrame();
                    if (frameData != null)
                    {
                        RenderFrame(frameData);

                        // Debug: Log rendering activity
                        if (_emulator.FrameNumber % 60 == 0)
                        {
                            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                            _logger.LogDebug("GameWindow: Rendered frame {Frame}, data size: {Size} bytes",
                                _emulator.FrameNumber, frameData.Length);
                            #pragma warning restore CA1848
                        }
                    }
                    else
                    {
                        // Debug: Log when no frame data is available
                        if (_emulator.FrameNumber % 60 == 0)
                        {
                            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                            _logger.LogDebug("GameWindow: No frame data from renderer at frame {Frame}", _emulator.FrameNumber);
                            #pragma warning restore CA1848
                        }
                    }

                    // Debug: Log emulator state every 60 frames
                    if (_emulator.FrameNumber % 60 == 0)
                    {
                        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                        _logger.LogDebug("Emulator running - Frame: {Frame}, Cycles: {Cycles}, State: {State}",
                            _emulator.FrameNumber, _emulator.CycleCount, _emulator.State);

                        // Also log basic emulator execution info
                        _logger.LogDebug("Emulator Cycles: {Cycles}, Frame: {Frame}",
                            _emulator.CycleCount, _emulator.FrameNumber);
                        #pragma warning restore CA1848
                    }
                }
                catch (Exception emulatorEx)
                {
                    _logger.LogError(emulatorEx, "Error during emulator execution");
                }
            }
            else
            {
                // Debug: Log why emulator is not executing
                if (gameTime.TotalGameTime.TotalSeconds < 5) // Only log for first 5 seconds
                {
                    #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
                    _logger.LogWarning("*** GameWindow.Update: Emulator not executing - Emulator={Emulator}, Renderer={Renderer}, Disposed={Disposed} ***",
                        _emulator != null ? "SET" : "NULL", _coreRenderer != null ? "SET" : "NULL", _disposed);
                    #pragma warning restore CA1848
                }
            }


            base.Update(gameTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during game update");
            // Don't rethrow to prevent crash
        }
    }

    /// <summary>
    /// Renders the game
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    protected override void Draw(GameTime gameTime)
    {
        if (!_isInitialized || _disposed || GraphicsDevice == null || gameTime == null)
        {
            return;
        }

        try
        {
            // Clear screen safely
            GraphicsDevice.Clear(Color.Black);

            // Calculate destination rectangle for NES screen
            var destRect = CalculateDestinationRectangle();

            // Draw the current frame safely
            _renderer?.DrawFrame(destRect);

            base.Draw(gameTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during game rendering");
            // Don't rethrow to prevent crash
        }
    }

    /// <summary>
    /// Renders a frame from pixel data
    /// </summary>
    /// <param name="frameData">RGBA pixel data</param>
    /// <returns>True if rendering succeeded, false otherwise</returns>
    public bool RenderFrame(byte[] frameData)
    {
        if (!_isInitialized || _disposed)
        {
            return false;
        }

        return _renderer.RenderFrame(frameData);
    }

    /// <summary>
    /// Plays audio samples
    /// </summary>
    /// <param name="samples">Audio sample data</param>
    /// <returns>True if audio was played successfully, false otherwise</returns>
    public bool PlayAudio(float[] samples)
    {
        if (!_isInitialized || _disposed || !_settings.AudioEnabled)
        {
            return false;
        }

        return _audioRenderer.PlayAudio(samples);
    }

    /// <summary>
    /// Closes the game window (implements IGameWindow.Close)
    /// </summary>
    public void Close()
    {
        Exit();
    }

    /// <summary>
    /// Sets the emulator and core renderer for frame execution
    /// </summary>
    /// <param name="emulator">The emulator instance</param>
    /// <param name="coreRenderer">The core renderer instance</param>
    public void SetEmulator(IEmulator emulator, Renderer coreRenderer)
    {
        _emulator = emulator;
        _coreRenderer = coreRenderer;

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        _logger.LogDebug("*** GameWindow.SetEmulator called: Emulator={Emulator}, Renderer={Renderer} ***",
            _emulator != null ? "SET" : "NULL", _coreRenderer != null ? "SET" : "NULL");
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Updates window settings
    /// </summary>
    /// <param name="newSettings">New window settings</param>
    public void UpdateSettings(IWindowSettings newSettings)
    {
        ArgumentNullException.ThrowIfNull(newSettings);

        // Cast to concrete type for full functionality
        if (newSettings is not GameWindowSettings gameSettings)
        {
            throw new ArgumentException("Settings must be of type GameWindowSettings", nameof(newSettings));
        }

        try
        {
            if (_graphics != null)
            {
                _graphics.PreferredBackBufferWidth = gameSettings.WindowWidth;
                _graphics.PreferredBackBufferHeight = gameSettings.WindowHeight;
                _graphics.IsFullScreen = gameSettings.IsFullScreen;
                _graphics.SynchronizeWithVerticalRetrace = gameSettings.VSync;
                _graphics.ApplyChanges();
            }

            if (Window != null)
            {
                Window.Title = gameSettings.WindowTitle;
                Window.AllowUserResizing = gameSettings.AllowResize;
            }

            _renderer.SetScaling(gameSettings.RenderScale);
            _renderer.EnableVSync(gameSettings.VSync);

            if (_settings.AudioEnabled != gameSettings.AudioEnabled)
            {
                if (gameSettings.AudioEnabled)
                {
                    _audioRenderer.StartPlayback();
                }
                else
                {
                    _audioRenderer.StopPlayback();
                }
            }

            _audioRenderer.SetVolume(gameSettings.AudioVolume);

            // Update settings
            _settings.UpdateFrom(gameSettings);

            _logger.LogInformation("Window settings updated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update window settings");
        }
    }

    /// <summary>
    /// Calculates the destination rectangle for NES screen rendering
    /// </summary>
    /// <returns>Destination rectangle</returns>
    private Rectangle CalculateDestinationRectangle()
    {
        if (_graphics == null)
        {
            return new Rectangle(0, 0, 256, 240);
        }

        var windowWidth = _graphics.PreferredBackBufferWidth;
        var windowHeight = _graphics.PreferredBackBufferHeight;

        if (_settings.MaintainAspectRatio)
        {
            // Calculate scaled size maintaining 256:240 aspect ratio
            var scaleX = windowWidth / 256.0f;
            var scaleY = windowHeight / 240.0f;
            var scale = Math.Min(scaleX, scaleY);

            var scaledWidth = (int)(256 * scale);
            var scaledHeight = (int)(240 * scale);

            // Center the image
            var x = (windowWidth - scaledWidth) / 2;
            var y = (windowHeight - scaledHeight) / 2;

            return new Rectangle(x, y, scaledWidth, scaledHeight);
        }
        else
        {
            // Stretch to fill window
            return new Rectangle(0, 0, windowWidth, windowHeight);
        }
    }

    /// <summary>
    /// Handles input state changes
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Input state change event arguments</param>
    private void OnInputStateChanged(object? sender, InputStateChangedEventArgs e)
    {
        InputChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Disposes of game window resources
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                _inputManager.InputStateChanged -= OnInputStateChanged;
                
                _audioRenderer?.StopPlayback();
                _audioRenderer?.Dispose();
                _renderer?.Dispose();
                _inputManager?.Dispose();
                _spriteBatch?.Dispose();
                _graphics?.Dispose();

                _isInitialized = false;
                _disposed = true;

                _logger.LogDebug("GameWindow disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during game window disposal");
            }
        }

        base.Dispose(disposing);
    }
}

/// <summary>
/// Game window configuration settings
/// </summary>
public sealed class GameWindowSettings : IWindowSettings
{
    /// <summary>
    /// Gets or sets the window width
    /// </summary>
    public int WindowWidth { get; set; } = 1024;

    /// <summary>
    /// Gets or sets the window height
    /// </summary>
    public int WindowHeight { get; set; } = 960;

    /// <summary>
    /// Gets or sets whether the window is fullscreen
    /// </summary>
    public bool IsFullScreen { get; set; }

    /// <summary>
    /// Gets or sets whether VSync is enabled
    /// </summary>
    public bool VSync { get; set; } = true;

    /// <summary>
    /// Gets or sets the render scale factor
    /// </summary>
    public float RenderScale { get; set; } = 4.0f;

    /// <summary>
    /// Gets or sets whether to maintain aspect ratio
    /// </summary>
    public bool MaintainAspectRatio { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the window can be resized
    /// </summary>
    public bool AllowResize { get; set; } = true;

    /// <summary>
    /// Gets or sets the window title
    /// </summary>
    public string WindowTitle { get; set; } = "8Bitten NES Emulator";

    /// <summary>
    /// Gets or sets whether audio is enabled
    /// </summary>
    public bool AudioEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the audio volume (0.0 to 1.0)
    /// </summary>
    public float AudioVolume { get; set; } = 0.7f;

    /// <summary>
    /// Gets or sets whether performance monitoring is enabled
    /// </summary>
    public bool PerformanceMonitoring { get; set; } = false;

    /// <summary>
    /// Updates settings from another settings object
    /// </summary>
    /// <param name="other">Other settings object</param>
    public void UpdateFrom(GameWindowSettings other)
    {
        ArgumentNullException.ThrowIfNull(other);

        WindowWidth = other.WindowWidth;
        WindowHeight = other.WindowHeight;
        IsFullScreen = other.IsFullScreen;
        VSync = other.VSync;
        RenderScale = other.RenderScale;
        MaintainAspectRatio = other.MaintainAspectRatio;
        AllowResize = other.AllowResize;
        WindowTitle = other.WindowTitle;
        AudioEnabled = other.AudioEnabled;
        AudioVolume = other.AudioVolume;
        PerformanceMonitoring = other.PerformanceMonitoring;
    }
}
