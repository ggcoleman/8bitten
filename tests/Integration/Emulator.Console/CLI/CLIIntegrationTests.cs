using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace EightBitten.Tests.Integration.Emulator.Console.CLI;

/// <summary>
/// Integration tests for CLI gaming mode
/// Tests end-to-end CLI execution with graphics and audio
/// </summary>
public sealed class CLIIntegrationTests : IDisposable
{
    private readonly string _testROMsPath;
    private readonly string _cliExecutablePath;

    public CLIIntegrationTests()
    {
        _testROMsPath = Path.Combine(Path.GetTempPath(), "8bitten-cli-test-roms");
        Directory.CreateDirectory(_testROMsPath);
        
        _cliExecutablePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "8Bitten.Console.CLI.exe"
        );
        
        CreateTestROMs();
    }

    [Fact]
    public async Task CLIExecutionWithValidROMShouldLaunchGameWindow()
    {
        // Arrange
        var romPath = Path.Combine(_testROMsPath, "test-nrom.nes");
        var args = new[] { romPath, "--windowed", "--test-mode" };

        // Act
        var result = await RunCLIEmulator(args, timeoutSeconds: 10);

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Game window opened");
        result.Output.Should().Contain("Graphics initialized");
        result.Output.Should().Contain("Audio initialized");
    }

    [Fact]
    public async Task CLIExecutionWithNonExistentROMShouldExitWithError()
    {
        // Arrange
        var romPath = Path.Combine(_testROMsPath, "nonexistent.nes");
        var args = new[] { romPath };

        // Act
        var result = await RunCLIEmulator(args, timeoutSeconds: 5);

        // Assert
        result.ExitCode.Should().Be(4); // I/O error
        result.ErrorOutput.Should().Contain("File not found");
    }

    [Fact]
    public async Task CLIExecutionWithInvalidROMShouldExitWithError()
    {
        // Arrange
        var romPath = Path.Combine(_testROMsPath, "invalid.nes");
        var args = new[] { romPath };

        // Act
        var result = await RunCLIEmulator(args, timeoutSeconds: 5);

        // Assert
        result.ExitCode.Should().Be(2); // Invalid ROM
        result.ErrorOutput.Should().Contain("Invalid ROM format");
    }

    [Fact]
    public async Task CLIExecutionWithFullscreenModeShouldWork()
    {
        // Arrange
        var romPath = Path.Combine(_testROMsPath, "test-nrom.nes");
        var args = new[] { romPath, "--fullscreen", "--test-mode" };

        // Act
        var result = await RunCLIEmulator(args, timeoutSeconds: 10);

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Fullscreen mode enabled");
        result.Output.Should().Contain("Graphics initialized");
    }

    [Fact]
    public async Task CLIExecutionWithCustomScalingShouldWork()
    {
        // Arrange
        var romPath = Path.Combine(_testROMsPath, "test-nrom.nes");
        var args = new[] { romPath, "--scale", "3", "--test-mode" };

        // Act
        var result = await RunCLIEmulator(args, timeoutSeconds: 10);

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Render scale: 3");
        result.Output.Should().Contain("Graphics initialized");
    }

    [Fact]
    public async Task CLIExecutionWithAudioDisabledShouldWork()
    {
        // Arrange
        var romPath = Path.Combine(_testROMsPath, "test-nrom.nes");
        var args = new[] { romPath, "--no-audio", "--test-mode" };

        // Act
        var result = await RunCLIEmulator(args, timeoutSeconds: 10);

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Audio disabled");
        result.Output.Should().Contain("Graphics initialized");
    }

    [Fact]
    public async Task CLIExecutionWithVSyncEnabledShouldWork()
    {
        // Arrange
        var romPath = Path.Combine(_testROMsPath, "test-nrom.nes");
        var args = new[] { romPath, "--vsync", "--test-mode" };

        // Act
        var result = await RunCLIEmulator(args, timeoutSeconds: 10);

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("VSync enabled");
        result.Output.Should().Contain("Graphics initialized");
    }

    [Fact]
    public async Task CLIExecutionWithPerformanceMonitoringShouldWork()
    {
        // Arrange
        var romPath = Path.Combine(_testROMsPath, "test-nrom.nes");
        var args = new[] { romPath, "--performance", "--test-mode" };

        // Act
        var result = await RunCLIEmulator(args, timeoutSeconds: 10);

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Performance monitoring enabled");
        result.Output.Should().Contain("FPS:");
        result.Output.Should().Contain("Frame time:");
    }

    [Fact]
    public async Task CLIExecutionWithHelpFlagShouldShowUsage()
    {
        // Arrange
        var args = new[] { "--help" };

        // Act
        var result = await RunCLIEmulator(args, timeoutSeconds: 5);

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("8Bitten NES Emulator - CLI Gaming Mode");
        result.Output.Should().Contain("Usage:");
        result.Output.Should().Contain("--windowed");
        result.Output.Should().Contain("--fullscreen");
        result.Output.Should().Contain("--scale");
    }

    [Fact]
    public async Task CLIExecutionWithInvalidArgumentsShouldShowError()
    {
        // Arrange
        var args = new[] { "--invalid-argument" };

        // Act
        var result = await RunCLIEmulator(args, timeoutSeconds: 5);

        // Assert
        result.ExitCode.Should().Be(1); // General error
        result.ErrorOutput.Should().Contain("Invalid argument");
    }

    private async Task<ProcessResult> RunCLIEmulator(string[] args, int timeoutSeconds = 30)
    {
        using var process = new Process();
        process.StartInfo.FileName = _cliExecutablePath;
        process.StartInfo.Arguments = string.Join(" ", args);
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync().ConfigureAwait(false);

        var output = await outputTask.ConfigureAwait(false);
        var errorOutput = await errorTask.ConfigureAwait(false);

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            Output = output,
            ErrorOutput = errorOutput
        };
    }

    private void CreateTestROMs()
    {
        // Create valid NROM test ROM
        var nromPath = Path.Combine(_testROMsPath, "test-nrom.nes");
        CreateValidNROMData(nromPath);

        // Create invalid ROM file
        var invalidPath = Path.Combine(_testROMsPath, "invalid.nes");
        File.WriteAllText(invalidPath, "This is not a valid ROM file");
    }

    private static void CreateValidNROMData(string filePath)
    {
        var romData = new byte[16 + 16384 + 8192]; // Header + 16KB PRG + 8KB CHR
        
        // iNES header
        romData[0] = 0x4E; // 'N'
        romData[1] = 0x45; // 'E'
        romData[2] = 0x53; // 'S'
        romData[3] = 0x1A; // EOF
        romData[4] = 1;    // 1 x 16KB PRG ROM
        romData[5] = 1;    // 1 x 8KB CHR ROM
        romData[6] = 0;    // Mapper 0 (NROM), horizontal mirroring
        romData[7] = 0;    // No special features

        // Simple program: infinite loop
        romData[16 + 0x7FFC] = 0x00; // Reset vector low
        romData[16 + 0x7FFD] = 0x80; // Reset vector high (0x8000)
        romData[16 + 0x0000] = 0x4C; // JMP absolute
        romData[16 + 0x0001] = 0x00; // Low byte
        romData[16 + 0x0002] = 0x80; // High byte

        File.WriteAllBytes(filePath, romData);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testROMsPath))
        {
            Directory.Delete(_testROMsPath, true);
        }
    }

    private sealed class ProcessResult
    {
        public int ExitCode { get; init; }
        public string Output { get; init; } = string.Empty;
        public string ErrorOutput { get; init; } = string.Empty;
    }
}
