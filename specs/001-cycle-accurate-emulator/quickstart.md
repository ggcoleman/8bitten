# Quick Start Guide: 8Bitten - Cycle-Accurate NES Emulator

**Date**: 2025-10-26
**Purpose**: Get started with development and usage of 8Bitten NES emulator

## Development Setup

### Prerequisites
- .NET 9.0 SDK or later
- Visual Studio 2022 or VS Code with C# extension
- Git for version control

### Clone and Build
```bash
git clone https://github.com/ggcoleman/8bitten.git
cd 8bitten
git checkout 001-cycle-accurate-emulator
dotnet restore
dotnet build
```

### Run Tests
```bash
# Run all tests
dotnet test

# Run specific test categories
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration
dotnet test --filter Category=Performance
```

## Usage Modes

### 1. Headless Mode (Testing/Automation)
```bash
# Run ROM test suite
dotnet run --project src/Emulator.Console/Headless -- --rom test.nes --test-mode

# Automated compatibility testing
dotnet run --project src/Emulator.Console/Headless -- --rom game.nes --frames 1000 --output results.json
```

### 2. Command Line Gaming
```bash
# Quick game launch
dotnet run --project src/Emulator.Console/CLI -- --rom game.nes

# With specific configuration
dotnet run --project src/Emulator.Console/CLI -- --rom game.nes --scale 3x --fullscreen
```

### 3. GUI Mode
```bash
# Launch graphical interface
dotnet run --project src/Emulator.Console/GUI

# Or build and run executable
dotnet publish -c Release
./bin/Release/net9.0/8Bitten.exe
```

## Configuration

### Default Configuration Location
- **Windows**: `%APPDATA%/8Bitten/config.json`
- **macOS**: `~/Library/Application Support/8Bitten/config.json`
- **Linux**: `~/.config/8Bitten/config.json`

### Sample Configuration
```json
{
  "graphics": {
    "scalingFactor": 3,
    "filterType": "nearest",
    "aspectRatio": "4:3",
    "fullscreen": false,
    "vsync": true
  },
  "audio": {
    "sampleRate": 44100,
    "bufferSize": 1024,
    "volume": 0.8,
    "deviceId": "default"
  },
  "performance": {
    "accuracyMode": "high",
    "frameLimit": true,
    "fastForward": 2.0,
    "rewindBuffer": 300
  },
  "input": {
    "player1": {
      "a": "Z",
      "b": "X", 
      "select": "RShift",
      "start": "Enter",
      "up": "Up",
      "down": "Down",
      "left": "Left",
      "right": "Right"
    }
  },
  "mcp": {
    "enabled": true,
    "port": 8080,
    "authRequired": true,
    "maxSessions": 10
  }
}
```

## AI/MCP Integration

### Start MCP Server
```bash
# Enable MCP interface
dotnet run --project src/Emulator.Console/CLI -- --rom game.nes --mcp-server --port 8080
```

### Connect AI Agent
```python
# Python example using requests
import requests
import base64

# Authenticate and create session
auth_response = requests.post('http://localhost:8080/mcp/session', json={
    'authToken': 'your-token-here',
    'agentId': 'my-ai-agent',
    'permissions': ['input', 'state', 'saveload', 'metrics']
})

session_id = auth_response.json()['sessionId']

# Send controller input
requests.post(f'http://localhost:8080/mcp/session/{session_id}/input', json={
    'buttons': {
        'a': True,
        'b': False,
        'start': False,
        'select': False,
        'up': False,
        'down': False,
        'left': False,
        'right': True
    },
    'duration': 5
})

# Get game state
state_response = requests.get(f'http://localhost:8080/mcp/session/{session_id}/state')
game_state = state_response.json()

# Decode screen buffer
screen_data = base64.b64decode(game_state['screenBuffer'])
# screen_data is now 256x240x3 RGB pixel array
```

## Development Workflow

### 1. Test-Driven Development
```bash
# Create failing test first
# tests/Unit/CPU/InstructionTests.cs

[Fact]
public void ADC_Immediate_ShouldAddToAccumulator()
{
    // Arrange
    var cpu = new CPU();
    cpu.A = 0x10;
    
    // Act
    cpu.ExecuteInstruction(0x69, 0x20); // ADC #$20
    
    // Assert
    Assert.Equal(0x30, cpu.A);
}

# Run test (should fail)
dotnet test --filter ADC_Immediate_ShouldAddToAccumulator

# Implement feature
# src/Core/CPU/Instructions.cs

# Run test again (should pass)
dotnet test --filter ADC_Immediate_ShouldAddToAccumulator
```

### 2. Component Development Order
1. **Memory System** - Foundation for all components
2. **CPU Core** - 6502 instruction set and timing
3. **PPU Core** - Basic rendering pipeline
4. **APU Core** - Audio generation
5. **Mappers** - Cartridge support (NROM, MMC1, MMC3)
6. **Integration** - Component synchronization
7. **Interfaces** - CLI, GUI, MCP

