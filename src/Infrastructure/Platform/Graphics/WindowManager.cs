using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EightBitten.Infrastructure.Platform.Graphics;

/// <summary>
/// Interface for manageable game windows
/// </summary>
public interface IGameWindow : IDisposable
{
    /// <summary>
    /// Event raised when the window is closing
    /// </summary>
    event EventHandler? WindowClosing;

    /// <summary>
    /// Closes the game window
    /// </summary>
    void Close();

    /// <summary>
    /// Updates window settings
    /// </summary>
    /// <param name="settings">New window settings</param>
    void UpdateSettings(IWindowSettings settings);
}

/// <summary>
/// Interface for window settings
/// </summary>
public interface IWindowSettings
{
    /// <summary>
    /// Gets the window width
    /// </summary>
    int WindowWidth { get; }

    /// <summary>
    /// Gets the window height
    /// </summary>
    int WindowHeight { get; }

    /// <summary>
    /// Gets whether the window is fullscreen
    /// </summary>
    bool IsFullScreen { get; }

    /// <summary>
    /// Gets the window title
    /// </summary>
    string WindowTitle { get; }
}

/// <summary>
/// Window management system for handling game windows and cleanup
/// Provides centralized window lifecycle management and resource cleanup
/// </summary>
public sealed class WindowManager : IDisposable
{
    private readonly ILogger<WindowManager> _logger;
    private readonly Dictionary<string, IGameWindow> _windows;
    private readonly object _lockObject;
    private bool _disposed;

    /// <summary>
    /// Gets the number of active windows
    /// </summary>
    public int ActiveWindowCount => _windows.Count;

    /// <summary>
    /// Gets the names of all active windows
    /// </summary>
    public IReadOnlyCollection<string> ActiveWindowNames => _windows.Keys;

    /// <summary>
    /// Event raised when a window is created
    /// </summary>
    public event EventHandler<WindowEventArgs>? WindowCreated;

    /// <summary>
    /// Event raised when a window is closed
    /// </summary>
    public event EventHandler<WindowEventArgs>? WindowClosed;

    /// <summary>
    /// Event raised when all windows are closed
    /// </summary>
    public event EventHandler? AllWindowsClosed;

    /// <summary>
    /// Initializes a new instance of the WindowManager class
    /// </summary>
    /// <param name="logger">Logger for diagnostic output</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null</exception>
    public WindowManager(ILogger<WindowManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _windows = new Dictionary<string, IGameWindow>();
        _lockObject = new object();
        
        _logger.LogDebug("WindowManager created");
    }

    /// <summary>
    /// Creates and registers a new game window
    /// </summary>
    /// <param name="windowName">Unique name for the window</param>
    /// <param name="gameWindow">Game window instance</param>
    /// <returns>True if window was created successfully, false otherwise</returns>
    public bool CreateWindow(string windowName, IGameWindow gameWindow)
    {
        ArgumentNullException.ThrowIfNull(gameWindow);

        if (string.IsNullOrWhiteSpace(windowName))
        {
            _logger.LogError("Window name cannot be null or empty");
            return false;
        }

        if (_disposed)
        {
            _logger.LogError("Cannot create window on disposed manager");
            return false;
        }

        lock (_lockObject)
        {
            if (_windows.ContainsKey(windowName))
            {
                _logger.LogError("Window already exists: {WindowName}", windowName);
                return false;
            }

            try
            {
                // Subscribe to window events
                gameWindow.WindowClosing += (sender, e) => OnWindowClosing(windowName);

                // Register window
                _windows[windowName] = gameWindow;

                _logger.LogInformation("Created window: {WindowName}", windowName);
                
                // Raise event
                WindowCreated?.Invoke(this, new WindowEventArgs { WindowName = windowName });
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create window: {WindowName}", windowName);
                return false;
            }
        }
    }

    /// <summary>
    /// Gets a window by name
    /// </summary>
    /// <param name="windowName">Name of the window</param>
    /// <returns>Game window instance, or null if not found</returns>
    public IGameWindow? GetWindow(string windowName)
    {
        if (string.IsNullOrWhiteSpace(windowName) || _disposed)
        {
            return null;
        }

        lock (_lockObject)
        {
            return _windows.TryGetValue(windowName, out var window) ? window : null;
        }
    }

