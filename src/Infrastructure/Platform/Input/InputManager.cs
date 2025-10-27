using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Input;

namespace EightBitten.Infrastructure.Platform.Input;

/// <summary>
/// Input manager for handling keyboard and gamepad input
/// Maps input to NES controller buttons with configurable key bindings
/// </summary>
public sealed class InputManager : IDisposable
{
    private readonly ILogger<InputManager> _logger;
    private readonly Dictionary<Keys, NESButton> _keyboardMappings;
    private readonly Dictionary<Buttons, NESButton> _gamepadMappings;
    private KeyboardState _previousKeyboardState;
    private KeyboardState _currentKeyboardState;
    private GamePadState _previousGamePadState;
    private GamePadState _currentGamePadState;
    private bool _disposed;

    /// <summary>
    /// Gets whether the input manager is initialized
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Gets the current controller state for Player 1
    /// </summary>
    public NESControllerState Player1State { get; private set; }

    /// <summary>
    /// Gets the current controller state for Player 2
    /// </summary>
    public NESControllerState Player2State { get; private set; }

    /// <summary>
    /// Event raised when input state changes
    /// </summary>
    public event EventHandler<InputStateChangedEventArgs>? InputStateChanged;

    /// <summary>
    /// Initializes a new instance of the InputManager class
    /// </summary>
    /// <param name="logger">Logger for diagnostic output</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null</exception>
    public InputManager(ILogger<InputManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyboardMappings = new Dictionary<Keys, NESButton>();
        _gamepadMappings = new Dictionary<Buttons, NESButton>();
        
        SetupDefaultMappings();
        _logger.LogDebug("InputManager created");
    }

