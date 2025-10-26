# Research: 8Bitten - Cycle-Accurate NES Emulator

**Date**: 2025-10-26  
**Purpose**: Resolve technical unknowns and establish technology choices for implementation

## Research Tasks Completed

### 1. Graphics Framework for C# Cross-Platform Development

**Decision**: MonoGame Framework  
**Rationale**: MonoGame provides excellent cross-platform support (Windows, macOS, Linux), hardware-accelerated 2D rendering suitable for pixel-perfect NES graphics, and mature ecosystem with good performance characteristics. It's specifically designed for games and emulators requiring precise timing and low-latency rendering.

**Alternatives considered**:
- **SkiaSharp**: Good for 2D graphics but lacks hardware acceleration and gaming-focused features
- **Avalonia**: UI framework but not optimized for real-time rendering
- **WPF/WinUI**: Platform-specific, doesn't meet cross-platform requirements
- **SDL2**: C library requiring P/Invoke, adds complexity

### 2. Audio Library for Low-Latency Audio Output

**Decision**: NAudio with WASAPI backend  
**Rationale**: NAudio provides excellent Windows audio support with low-latency WASAPI backend. For cross-platform support, will use OpenTK.Audio (OpenAL) on macOS/Linux. This combination provides the low-latency audio required for authentic NES sound reproduction.

**Alternatives considered**:
- **BASS.NET**: Commercial license required, excellent quality but cost prohibitive
- **OpenTK.Audio only**: Cross-platform but less optimal on Windows
- **System.Media**: Too basic, lacks low-latency capabilities
- **CSCore**: Good but less mature than NAudio

### 3. UI Framework for Configuration GUI

**Decision**: Avalonia UI  
**Rationale**: Modern, cross-platform XAML-based UI framework with excellent .NET integration. Provides native look and feel on each platform while maintaining consistent functionality. Good performance and supports modern UI patterns needed for configuration interfaces.

**Alternatives considered**:
- **MAUI**: Microsoft's cross-platform framework but primarily mobile-focused
- **WinUI 3**: Windows-only, doesn't meet cross-platform requirements
- **Eto.Forms**: Cross-platform but less modern and smaller ecosystem
- **Console-based TUI**: Too limited for complex configuration needs

### 4. MCP (Model Context Protocol) Implementation

**Decision**: Custom implementation using System.Net.Http and SignalR  
**Rationale**: MCP is a relatively new protocol, so custom implementation provides full control over the interface. SignalR provides real-time bidirectional communication needed for AI agent interaction, while System.Net.Http handles standard HTTP operations. This approach ensures optimal performance for AI training scenarios.

**Alternatives considered**:
- **Existing MCP libraries**: Limited availability and maturity for .NET
- **gRPC**: Good performance but more complex than needed for this use case
- **WebSockets directly**: Lower-level, SignalR provides better abstraction
- **REST API only**: Insufficient for real-time AI agent communication

### 5. C# Testing Framework and Tools

**Decision**: xUnit with FluentAssertions, NBomber for performance testing  
**Rationale**: xUnit is the modern standard for .NET testing with excellent async support and extensibility. FluentAssertions provides readable test assertions crucial for complex emulation validation. NBomber offers comprehensive performance testing capabilities needed for emulation benchmarking.

**Alternatives considered**:
- **NUnit**: Mature but less modern than xUnit
- **MSTest**: Microsoft's framework but less feature-rich
- **BenchmarkDotNet**: Excellent for micro-benchmarks but NBomber better for system-level performance testing

### 6. ROM Test Suite Integration

**Decision**: Blargg's test ROMs with custom test runner
**Rationale**: Blargg's test suite is the gold standard for NES emulator validation, covering CPU, PPU, and APU accuracy. Custom test runner will integrate with xUnit to provide automated validation of emulation accuracy as part of the CI/CD pipeline.

**Alternatives considered**:
- **Manual testing only**: Insufficient for continuous validation
- **Other test ROM suites**: Blargg's is most comprehensive and widely accepted
- **Custom test ROMs**: Would require significant development time

### 7. CLI Visualization and Output

**Decision**: Spectre.Console
**Rationale**: Spectre.Console provides rich, modern CLI output with support for progress bars, tables, colors, and interactive elements. Essential for providing clear diagnostic information, test results, and emulation status in command-line modes.

**Alternatives considered**:
- **Console.WriteLine only**: Too basic for complex diagnostic output
- **Colorful.Console**: Good but less feature-rich than Spectre.Console
- **ConsoleTables**: Limited to table output only
- **Custom formatting**: Significant development overhead

### 8. Hardware Reference Documentation