    /// <summary>
    /// Closes a specific window
    /// </summary>
    /// <param name="windowName">Name of the window to close</param>
    /// <returns>True if window was closed successfully, false otherwise</returns>
    public bool CloseWindow(string windowName)
    {
        if (string.IsNullOrWhiteSpace(windowName) || _disposed)
        {
            return false;
        }

        lock (_lockObject)
        {
            if (!_windows.TryGetValue(windowName, out var window))
            {
                _logger.LogWarning("Window not found: {WindowName}", windowName);
                return false;
            }

            try
            {
                // Close and dispose window
                window.Close();
                window.Dispose();

                // Remove from collection
                _windows.Remove(windowName);

                _logger.LogInformation("Closed window: {WindowName}", windowName);
                
                // Raise events
                WindowClosed?.Invoke(this, new WindowEventArgs { WindowName = windowName });
                
                if (_windows.Count == 0)
                {
                    AllWindowsClosed?.Invoke(this, EventArgs.Empty);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to close window: {WindowName}", windowName);
                return false;
            }
        }
    }

    /// <summary>
    /// Closes all windows
    /// </summary>
    /// <returns>Task representing the close operation</returns>
    public async Task CloseAllWindowsAsync()
    {
        if (_disposed)
        {
            return;
        }

        List<string> windowNames;
        
        lock (_lockObject)
        {
            windowNames = new List<string>(_windows.Keys);
        }

        _logger.LogInformation("Closing {Count} windows", windowNames.Count);

        var closeTasks = new List<Task>();
        
        foreach (var windowName in windowNames)
        {
            closeTasks.Add(Task.Run(() => CloseWindow(windowName)));
        }

        try
        {
            await Task.WhenAll(closeTasks).ConfigureAwait(false);
            _logger.LogInformation("All windows closed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing windows");
        }
    }

    /// <summary>
    /// Updates settings for a specific window
    /// </summary>
    /// <param name="windowName">Name of the window</param>
    /// <param name="settings">New window settings</param>
    /// <returns>True if settings were updated successfully, false otherwise</returns>
    public bool UpdateWindowSettings(string windowName, IWindowSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(windowName) || _disposed)
        {
            return false;
        }

        lock (_lockObject)
        {
            if (!_windows.TryGetValue(windowName, out var window))
            {
                _logger.LogWarning("Window not found: {WindowName}", windowName);
                return false;
            }

            try
            {
                window.UpdateSettings(settings);
                _logger.LogDebug("Updated settings for window: {WindowName}", windowName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update window settings: {WindowName}", windowName);
                return false;
            }
        }
    }

    /// <summary>
    /// Updates settings for all windows
    /// </summary>
    /// <param name="settings">New window settings</param>
    /// <returns>Number of windows successfully updated</returns>
    public int UpdateAllWindowSettings(IWindowSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (_disposed)
        {
            return 0;
        }

        var successCount = 0;
        List<string> windowNames;

        lock (_lockObject)
        {
            windowNames = new List<string>(_windows.Keys);
        }

        foreach (var windowName in windowNames)
        {
            if (UpdateWindowSettings(windowName, settings))
            {
                successCount++;
            }
        }

        _logger.LogInformation("Updated settings for {Count}/{Total} windows", successCount, windowNames.Count);
        return successCount;
    }

    /// <summary>
    /// Checks if a window exists
    /// </summary>
    /// <param name="windowName">Name of the window</param>
    /// <returns>True if window exists, false otherwise</returns>
    public bool WindowExists(string windowName)
    {
        if (string.IsNullOrWhiteSpace(windowName) || _disposed)
        {
            return false;
        }

        lock (_lockObject)
        {
            return _windows.ContainsKey(windowName);
        }
    }

    /// <summary>
    /// Gets window statistics
    /// </summary>
    /// <returns>Window manager statistics</returns>
    public WindowManagerStats GetStatistics()
    {
        lock (_lockObject)
        {
            return new WindowManagerStats
            {
                ActiveWindowCount = _windows.Count,
                WindowNames = new List<string>(_windows.Keys)
            };
        }
    }

    /// <summary>
    /// Handles window closing events
    /// </summary>
    /// <param name="windowName">Name of the closing window</param>
    private void OnWindowClosing(string windowName)
    {
        try
        {
            _logger.LogDebug("Window closing: {WindowName}", windowName);
            CloseWindow(windowName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling window closing: {WindowName}", windowName);
        }
    }

    /// <summary>
    /// Disposes of window manager resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Disposing WindowManager");

            // Close all windows
            CloseAllWindowsAsync().GetAwaiter().GetResult();

            // Clear events
            WindowCreated = null;
            WindowClosed = null;
            AllWindowsClosed = null;

            _disposed = true;
            _logger.LogDebug("WindowManager disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during window manager disposal");
        }
    }
}

/// <summary>
/// Event arguments for window events
/// </summary>
public sealed class WindowEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the window name
    /// </summary>
    public string WindowName { get; init; } = string.Empty;
}

/// <summary>
/// Window manager statistics
/// </summary>
public sealed class WindowManagerStats
{
    /// <summary>
    /// Gets or sets the number of active windows
    /// </summary>
    public int ActiveWindowCount { get; init; }

    /// <summary>
    /// Gets the list of window names
    /// </summary>
    public IReadOnlyList<string> WindowNames { get; init; } = new List<string>();
}
