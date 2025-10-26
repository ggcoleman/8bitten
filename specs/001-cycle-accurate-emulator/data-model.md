# Data Model: 8Bitten - Cycle-Accurate NES Emulator

**Date**: 2025-10-26  
**Purpose**: Define core entities, relationships, and state management for 8Bitten NES emulation

## Core Emulation Entities

### CPU State
Represents the complete state of the 6502 processor.

**Properties**:
- `A`: Accumulator register (byte)
- `X`: X index register (byte) 
- `Y`: Y index register (byte)
- `PC`: Program counter (ushort)
- `SP`: Stack pointer (byte)
- `P`: Processor status flags (byte)
- `CycleCount`: Current CPU cycle count (ulong)
- `IRQPending`: Interrupt request pending (bool)
- `NMIPending`: Non-maskable interrupt pending (bool)

**State Transitions**:
- Reset → Initialize registers to known state
- Execute → Update registers and cycle count
- Interrupt → Save state and jump to interrupt vector

### PPU State  
Represents the Picture Processing Unit state and rendering pipeline.

**Properties**:
- `VRAM`: Video RAM (8KB byte array)
- `OAM`: Object Attribute Memory for sprites (256 bytes)
- `Registers`: PPU control/status registers (8 bytes)
- `Scanline`: Current scanline being rendered (ushort)
- `Cycle`: Current cycle within scanline (ushort)
- `FrameCount`: Total frames rendered (ulong)
- `BackgroundBuffer`: Background rendering buffer
- `SpriteBuffer`: Sprite rendering buffer
- `OutputBuffer`: Final frame buffer (256x240 pixels)

**State Transitions**:
- VBlank → Trigger NMI, reset scanline counters
- Render → Process background and sprites
- HBlank → Prepare next scanline

### APU State
Represents the Audio Processing Unit state and sound generation.

**Properties**:
- `Pulse1`: Pulse wave channel 1 state
- `Pulse2`: Pulse wave channel 2 state  
- `Triangle`: Triangle wave channel state
- `Noise`: Noise channel state
- `DMC`: Delta Modulation Channel state
- `FrameCounter`: APU frame counter state
- `AudioBuffer`: Output audio sample buffer
- `SampleRate`: Audio output sample rate (int)

**Validation Rules**:
- Sample rate must be 44100Hz or 48000Hz
- Audio buffer size must be power of 2
- All channels must maintain proper timing synchronization

### Memory Map
Represents the complete NES memory layout and address space.

**Properties**:
- `RAM`: Internal RAM (2KB, mirrored to 8KB)
- `PPURegisters`: PPU memory-mapped registers
- `APURegisters`: APU memory-mapped registers  
- `CartridgeSpace`: Cartridge ROM/RAM mapping
- `AddressDecoder`: Memory address decoding logic

**Relationships**:
- Contains references to CPU, PPU, APU for memory-mapped I/O
- Manages cartridge mapper for bank switching
- Handles address mirroring and decoding

### ROM Cartridge
Represents a NES game cartridge with header and ROM data.

**Properties**:
- `Header`: iNES header information (16 bytes)
- `PRGROM`: Program ROM data (variable size)
- `CHRROM`: Character ROM data (variable size)  
- `MapperNumber`: Cartridge mapper type (byte)
- `PRGRAMSize`: Program RAM size (int)
- `CHRRAMSize`: Character RAM size (int)
- `Mirroring`: Nametable mirroring mode (enum)

**Validation Rules**:
- Header must be valid iNES format
- ROM sizes must match header specifications
- Mapper number must be supported

## Configuration Entities

### Configuration Profile
Represents user preferences and emulator settings.

**Properties**:
- `Graphics`: Graphics configuration settings
- `Audio`: Audio configuration settings
- `Input`: Controller input mappings
- `Performance`: Performance optimization settings
- `MCP`: AI agent interface settings