**Decision**: Comprehensive multi-source approach with authoritative references
**Rationale**: Cycle-accurate emulation requires cross-referencing multiple authoritative sources to achieve the highest accuracy. No single source contains all necessary details, and different sources excel in different areas.

**Primary References**:

#### **Community Documentation**
- **NESdev Wiki** (https://www.nesdev.org/wiki/Nesdev_Wiki) - Most comprehensive community resource
- **NESdev Forums** (https://forums.nesdev.org/) - Active community discussions and discoveries
- **6502.org** (http://www.6502.org/) - Authoritative 6502 processor documentation

#### **Official Nintendo Documentation**
- **Nintendo Famicom/NES Hardware Manual** - Official hardware specifications (when available)
- **Nintendo Developer Documentation** - Internal development guidelines and timing specifications
- **Patent Documents** - US Patents 4,799,635 (PPU) and related Nintendo hardware patents

#### **Academic and Research Papers**
- **"Reverse Engineering the Nintendo Entertainment System"** - Academic analysis of NES architecture
- **IEEE Papers on Video Game Console Architecture** - Peer-reviewed technical analysis
- **Computer Architecture Research** - Papers on 6502 timing and behavior analysis

#### **Proven Emulator Implementations**
- **Mesen Source Code** (https://github.com/SourMesen/Mesen2) - Highly accurate open-source emulator
- **FCEUX Source Code** (https://github.com/TASEmulators/fceux) - Well-documented emulator with extensive testing
- **Nestopia Source Code** - Reference implementation for accuracy
- **puNES Source Code** (https://github.com/punesemu/puNES) - Another accurate implementation

#### **Hardware Analysis and Reverse Engineering**
- **Visual6502** (http://visual6502.org/) - Transistor-level 6502 simulation and analysis
- **FPGA NES Implementations** - Hardware-accurate implementations like MiSTer NES core
- **Decapping Projects** - Physical chip analysis and die photography
- **Oscilloscope Analysis** - Real hardware timing measurements and signal analysis

#### **Test ROMs and Validation Suites**
- **Blargg's Test ROMs** - Comprehensive CPU, PPU, and APU test suites
- **Kevin Horton's Test ROMs** - Additional hardware validation tests
- **Shay Green's Test ROMs** - Audio and timing validation
- **Test ROM Database** (https://github.com/christopherpow/nes-test-roms) - Comprehensive collection

#### **Specialized Documentation**
- **Mapper Documentation** - Individual mapper specifications and implementations
- **Audio Analysis** - Detailed APU channel analysis and waveform documentation
- **PPU Timing Analysis** - Scanline-by-scanline timing documentation
- **Memory Mapping Documentation** - Complete address space analysis

**Implementation approach**:
- Cross-reference multiple sources for each implementation detail
- Prioritize official Nintendo documentation when available
- Use community wiki as primary reference with academic validation
- Validate against proven emulator implementations
- Test with comprehensive ROM test suites
- Document source references in code comments
- Contribute discoveries back to the community

## Technology Stack Summary

| Component | Technology | Version | Purpose |
|-----------|------------|---------|---------|
| **Runtime** | .NET 9.0 | 9.0.x | Primary runtime environment |
| **Language** | C# | 13.0 | Implementation language |
| **Graphics** | MonoGame | 3.8.x | Cross-platform 2D rendering |
| **Audio** | NAudio + OpenTK.Audio | Latest | Low-latency audio output |
| **UI Framework** | Avalonia UI | 11.x | Configuration interface |
| **CLI Output** | Spectre.Console | Latest | Rich command-line interface |
| **Real-time Communication** | SignalR | 9.0.x | MCP interface for AI agents |
| **HTTP Client** | System.Net.Http | Built-in | REST API operations |
| **Testing** | xUnit + FluentAssertions | Latest | Unit and integration testing |
| **Performance Testing** | NBomber | Latest | Performance benchmarking |
| **Configuration** | System.Text.Json | Built-in | JSON configuration files |
| **Logging** | Microsoft.Extensions.Logging | 9.0.x | Structured logging |
| **Hardware References** | Multi-source approach | Current | Comprehensive hardware documentation |

## Performance Considerations

### Memory Management
- Use `Span<T>` and `Memory<T>` for high-performance memory operations
- Implement object pooling for frequently allocated objects (audio buffers, graphics primitives)
- Minimize garbage collection pressure in emulation hot paths

### Threading Strategy
- Main emulation loop on dedicated thread for consistent timing
- Separate threads for audio, video, and input processing
- Lock-free data structures where possible for inter-thread communication

### Optimization Techniques
- Profile-guided optimization for hot code paths
- SIMD instructions for parallel operations where applicable
- Aggressive inlining for performance-critical methods

## Next Steps

All technical unknowns have been resolved. Ready to proceed to Phase 1: Design & Contracts.
