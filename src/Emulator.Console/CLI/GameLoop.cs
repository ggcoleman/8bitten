using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using EightBitten.Core.Emulator;
using EightBitten.Core.PPU;
using EightBitten.Core.APU;
using EightBitten.Infrastructure.Platform.Input;

namespace EightBitten.Emulator.Console.CLI;

/// <summary>
/// Real-time execution loop for CLI gaming mode
/// Coordinates emulation timing, rendering, and audio output
/// </summary>
public sealed class GameLoop : IDisposable
{
    private readonly ILogger<GameLoop> _logger;
    private readonly NESEmulator _emulator;
    private readonly GameWindow _gameWindow;
    private readonly Renderer _renderer;
    private readonly AudioGenerator _audioGenerator;
    private readonly GameLoopSettings _settings;
    
    private readonly Stopwatch _frameTimer;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private Task? _gameLoopTask;
    private bool _isRunning;
    private bool _disposed;

    // Timing constants
    private const double TARGET_FPS = 60.0988; // NTSC NES frame rate
    private const double TARGET_FRAME_TIME_MS = 1000.0 / TARGET_FPS;
    private const int AUDIO_SAMPLES_PER_FRAME = 735; // ~44100Hz / 60fps

    /// <summary>
    /// Gets whether the game loop is running
    /// </summary>
    public bool IsRunning => _isRunning && !_disposed;

    /// <summary>
    /// Gets the current frame rate
    /// </summary>
    public double CurrentFPS { get; private set; }

    /// <summary>
    /// Gets the current frame time in milliseconds
    /// </summary>
    public double CurrentFrameTime { get; private set; }

    /// <summary>
    /// Gets the total number of frames processed
    /// </summary>
    public long TotalFrames { get; private set; }

    /// <summary>
    /// Event raised when a frame is completed
    /// </summary>
    public event EventHandler<FrameCompletedEventArgs>? FrameCompleted;

    /// <summary>
    /// Initializes a new instance of the GameLoop class
    /// </summary>
    /// <param name="emulator">NES emulator instance</param>
    /// <param name="gameWindow">Game window for rendering</param>
    /// <param name="renderer">PPU renderer</param>
    /// <param name="audioGenerator">APU audio generator</param>
    /// <param name="settings">Game loop settings</param>
    /// <param name="logger">Logger for diagnostic output</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
    public GameLoop(
        NESEmulator emulator,
        GameWindow gameWindow,
        Renderer renderer,
        AudioGenerator audioGenerator,
        GameLoopSettings settings,
        ILogger<GameLoop> logger)
    {
        _emulator = emulator ?? throw new ArgumentNullException(nameof(emulator));
        _gameWindow = gameWindow ?? throw new ArgumentNullException(nameof(gameWindow));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _audioGenerator = audioGenerator ?? throw new ArgumentNullException(nameof(audioGenerator));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _frameTimer = new Stopwatch();
        _cancellationTokenSource = new CancellationTokenSource();

        // Subscribe to input events
        _gameWindow.InputChanged += OnInputChanged;

        _logger.LogDebug("GameLoop created");
    }

    /// <summary>
    /// Starts the game loop
    /// </summary>
    /// <returns>True if the game loop started successfully, false otherwise</returns>
    public bool Start()
    {
        if (_disposed || _isRunning)
        {
            _logger.LogWarning("Cannot start game loop: disposed or already running");
            return false;
        }

        try
        {
            _logger.LogInformation("Starting game loop");

            // Initialize components
            if (!_renderer.Initialize())
            {
                _logger.LogError("Failed to initialize renderer");
                return false;
            }

            if (!_audioGenerator.Initialize())
            {
                _logger.LogError("Failed to initialize audio generator");
                return false;
            }

            // Start the game loop task
            _gameLoopTask = Task.Run(GameLoopAsync, _cancellationTokenSource.Token);
            _isRunning = true;

            _logger.LogInformation("Game loop started successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start game loop");
            return false;
        }
    }