### 3. Performance Testing
```bash
# Run performance benchmarks
dotnet run --project tests/Performance -- --rom test.nes --duration 60s

# Profile specific components
dotnet run --project tests/Performance -- --component CPU --iterations 1000000
```

## ROM Testing

### Blargg's Test ROMs
```bash
# Download test ROMs (not included in repository)
mkdir test-roms
cd test-roms
wget https://github.com/christopherpow/nes-test-roms/archive/master.zip
unzip master.zip

# Run CPU tests
dotnet run --project src/Emulator.Console/Headless -- --rom test-roms/cpu_exec_space/test_cpu_exec_space_ppuio.nes --test-mode

# Run PPU tests  
dotnet run --project src/Emulator.Console/Headless -- --rom test-roms/ppu_vbl_nmi/rom_singles/01-vbl_basics.nes --test-mode
```

### Expected Test Results
- **CPU Tests**: All Blargg CPU tests should pass
- **PPU Tests**: Basic PPU functionality tests should pass
- **APU Tests**: Audio generation tests should pass
- **Integration Tests**: Component interaction tests should pass

## Troubleshooting

### Common Issues

**Build Errors**
```bash
# Clear build cache
dotnet clean
dotnet restore
dotnet build
```

**Performance Issues**
```bash
# Check performance mode
dotnet run --project src/Emulator.Console/CLI -- --rom game.nes --performance-mode speed

# Enable profiling
dotnet run --project src/Emulator.Console/CLI -- --rom game.nes --profile
```

**Audio Problems**
```bash
# List audio devices
dotnet run --project src/Emulator.Console/CLI -- --list-audio-devices

# Test with different buffer size
dotnet run --project src/Emulator.Console/CLI -- --rom game.nes --audio-buffer 2048
```

### Debug Mode
```bash
# Enable debug logging with rich CLI output
dotnet run --project src/Emulator.Console/CLI -- --rom game.nes --log-level Debug

# CPU instruction tracing with formatted output
dotnet run --project src/Emulator.Console/CLI -- --rom game.nes --trace-cpu

# Interactive diagnostic mode with live tables
dotnet run --project src/Emulator.Console/CLI -- --rom game.nes --interactive-debug
```

## Hardware Reference

### Comprehensive Documentation Sources
8Bitten references multiple authoritative sources for maximum accuracy:

#### **Primary Community Resources**
- **[NESdev Wiki](https://www.nesdev.org/wiki/Nesdev_Wiki)** - Comprehensive community documentation
  - [CPU Reference](https://www.nesdev.org/wiki/CPU) - 6502 processor documentation
  - [PPU Reference](https://www.nesdev.org/wiki/PPU) - Picture Processing Unit
  - [APU Reference](https://www.nesdev.org/wiki/APU) - Audio Processing Unit
  - [Mapper Reference](https://www.nesdev.org/wiki/Mapper) - Cartridge mappers
  - [Timing Reference](https://www.nesdev.org/wiki/Cycle_reference_chart) - Cycle timing
- **[NESdev Forums](https://forums.nesdev.org/)** - Active community discussions
- **[6502.org](http://www.6502.org/)** - Authoritative 6502 documentation

#### **Reference Emulator Implementations**
- **[Mesen2](https://github.com/SourMesen/Mesen2)** - Highly accurate open-source emulator
- **[FCEUX](https://github.com/TASEmulators/fceux)** - Well-documented with extensive testing
- **[puNES](https://github.com/punesemu/puNES)** - Another accurate implementation

#### **Hardware Analysis Resources**
- **[Visual6502](http://visual6502.org/)** - Transistor-level 6502 simulation
- **MiSTer FPGA NES Core** - Hardware-accurate FPGA implementation
- **Nintendo Patent Documents** - Official hardware specifications

#### **Test ROM Collections**
- **[Blargg's Test ROMs](https://github.com/christopherpow/nes-test-roms)** - Comprehensive validation suites
- **Kevin Horton's Test ROMs** - Additional hardware validation
- **Shay Green's Test ROMs** - Audio and timing validation

### CLI Diagnostic Features
The command-line interface uses Spectre.Console for rich output:

```bash
# Progress bars for ROM loading and testing
dotnet run --project src/Emulator.Console/CLI -- --rom game.nes --show-progress

# Formatted tables for register states
dotnet run --project src/Emulator.Console/CLI -- --rom game.nes --show-registers

# Color-coded diagnostic output
dotnet run --project src/Emulator.Console/CLI -- --rom game.nes --colored-output

# Interactive performance monitoring
dotnet run --project src/Emulator.Console/CLI -- --rom game.nes --performance-monitor
```

## Next Steps

1. **Read Architecture Documentation**: `docs/Architecture.md`
2. **Study Component Documentation**: `docs/Components/`
3. **Review API Documentation**: `docs/API/`
4. **Join Development**: See `CONTRIBUTING.md`

For more detailed information, see the complete documentation in the `docs/` directory.
