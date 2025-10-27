using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Input;

namespace EightBitten.Infrastructure.Platform.Input;

/// <summary>
/// Controller input mapping system for configurable key bindings
/// Supports keyboard and gamepad mapping with save/load functionality
/// </summary>
public sealed class ControllerMapper : IDisposable
{
    private readonly ILogger<ControllerMapper> _logger;
    private readonly Dictionary<string, InputMapping> _mappingProfiles;
    private readonly string _configFilePath;
    private string _currentProfile;
    private bool _disposed;

    /// <summary>
    /// Gets the current mapping profile name
    /// </summary>
    public string CurrentProfile => _currentProfile;

    /// <summary>
    /// Gets the available mapping profile names
    /// </summary>
    public IReadOnlyCollection<string> AvailableProfiles => _mappingProfiles.Keys;

    /// <summary>
    /// Event raised when mapping profile changes
    /// </summary>
    public event EventHandler<MappingProfileChangedEventArgs>? ProfileChanged;

    /// <summary>
    /// Initializes a new instance of the ControllerMapper class
    /// </summary>
    /// <param name="configFilePath">Path to the configuration file</param>
    /// <param name="logger">Logger for diagnostic output</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null</exception>
    /// <exception cref="ArgumentException">Thrown when configFilePath is invalid</exception>
    public ControllerMapper(string configFilePath, ILogger<ControllerMapper> logger)
    {
        if (string.IsNullOrWhiteSpace(configFilePath))
            throw new ArgumentException("Config file path cannot be null or empty", nameof(configFilePath));
        
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configFilePath = configFilePath;
        _mappingProfiles = new Dictionary<string, InputMapping>();
        _currentProfile = "Default";

        CreateDefaultMappings();
        LoadMappings();
        
        _logger.LogDebug("ControllerMapper created with config: {ConfigPath}", configFilePath);
    }

    /// <summary>
    /// Gets the current input mapping
    /// </summary>
    /// <returns>Current input mapping</returns>
    public InputMapping GetCurrentMapping()
    {
        return _mappingProfiles.TryGetValue(_currentProfile, out var mapping) 
            ? mapping 
            : _mappingProfiles["Default"];
    }

    /// <summary>
    /// Sets the current mapping profile
    /// </summary>
    /// <param name="profileName">Profile name to activate</param>
    /// <returns>True if profile was set successfully, false otherwise</returns>
    public bool SetCurrentProfile(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            _logger.LogWarning("Invalid profile name: {ProfileName}", profileName);
            return false;
        }

        if (!_mappingProfiles.ContainsKey(profileName))
        {
            _logger.LogWarning("Profile not found: {ProfileName}", profileName);
            return false;
        }

        var oldProfile = _currentProfile;
        _currentProfile = profileName;

        ProfileChanged?.Invoke(this, new MappingProfileChangedEventArgs
        {
            OldProfile = oldProfile,
            NewProfile = _currentProfile,
            Mapping = GetCurrentMapping()
        });