**Persistence**:
- Stored as JSON in user application data directory
- Automatically loaded on startup
- Changes saved immediately when modified

### Graphics Configuration
**Properties**:
- `ScalingFactor`: Display scaling (1x, 2x, 3x, 4x)
- `FilterType`: Scaling filter (nearest, linear, etc.)
- `AspectRatio`: Display aspect ratio mode
- `Fullscreen`: Fullscreen mode enabled (bool)
- `VSync`: Vertical synchronization enabled (bool)

### Audio Configuration  
**Properties**:
- `SampleRate`: Audio sample rate (44100, 48000)
- `BufferSize`: Audio buffer size in samples
- `Volume`: Master volume (0.0 to 1.0)
- `DeviceId`: Audio output device identifier

### Performance Configuration
**Properties**:
- `AccuracyMode`: Accuracy vs speed trade-off
- `FrameLimit`: Frame rate limiting enabled
- `FastForward`: Fast forward multiplier
- `RewindBuffer`: Rewind buffer size

## AI/MCP Entities

### AI Agent Session
Represents an active AI connection and session state.

**Properties**:
- `SessionId`: Unique session identifier (Guid)
- `AuthToken`: Authentication token for security
- `ConnectionTime`: Session start timestamp
- `LastActivity`: Last activity timestamp  
- `Permissions`: Agent permissions and capabilities
- `State`: Current session state (Connected, Authenticated, Active, Disconnected)

**Relationships**:
- Associated with specific emulator instance
- Can control game state and input
- Receives game state observations

### Game State Snapshot
Represents structured game data for AI consumption.

**Properties**:
- `FrameNumber`: Current frame number (ulong)
- `ScreenBuffer`: Raw pixel data (256x240x3 bytes)
- `MemoryState`: Relevant memory locations
- `InputState`: Current controller input
- `GameMetrics`: Score, lives, time, etc.
- `Timestamp`: Snapshot creation time

### Gameplay Metrics
Represents tracked statistics for AI training and analysis.

**Properties**:
- `SessionId`: Associated AI session
- `GameTitle`: ROM identification
- `StartTime`: Session start time
- `Duration`: Total play time
- `ActionsPerSecond`: Input rate statistics
- `PerformanceMetrics`: Frame rate, timing accuracy
- `GameSpecificMetrics`: Score, progress, completion

## State Management

### Save State
Complete emulator state snapshot for save/load functionality.

**Properties**:
- `Version`: Save state format version
- `Timestamp`: Creation timestamp
- `CPUState`: Complete CPU state
- `PPUState`: Complete PPU state  
- `APUState`: Complete APU state
- `MemoryState`: Complete memory contents
- `CartridgeState`: Cartridge-specific state

**Validation Rules**:
- Version must be compatible with current emulator
- All state components must be present and valid
- Checksum validation for data integrity

### Timing Synchronization
Manages timing coordination between emulation components.

**Properties**:
- `MasterClock`: Master timing reference
- `CPUCycles`: CPU cycle accumulator
- `PPUCycles`: PPU cycle accumulator  
- `APUCycles`: APU cycle accumulator
- `FrameSync`: Frame synchronization state

**Relationships**:
- Coordinates timing between CPU, PPU, APU
- Maintains cycle-accurate execution
- Handles timing edge cases and synchronization

## Entity Relationships

```
ROM Cartridge
    ↓ loads into
Memory Map ←→ CPU State
    ↓ controls      ↑ reads/writes
PPU State ←→ APU State
    ↓ generates     ↓ generates  
Video Output    Audio Output
    ↓ consumed by   ↓ consumed by
AI Agent Session ←→ Game State Snapshot
    ↓ produces
Gameplay Metrics

Configuration Profile
    ↓ configures
All Components

Save State
    ↓ preserves
All Component States
```

This data model provides the foundation for implementing 8Bitten, the cycle-accurate NES emulator, with proper separation of concerns, clear state management, and support for all required features including AI training capabilities.
