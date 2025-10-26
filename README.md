# 8Bitten - The Definitive Cycle-Accurate NES Emulator

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13.0-green.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey.svg)](https://github.com/ggcoleman/8bitten)

> **The gold standard NES emulator designed for academics, AI/ML researchers, speedrunners, and competitive gamers**

8Bitten is a research-grade, cycle-accurate Nintendo Entertainment System (NES) emulator built with uncompromising accuracy and comprehensive analysis capabilities. Designed to be the go-to emulator for scientific research, competitive gaming optimization, and AI training while serving as the authoritative reference implementation for the emulation community.

## 🎯 **Project Vision**

**8Bitten aims to be THE definitive NES emulator that serves as the universal standard for:**

- **🔬 Academic Researchers** conducting AI and machine learning studies with reproducible, deterministic results
- **🏃‍♂️ Speedrunners** seeking frame-perfect optimization analysis and competitive gaming verification
- **🤖 AI/ML Engineers** training models with consistent, reliable emulation behavior and comprehensive data
- **🎮 Retro Gaming Enthusiasts** wanting the most authentic and accurate NES experience possible
- **📚 Emulation Community** seeking the authoritative reference implementation for hardware preservation

## ✨ **What Makes 8Bitten Different**

### 🔬 **Research-Grade Precision**
- **Cycle-accurate timing** with sub-frame precision measurement
- **100% deterministic replay** for reproducible research results
- **Comprehensive metrics collection** without performance impact
- **Multi-format data export** (CSV, JSON, HDF5) for scientific analysis
- **Statistical analysis tools** with confidence intervals and significance testing

### 🏆 **Competitive Gaming Excellence**
- **Frame-perfect timing analysis** for speedrunning optimization
- **Input efficiency analysis** with timing windows and recommendations
- **Real-time performance overlays** with customizable metrics display
- **Run comparison tools** for analyzing improvements and strategies
- **Hardware-accurate timing validation** for official verification

### 🤖 **AI/ML Research Platform**
- **Model Context Protocol (MCP) interface** for real-time AI agent communication
- **Complete state capture** for training data with full emulator state
- **Programmatic control** for automated gameplay and training sessions
- **Session recording** with perfect fidelity for training validation
- **Performance profiling** with optimization opportunity identification

### 🎮 **Authentic Gaming Experience**
- **Multiple execution modes**: Headless, CLI, and GUI with rich configuration
- **Cross-platform support**: Windows, macOS, and Linux
- **Comprehensive mapper support** with educational explanations
- **Save state management** with instant save/load capabilities
- **Rich CLI output** with Spectre.Console for diagnostic visualization

## 🏗️ **Architecture Highlights**

### **Component-Based Design**
```
Core Emulation Engine
├── CPU (6502) - Cycle-accurate processor emulation
├── PPU - Picture Processing Unit with authentic rendering
├── APU - Audio Processing Unit with original sound generation
├── Memory - Complete address space and memory mapping
└── Cartridge - ROM loading and mapper implementations

Research Infrastructure
├── Metrics - Real-time performance data collection
├── Recording - Input recording and deterministic replay
├── Analysis - Statistical analysis and optimization tools
└── Export - Multi-format data export for research

User Interfaces
├── Headless - No graphics for testing and automation
├── CLI - Command-line gaming with rich diagnostics
├── GUI - Avalonia UI for configuration and management
└── MCP - AI agent communication and control
```

### **Multi-Source Hardware Validation**
8Bitten references multiple authoritative sources for maximum accuracy:
- **[NESdev Wiki](https://www.nesdev.org/wiki/Nesdev_Wiki)** - Community documentation
- **[Official Nintendo Documentation](https://patents.google.com/patent/US4799635A)** - Hardware patents and specifications
- **[Academic Research](http://visual6502.org/)** - Peer-reviewed analysis and transistor-level simulation
- **[Proven Emulators](https://github.com/SourMesen/Mesen2)** - Reference implementations for validation
- **[Hardware Analysis](https://github.com/christopherpow/nes-test-roms)** - Test ROMs and validation suites

## 🚀 **Getting Started**

### **Prerequisites**
- .NET 9.0 SDK or later
- Visual Studio 2022 or VS Code with C# extension
- Git for version control

### **Quick Start**
```bash
# Clone the repository
git clone https://github.com/ggcoleman/8bitten.git
cd 8bitten

# Build the project
dotnet build

# Run headless mode (testing)
dotnet run --project src/Emulator.Console/Headless -- --rom game.nes

# Run CLI gaming mode
dotnet run --project src/Emulator.Console/CLI -- --rom game.nes

# Run GUI mode
dotnet run --project src/Emulator.Console/GUI
```

### **Research Usage**
```bash
# Start metrics collection session
dotnet run --project src/Emulator.Console/CLI -- --rom game.nes --research-mode --metrics-export data.csv

# Record deterministic session
dotnet run --project src/Emulator.Console/CLI -- --rom game.nes --record session.8br

# Replay recorded session
dotnet run --project src/Emulator.Console/CLI -- --replay session.8br --verify-determinism

# Export research data
dotnet run --project src/Emulator.Console/CLI -- --export-session session.8br --format hdf5 --output research_data.h5
```

### **Speedrunning Analysis**
```bash
# Real-time analysis with overlays
dotnet run --project src/Emulator.Console/CLI -- --rom game.nes --speedrun-mode --overlays timing,input,optimization

# Analyze completed run
dotnet run --project src/Emulator.Console/CLI -- --analyze-run session.8br --generate-report optimization_report.json

# Compare multiple runs
dotnet run --project src/Emulator.Console/CLI -- --compare-runs run1.8br run2.8br run3.8br --output comparison.csv
```

## 🎯 **Use Cases & Applications**

### **🔬 Academic Research**
- **Game AI Research**: Train and evaluate AI agents with deterministic, reproducible environments
- **Human-Computer Interaction**: Study player behavior with comprehensive input and timing analysis
- **Retro Gaming Studies**: Analyze historical games with modern research methodologies
- **Machine Learning**: Generate training datasets with perfect ground truth and state information

### **🏃‍♂️ Competitive Speedrunning**
- **Route Optimization**: Identify frame-perfect strategies and timing windows
- **Run Verification**: Validate timing accuracy against console hardware behavior
- **Performance Analysis**: Compare runs with statistical significance testing
- **Training Tools**: Practice with real-time feedback and optimization suggestions

### **🤖 AI/ML Development**
- **Reinforcement Learning**: Train agents with consistent, deterministic environments
- **Imitation Learning**: Record human gameplay for behavioral cloning
- **Game State Analysis**: Extract complete game state for model training
- **Automated Testing**: Validate AI performance across multiple game scenarios

### **🎮 Gaming & Preservation**
- **Authentic Experience**: Play games exactly as they appeared on original hardware
- **Hardware Preservation**: Document and preserve NES hardware behavior for future generations
- **Educational Tool**: Learn about computer architecture and emulation techniques
- **Development Reference**: Use as a reference for other emulation projects

## 📊 **Technical Specifications**

### **Accuracy Standards**
- **CPU Emulation**: Cycle-accurate 6502 processor with all documented and undocumented instructions
- **PPU Emulation**: Scanline-accurate rendering with authentic timing and edge cases
- **APU Emulation**: Sample-accurate audio generation with original waveforms and mixing
- **Memory System**: Complete address space emulation with accurate memory mapping
- **Timing Precision**: Sub-frame accuracy with deterministic execution

### **Performance Targets**
- **Real-time Emulation**: Consistent 60 FPS on mid-range hardware
- **Input Latency**: <16.67ms (one frame) for authentic responsiveness
- **Memory Usage**: <100MB for efficient resource utilization
- **Save States**: <100ms save/load operations for seamless gameplay
- **Metrics Collection**: Zero performance impact during data collection

### **Supported Features**
- **Mappers**: NROM, MMC1, MMC3, UNROM (with more planned)
- **Controllers**: Standard NES gamepad with customizable key mapping
- **Audio**: Authentic NES audio with configurable sample rates and drivers
- **Video**: Original 256x240 resolution with scaling and filter options
- **Save States**: Instant save/load with unlimited slots

## 🛠️ **Development Status**

### **Current Phase**: Implementation Planning Complete ✅
- ✅ Comprehensive specification with 55 functional requirements
- ✅ Research-grade architecture design with constitutional compliance
- ✅ 113 implementation tasks across 8 user stories
- ✅ Multi-source hardware reference validation strategy
- ✅ Complete interface contracts and data models

### **Roadmap**
- **Phase 1**: Project Setup & Foundational Components
- **Phase 2**: Headless ROM Execution (MVP)
- **Phase 3**: Command-Line Gaming with Graphics/Audio
- **Phase 4**: GUI Configuration Interface
- **Phase 5**: AI/ML Research Platform
- **Phase 6**: Academic Research Tools
- **Phase 7**: Speedrunning Analysis Features
- **Phase 8**: Hardware Accuracy Validation

### **Estimated Timeline**: 6-12 months for complete implementation

## 🤝 **Contributing**

We welcome contributions from the emulation community, researchers, and developers! 8Bitten is designed to be the community standard for NES emulation.

### **How to Contribute**
1. **Fork the repository** and create a feature branch
2. **Follow the constitutional principles** outlined in `.specify/memory/constitution.md`
3. **Write tests first** using our TDD approach
4. **Maintain research-grade quality** with comprehensive documentation
5. **Submit pull requests** with detailed descriptions and test coverage

### **Areas for Contribution**
- **Core Emulation**: CPU, PPU, APU implementation and optimization
- **Mapper Support**: Additional cartridge mapper implementations
- **Research Tools**: Analysis algorithms and statistical methods
- **Documentation**: Technical documentation and educational content
- **Testing**: Hardware validation and accuracy testing
- **Platform Support**: Platform-specific optimizations and features

## 📚 **Documentation**

- **[Architecture Overview](docs/Architecture/Overview.md)** - System design and component relationships
- **[API Documentation](docs/API/)** - Complete interface documentation
- **[Research Guide](docs/Research/)** - Academic research capabilities and data formats
- **[Speedrunning Guide](docs/Speedrunning/)** - Competitive gaming analysis tools
- **[Developer Guide](docs/Development/)** - Contributing and development setup
- **[Hardware Reference](docs/Hardware/)** - NES hardware documentation and sources

## 📄 **License**

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 **Acknowledgments**

8Bitten builds upon decades of emulation community research and development:

- **[NESdev Community](https://forums.nesdev.org/)** - Comprehensive hardware documentation and research
- **[Mesen](https://github.com/SourMesen/Mesen2)** - Reference implementation for accuracy validation
- **[FCEUX](https://github.com/TASEmulators/fceux)** - Extensive testing frameworks and tools
- **[Blargg](https://github.com/christopherpow/nes-test-roms)** - Comprehensive test ROM suites
- **[Visual6502](http://visual6502.org/)** - Transistor-level hardware analysis
- **Nintendo** - Creating the original hardware that inspired generations

## 🔗 **Links**

- **[Project Repository](https://github.com/ggcoleman/8bitten)**
- **[Issue Tracker](https://github.com/ggcoleman/8bitten/issues)**
- **[Discussions](https://github.com/ggcoleman/8bitten/discussions)**
- **[Wiki](https://github.com/ggcoleman/8bitten/wiki)**
- **[Releases](https://github.com/ggcoleman/8bitten/releases)**

---