    /// <summary>
    /// Stops the game loop
    /// </summary>
    /// <returns>Task representing the stop operation</returns>
    public async Task StopAsync()
    {
        if (!_isRunning || _disposed)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Stopping game loop");

            _cancellationTokenSource.Cancel();
            
            if (_gameLoopTask != null)
            {
                await _gameLoopTask.ConfigureAwait(false);
            }

            _isRunning = false;
            _logger.LogInformation("Game loop stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping game loop");
        }
    }

    /// <summary>
    /// Main game loop execution
    /// </summary>
    /// <returns>Task representing the game loop</returns>
    private async Task GameLoopAsync()
    {
        var token = _cancellationTokenSource.Token;
        var frameCount = 0L;
        var fpsTimer = Stopwatch.StartNew();

        _logger.LogDebug("Game loop thread started");

        try
        {
            while (!token.IsCancellationRequested)
            {
                _frameTimer.Restart();

                // Execute one frame of emulation
                await ExecuteFrameAsync(token).ConfigureAwait(false);

                // Update timing statistics
                _frameTimer.Stop();
                CurrentFrameTime = _frameTimer.Elapsed.TotalMilliseconds;
                TotalFrames++;
                frameCount++;

                // Calculate FPS every second
                if (fpsTimer.ElapsedMilliseconds >= 1000)
                {
                    CurrentFPS = frameCount * 1000.0 / fpsTimer.ElapsedMilliseconds;
                    frameCount = 0;
                    fpsTimer.Restart();
                }

                // Frame rate limiting
                if (_settings.EnableFrameRateLimit)
                {
                    await LimitFrameRateAsync(token).ConfigureAwait(false);
                }

                // Raise frame completed event
                FrameCompleted?.Invoke(this, new FrameCompletedEventArgs
                {
                    FrameNumber = TotalFrames,
                    FrameTime = CurrentFrameTime,
                    FPS = CurrentFPS
                });
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Game loop cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in game loop");
        }
        finally
        {
            _logger.LogDebug("Game loop thread ended");
        }
    }

    /// <summary>
    /// Executes one frame of emulation
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the frame execution</returns>
    private async Task ExecuteFrameAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Step emulator for one frame
            _emulator.StepFrame();

            // Generate graphics from PPU
            var frameData = _renderer.RenderFrame();

            // Send frame data to MonoGame renderer and render to window
            _gameWindow.RenderFrame(frameData);

            // Generate audio
            if (_settings.AudioEnabled)
            {
                var audioSamples = _audioGenerator.GenerateSamples(AUDIO_SAMPLES_PER_FRAME);
                _gameWindow.PlayAudio(audioSamples);
            }

            // Yield to prevent blocking
            await Task.Yield();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing frame {FrameNumber}", TotalFrames);
        }
    }

    /// <summary>
    /// Limits frame rate to target FPS
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the delay</returns>
    private async Task LimitFrameRateAsync(CancellationToken cancellationToken)
    {
        var targetFrameTime = TARGET_FRAME_TIME_MS;
        var actualFrameTime = CurrentFrameTime;
        var remainingTime = targetFrameTime - actualFrameTime;

        if (remainingTime > 0)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(remainingTime), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }
    }

    /// <summary>
    /// Handles input state changes
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Input state change event arguments</param>
    private void OnInputChanged(object? sender, InputStateChangedEventArgs e)
    {
        try
        {
            // Input state is handled by the InputManager and passed to the emulator
            // through the memory-mapped controller registers during emulation
            // No direct controller state setting needed here
            if (e.Player1Changed)
            {
                // Input changes are automatically handled by the input system
            }

            if (e.Player2Changed)
            {
                // Input changes are automatically handled by the input system
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling input change");
        }
    }

    /// <summary>
    /// Disposes of game loop resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            // Stop the game loop
            StopAsync().GetAwaiter().GetResult();

            // Unsubscribe from events
            _gameWindow.InputChanged -= OnInputChanged;

            // Dispose resources
            _cancellationTokenSource.Dispose();
            _frameTimer.Stop();

            _disposed = true;
            _logger.LogDebug("GameLoop disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during game loop disposal");
        }
    }
}

/// <summary>
/// Game loop configuration settings
/// </summary>
public sealed class GameLoopSettings
{
    /// <summary>
    /// Gets or sets whether frame rate limiting is enabled
    /// </summary>
    public bool EnableFrameRateLimit { get; set; } = true;

    /// <summary>
    /// Gets or sets whether audio is enabled
    /// </summary>
    public bool AudioEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether performance monitoring is enabled
    /// </summary>
    public bool PerformanceMonitoring { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum frame skip count
    /// </summary>
    public int MaxFrameSkip { get; set; } = 5;
}

/// <summary>
/// Event arguments for frame completed events
/// </summary>
public sealed class FrameCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the frame number
    /// </summary>
    public long FrameNumber { get; init; }

    /// <summary>
    /// Gets the frame time in milliseconds
    /// </summary>
    public double FrameTime { get; init; }

    /// <summary>
    /// Gets the current FPS
    /// </summary>
    public double FPS { get; init; }
}
