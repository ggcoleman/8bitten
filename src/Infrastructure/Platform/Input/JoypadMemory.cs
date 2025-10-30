using System;
using System.Collections.Generic;
using EightBitten.Core.Contracts;
using Microsoft.Extensions.Logging;

namespace EightBitten.Infrastructure.Platform.Input;

/// <summary>
/// NES Joypad ($4016/$4017) memory-mapped device.
/// Implements standard strobe/latch + serial read behavior for both controllers.
/// </summary>
public sealed class JoypadMemory : IMemoryMappedComponent
{
    private readonly ILogger<JoypadMemory> _logger;
    private readonly InputManager _input;
    private readonly List<MemoryRange> _ranges = new() { new MemoryRange(0x4016, 0x4017) };

    // Strobe and shift/latch state
    private bool _strobe;
    private byte _latchedP1;
    private byte _latchedP2;
    private int _indexP1;
    private int _indexP2;

    public string Name => "Joypad";
    public bool IsEnabled { get; set; } = true;
    public IReadOnlyList<MemoryRange> AddressRanges => _ranges;

    public JoypadMemory(ILogger<JoypadMemory> logger, InputManager input)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _input = input ?? throw new ArgumentNullException(nameof(input));
    }

    public void Initialize() => Reset();

    public void Reset()
    {
        _strobe = false;
        _latchedP1 = 0;
        _latchedP2 = 0;
        _indexP1 = 0;
        _indexP2 = 0;
    }

    public int ExecuteCycle() => 1; // No timing behavior needed here

    public byte ReadByte(ushort address)
    {
        if (!IsEnabled) return 0x00;

        // When strobe is high, reads should reflect current A button state repeatedly.
        // When strobe is low, shift out latched bits A,B,Select,Start,Up,Down,Left,Right
        // then 1s afterwards.
        return address switch
        {
            0x4016 => ReadPort(0),
            0x4017 => ReadPort(1),
            _ => 0x00
        };
    }

    public void WriteByte(ushort address, byte value)
    {
        if (!IsEnabled) return;

        if (address == 0x4016)
        {
            var newStrobe = (value & 0x01) != 0;

            // On 1->0 transition, latch controller states and reset shift indices
            if (_strobe && !newStrobe)
            {
                _latchedP1 = _input.Player1State.GetRawState();
                _latchedP2 = _input.Player2State.GetRawState();
                _indexP1 = 0;
                _indexP2 = 0;

                #pragma warning disable CA1848
                _logger.LogDebug("Joypad latched: P1={P1:X2} P2={P2:X2}", _latchedP1, _latchedP2);
                #pragma warning restore CA1848
            }

            _strobe = newStrobe;
        }
        else if (address == 0x4017)
        {
            // Some software may write here; generally ignored for standard pads
        }
    }

    public bool HandlesAddress(ushort address)
    {
        foreach (var r in _ranges)
        {
            if (r.Contains(address)) return true;
        }
        return false;
    }

    private byte ReadPort(int port)
    {
        if (_strobe)
        {
            // Return current A button while strobe high
            var raw = port == 0 ? _input.Player1State.GetRawState() : _input.Player2State.GetRawState();
            return (byte)(raw & 0x01);
        }

        // Shift from latched state
        ref int index = ref (port == 0 ? ref _indexP1 : ref _indexP2);
        var latched = port == 0 ? _latchedP1 : _latchedP2;

        byte bit;
        if (index < 8)
        {
            bit = (byte)((latched >> index) & 0x01);
            index++;
        }
        else
        {
            // After 8 reads, hardware returns 1 on subsequent reads
            bit = 0x01;
        }

        return bit;
    }

    public ComponentState GetState()
    {
        return new JoypadState
        {
            ComponentName = Name,
            Strobe = _strobe,
            LatchedP1 = _latchedP1,
            LatchedP2 = _latchedP2,
            IndexP1 = _indexP1,
            IndexP2 = _indexP2
        };
    }

    public void SetState(ComponentState state)
    {
        if (state is JoypadState s)
        {
            _strobe = s.Strobe;
            _latchedP1 = s.LatchedP1;
            _latchedP2 = s.LatchedP2;
            _indexP1 = s.IndexP1;
            _indexP2 = s.IndexP2;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Serializable Joypad state
/// </summary>
public sealed class JoypadState : ComponentState
{
    public bool Strobe { get; set; }
    public byte LatchedP1 { get; set; }
    public byte LatchedP2 { get; set; }
    public int IndexP1 { get; set; }
    public int IndexP2 { get; set; }
}