    /// <summary>
    /// Initializes the input manager
    /// </summary>
    /// <returns>True if initialization succeeded, false otherwise</returns>
    public bool Initialize()
    {
        if (_disposed)
        {
            _logger.LogError("Cannot initialize disposed input manager");
            return false;
        }

        try
        {
            // Initialize input states
            _currentKeyboardState = Keyboard.GetState();
            _previousKeyboardState = _currentKeyboardState;
            _currentGamePadState = GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One);
            _previousGamePadState = _currentGamePadState;

            Player1State = new NESControllerState();
            Player2State = new NESControllerState();

            IsInitialized = true;
            _logger.LogInformation("Input manager initialized");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize input manager");
            return false;
        }
    }

    /// <summary>
    /// Updates input state and processes input events
    /// </summary>
    public void Update()
    {
        if (!IsInitialized || _disposed)
        {
            return;
        }

        try
        {
            // Update input states
            _previousKeyboardState = _currentKeyboardState;
            _previousGamePadState = _currentGamePadState;
            _currentKeyboardState = Keyboard.GetState();
            _currentGamePadState = GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One);

            // Process Player 1 input
            var newPlayer1State = ProcessPlayerInput(_currentKeyboardState, _currentGamePadState);
            var player1Changed = !Player1State.Equals(newPlayer1State);
            Player1State = newPlayer1State;

            // Process Player 2 input (gamepad only for now)
            var gamepad2State = GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.Two);
            var newPlayer2State = ProcessPlayerInput(new KeyboardState(), gamepad2State);
            var player2Changed = !Player2State.Equals(newPlayer2State);
            Player2State = newPlayer2State;

            // Raise events if state changed
            if (player1Changed || player2Changed)
            {
                InputStateChanged?.Invoke(this, new InputStateChangedEventArgs
                {
                    Player1State = Player1State,
                    Player2State = Player2State,
                    Player1Changed = player1Changed,
                    Player2Changed = player2Changed
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating input state");
        }
    }

    /// <summary>
    /// Sets keyboard mapping for a specific NES button
    /// </summary>
    /// <param name="key">Keyboard key</param>
    /// <param name="button">NES button</param>
    public void SetKeyboardMapping(Keys key, NESButton button)
    {
        _keyboardMappings[key] = button;
        _logger.LogDebug("Keyboard mapping set: {Key} -> {Button}", key, button);
    }

    /// <summary>
    /// Sets gamepad mapping for a specific NES button
    /// </summary>
    /// <param name="gamepadButton">Gamepad button</param>
    /// <param name="nesButton">NES button</param>
    public void SetGamepadMapping(Buttons gamepadButton, NESButton nesButton)
    {
        _gamepadMappings[gamepadButton] = nesButton;
        _logger.LogDebug("Gamepad mapping set: {GamepadButton} -> {NESButton}", gamepadButton, nesButton);
    }

    /// <summary>
    /// Gets the current keyboard mappings
    /// </summary>
    /// <returns>Dictionary of keyboard mappings</returns>
    public IReadOnlyDictionary<Keys, NESButton> GetKeyboardMappings()
    {
        return _keyboardMappings;
    }

    /// <summary>
    /// Gets the current gamepad mappings
    /// </summary>
    /// <returns>Dictionary of gamepad mappings</returns>
    public IReadOnlyDictionary<Buttons, NESButton> GetGamepadMappings()
    {
        return _gamepadMappings;
    }

    /// <summary>
    /// Checks if a specific key was just pressed
    /// </summary>
    /// <param name="key">Key to check</param>
    /// <returns>True if key was just pressed, false otherwise</returns>
    public bool IsKeyPressed(Keys key)
    {
        return _currentKeyboardState.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);
    }

    /// <summary>
    /// Checks if a specific gamepad button was just pressed
    /// </summary>
    /// <param name="button">Button to check</param>
    /// <returns>True if button was just pressed, false otherwise</returns>
    public bool IsButtonPressed(Buttons button)
    {
        return _currentGamePadState.IsButtonDown(button) && !_previousGamePadState.IsButtonDown(button);
    }

    /// <summary>
    /// Sets up default input mappings
    /// </summary>
    private void SetupDefaultMappings()
    {
        // Default keyboard mappings (Player 1)
        _keyboardMappings[Keys.Z] = NESButton.A;
        _keyboardMappings[Keys.X] = NESButton.B;
        _keyboardMappings[Keys.Enter] = NESButton.Start;
        _keyboardMappings[Keys.RightShift] = NESButton.Select;
        _keyboardMappings[Keys.Up] = NESButton.Up;
        _keyboardMappings[Keys.Down] = NESButton.Down;
        _keyboardMappings[Keys.Left] = NESButton.Left;
        _keyboardMappings[Keys.Right] = NESButton.Right;

        // Default gamepad mappings
        _gamepadMappings[Buttons.A] = NESButton.A;
        _gamepadMappings[Buttons.B] = NESButton.B;
        _gamepadMappings[Buttons.Start] = NESButton.Start;
        _gamepadMappings[Buttons.Back] = NESButton.Select;
        _gamepadMappings[Buttons.DPadUp] = NESButton.Up;
        _gamepadMappings[Buttons.DPadDown] = NESButton.Down;
        _gamepadMappings[Buttons.DPadLeft] = NESButton.Left;
        _gamepadMappings[Buttons.DPadRight] = NESButton.Right;

        _logger.LogDebug("Default input mappings configured");
    }

    /// <summary>
    /// Processes input for a player
    /// </summary>
    /// <param name="keyboardState">Current keyboard state</param>
    /// <param name="gamepadState">Current gamepad state</param>
    /// <returns>NES controller state</returns>
    private NESControllerState ProcessPlayerInput(KeyboardState keyboardState, GamePadState gamepadState)
    {
        var state = new NESControllerState();

        // Process keyboard input
        foreach (var mapping in _keyboardMappings)
        {
            if (keyboardState.IsKeyDown(mapping.Key))
            {
                state.SetButton(mapping.Value, true);
            }
        }

        // Process gamepad input (overrides keyboard)
        foreach (var mapping in _gamepadMappings)
        {
            if (gamepadState.IsButtonDown(mapping.Key))
            {
                state.SetButton(mapping.Value, true);
            }
        }

        // Handle analog stick as D-pad
        var leftStick = gamepadState.ThumbSticks.Left;
        if (leftStick.Y > 0.5f) state.SetButton(NESButton.Up, true);
        if (leftStick.Y < -0.5f) state.SetButton(NESButton.Down, true);
        if (leftStick.X < -0.5f) state.SetButton(NESButton.Left, true);
        if (leftStick.X > 0.5f) state.SetButton(NESButton.Right, true);

        return state;
    }

    /// <summary>
    /// Disposes of input resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            InputStateChanged = null;
            _keyboardMappings.Clear();
            _gamepadMappings.Clear();
            IsInitialized = false;
            _disposed = true;

            _logger.LogDebug("InputManager disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during input manager disposal");
        }
    }
}