        _logger.LogInformation("Switched to mapping profile: {ProfileName}", profileName);
        return true;
    }

    /// <summary>
    /// Creates a new mapping profile
    /// </summary>
    /// <param name="profileName">Name of the new profile</param>
    /// <param name="mapping">Input mapping for the profile</param>
    /// <returns>True if profile was created successfully, false otherwise</returns>
    public bool CreateProfile(string profileName, InputMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(mapping);

        if (string.IsNullOrWhiteSpace(profileName))
        {
            _logger.LogWarning("Invalid profile name: {ProfileName}", profileName);
            return false;
        }

        if (_mappingProfiles.ContainsKey(profileName))
        {
            _logger.LogWarning("Profile already exists: {ProfileName}", profileName);
            return false;
        }

        _mappingProfiles[profileName] = mapping;
        _logger.LogInformation("Created mapping profile: {ProfileName}", profileName);
        return true;
    }

    /// <summary>
    /// Updates an existing mapping profile
    /// </summary>
    /// <param name="profileName">Name of the profile to update</param>
    /// <param name="mapping">New input mapping</param>
    /// <returns>True if profile was updated successfully, false otherwise</returns>
    public bool UpdateProfile(string profileName, InputMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(mapping);

        if (string.IsNullOrWhiteSpace(profileName))
        {
            _logger.LogWarning("Invalid profile name: {ProfileName}", profileName);
            return false;
        }

        if (!_mappingProfiles.ContainsKey(profileName))
        {
            _logger.LogWarning("Profile not found: {ProfileName}", profileName);
            return false;
        }

        _mappingProfiles[profileName] = mapping;
        _logger.LogInformation("Updated mapping profile: {ProfileName}", profileName);
        return true;
    }

    /// <summary>
    /// Deletes a mapping profile
    /// </summary>
    /// <param name="profileName">Name of the profile to delete</param>
    /// <returns>True if profile was deleted successfully, false otherwise</returns>
    public bool DeleteProfile(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName) || profileName == "Default")
        {
            _logger.LogWarning("Cannot delete profile: {ProfileName}", profileName);
            return false;
        }

        if (!_mappingProfiles.ContainsKey(profileName))
        {
            _logger.LogWarning("Profile not found: {ProfileName}", profileName);
            return false;
        }

        _mappingProfiles.Remove(profileName);

        // Switch to default if current profile was deleted
        if (_currentProfile == profileName)
        {
            SetCurrentProfile("Default");
        }

        _logger.LogInformation("Deleted mapping profile: {ProfileName}", profileName);
        return true;
    }

    /// <summary>
    /// Saves all mapping profiles to file
    /// </summary>
    /// <returns>True if save was successful, false otherwise</returns>
    public bool SaveMappings()
    {
        try
        {
            var config = new MappingConfiguration
            {
                CurrentProfile = _currentProfile,
                Profiles = _mappingProfiles
            };

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Ensure directory exists
            var directory = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_configFilePath, json);
            _logger.LogInformation("Saved mapping configuration to: {ConfigPath}", _configFilePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save mapping configuration");
            return false;
        }
    }

    /// <summary>
    /// Loads mapping profiles from file
    /// </summary>
    /// <returns>True if load was successful, false otherwise</returns>
    public bool LoadMappings()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                _logger.LogInformation("Config file not found, using defaults: {ConfigPath}", _configFilePath);
                return true;
            }

            var json = File.ReadAllText(_configFilePath);
            var config = JsonSerializer.Deserialize<MappingConfiguration>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (config != null)
            {
                _mappingProfiles.Clear();
                
                // Ensure default profile exists
                CreateDefaultMappings();
                
                // Load saved profiles
                foreach (var profile in config.Profiles)
                {
                    _mappingProfiles[profile.Key] = profile.Value;
                }

                // Set current profile
                if (!string.IsNullOrEmpty(config.CurrentProfile) && 
                    _mappingProfiles.ContainsKey(config.CurrentProfile))
                {
                    _currentProfile = config.CurrentProfile;
                }

                _logger.LogInformation("Loaded mapping configuration from: {ConfigPath}", _configFilePath);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load mapping configuration");
        }

        return false;
    }

    /// <summary>
    /// Creates default mapping profiles
    /// </summary>
    private void CreateDefaultMappings()
    {
        // Default keyboard mapping
        var defaultMapping = new InputMapping
        {
            Name = "Default",
            KeyboardMappings = new Dictionary<Keys, NESButton>
            {
                [Keys.Z] = NESButton.A,
                [Keys.X] = NESButton.B,
                [Keys.Enter] = NESButton.Start,
                [Keys.RightShift] = NESButton.Select,
                [Keys.Up] = NESButton.Up,
                [Keys.Down] = NESButton.Down,
                [Keys.Left] = NESButton.Left,
                [Keys.Right] = NESButton.Right
            },
            GamepadMappings = new Dictionary<Buttons, NESButton>
            {
                [Buttons.A] = NESButton.A,
                [Buttons.B] = NESButton.B,
                [Buttons.Start] = NESButton.Start,
                [Buttons.Back] = NESButton.Select,
                [Buttons.DPadUp] = NESButton.Up,
                [Buttons.DPadDown] = NESButton.Down,
                [Buttons.DPadLeft] = NESButton.Left,
                [Buttons.DPadRight] = NESButton.Right
            }
        };

        _mappingProfiles["Default"] = defaultMapping;

        // Alternative WASD mapping
        var wasdMapping = new InputMapping
        {
            Name = "WASD",
            KeyboardMappings = new Dictionary<Keys, NESButton>
            {
                [Keys.J] = NESButton.A,
                [Keys.K] = NESButton.B,
                [Keys.Enter] = NESButton.Start,
                [Keys.RightShift] = NESButton.Select,
                [Keys.W] = NESButton.Up,
                [Keys.S] = NESButton.Down,
                [Keys.A] = NESButton.Left,
                [Keys.D] = NESButton.Right
            },
            GamepadMappings = defaultMapping.GamepadMappings
        };

        _mappingProfiles["WASD"] = wasdMapping;
    }

    /// <summary>
    /// Disposes of controller mapper resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            SaveMappings();
            ProfileChanged = null;
            _mappingProfiles.Clear();
            _disposed = true;

            _logger.LogDebug("ControllerMapper disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during controller mapper disposal");
        }
    }
}

/// <summary>
/// Input mapping configuration
/// </summary>
public sealed class InputMapping
{
    /// <summary>
    /// Gets or sets the mapping name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the keyboard mappings
    /// </summary>
    public Dictionary<Keys, NESButton> KeyboardMappings { get; init; } = new();

    /// <summary>
    /// Gets the gamepad mappings
    /// </summary>
    public Dictionary<Buttons, NESButton> GamepadMappings { get; init; } = new();
}

/// <summary>
/// Mapping configuration for serialization
/// </summary>
internal sealed class MappingConfiguration
{
    public string CurrentProfile { get; set; } = "Default";
    public Dictionary<string, InputMapping> Profiles { get; set; } = new();
}

/// <summary>
/// Event arguments for mapping profile changes
/// </summary>
public sealed class MappingProfileChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the old profile name
    /// </summary>
    public string OldProfile { get; init; } = string.Empty;

    /// <summary>
    /// Gets the new profile name
    /// </summary>
    public string NewProfile { get; init; } = string.Empty;

    /// <summary>
    /// Gets the new input mapping
    /// </summary>
    public InputMapping Mapping { get; init; } = new();
}