/// <summary>
/// NES controller buttons enumeration
/// </summary>
public enum NESButton
{
    A,
    B,
    Select,
    Start,
    Up,
    Down,
    Left,
    Right
}

/// <summary>
/// Represents the state of a NES controller
/// </summary>
public struct NESControllerState : IEquatable<NESControllerState>
{
    private byte _buttonState;

    /// <summary>
    /// Gets whether the A button is pressed
    /// </summary>
    public bool A => (_buttonState & 0x01) != 0;

    /// <summary>
    /// Gets whether the B button is pressed
    /// </summary>
    public bool B => (_buttonState & 0x02) != 0;

    /// <summary>
    /// Gets whether the Select button is pressed
    /// </summary>
    public bool Select => (_buttonState & 0x04) != 0;

    /// <summary>
    /// Gets whether the Start button is pressed
    /// </summary>
    public bool Start => (_buttonState & 0x08) != 0;

    /// <summary>
    /// Gets whether the Up button is pressed
    /// </summary>
    public bool Up => (_buttonState & 0x10) != 0;

    /// <summary>
    /// Gets whether the Down button is pressed
    /// </summary>
    public bool Down => (_buttonState & 0x20) != 0;

    /// <summary>
    /// Gets whether the Left button is pressed
    /// </summary>
    public bool Left => (_buttonState & 0x40) != 0;

    /// <summary>
    /// Gets whether the Right button is pressed
    /// </summary>
    public bool Right => (_buttonState & 0x80) != 0;

    /// <summary>
    /// Sets the state of a specific button
    /// </summary>
    /// <param name="button">Button to set</param>
    /// <param name="pressed">Whether the button is pressed</param>
    public void SetButton(NESButton button, bool pressed)
    {
        var mask = (byte)(1 << (int)button);
        if (pressed)
        {
            _buttonState |= mask;
        }
        else
        {
            _buttonState &= (byte)~mask;
        }
    }

    /// <summary>
    /// Gets the raw button state as a byte
    /// </summary>
    /// <returns>Button state byte</returns>
    public byte GetRawState() => _buttonState;

    public bool Equals(NESControllerState other) => _buttonState == other._buttonState;
    public override bool Equals(object? obj) => obj is NESControllerState other && Equals(other);
    public override int GetHashCode() => _buttonState.GetHashCode();
    public static bool operator ==(NESControllerState left, NESControllerState right) => left.Equals(right);
    public static bool operator !=(NESControllerState left, NESControllerState right) => !left.Equals(right);
}

/// <summary>
/// Event arguments for input state changes
/// </summary>
public sealed class InputStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the current Player 1 controller state
    /// </summary>
    public NESControllerState Player1State { get; init; }

    /// <summary>
    /// Gets the current Player 2 controller state
    /// </summary>
    public NESControllerState Player2State { get; init; }

    /// <summary>
    /// Gets whether Player 1 state changed
    /// </summary>
    public bool Player1Changed { get; init; }

    /// <summary>
    /// Gets whether Player 2 state changed
    /// </summary>
    public bool Player2Changed { get; init; }
}
