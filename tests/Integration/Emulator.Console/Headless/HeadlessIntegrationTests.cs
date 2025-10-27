using FluentAssertions;
using Xunit;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace EightBitten.Tests.Integration.Emulator.Console.Headless;

/// <summary>
/// Integration tests for headless emulation mode
/// Tests end-to-end ROM loading and execution without graphics
/// </summary>
public sealed class HeadlessIntegrationTests : IDisposable
{
    private readonly string _testROMsPath;
    private readonly string _headlessExecutablePath;

    public HeadlessIntegrationTests()
    {
        _testROMsPath = Path.Combine(Path.GetTempPath(), "8bitten-integration-test-roms");
        Directory.CreateDirectory(_testROMsPath);
        
        // Path to the headless console application
        _headlessExecutablePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "8Bitten.Console.Headless.exe"
        );
    }

    [Fact]
    public async Task HeadlessExecutionWithValidROMShouldExitSuccessfully()
    {
        // Arrange
        var romPath = CreateTestROM("valid-test.nes");

        // Act
        var result = await RunHeadlessEmulator(romPath, "--cycles", "1000");

        // Assert
        result.ExitCode.Should().Be(0, "Valid ROM should execute successfully");
        result.Output.Should().Contain("ROM loaded successfully");
        result.Output.Should().Contain("Emulation completed");
    }

    [Fact]
    public async Task HeadlessExecutionWithNonExistentROMShouldExitWithError()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testROMsPath, "non-existent.nes");

        // Act
        var result = await RunHeadlessEmulator(nonExistentPath);

        // Assert
        result.ExitCode.Should().Be(4, "File not found should return I/O error code");
        result.ErrorOutput.Should().Contain("File not found");
    }

    [Fact]
    public async Task HeadlessExecutionWithInvalidROMShouldExitWithError()
    {
        // Arrange
        var invalidRomPath = CreateInvalidROM("invalid.nes");

        // Act
        var result = await RunHeadlessEmulator(invalidRomPath);

        // Assert
        result.ExitCode.Should().Be(2, "Invalid ROM should return invalid ROM error code");
        result.ErrorOutput.Should().Contain("Invalid iNES header");
    }

    [Fact]
    public async Task HeadlessExecutionWithUnsupportedMapperShouldExitWithError()
    {
        // Arrange
        var unsupportedMapperRomPath = CreateUnsupportedMapperROM("unsupported.nes");

        // Act
        var result = await RunHeadlessEmulator(unsupportedMapperRomPath);

        // Assert
        result.ExitCode.Should().Be(3, "Unsupported mapper should return unsupported feature error code");
        result.ErrorOutput.Should().Contain("Unsupported mapper");
    }

    [Fact]
    public async Task HeadlessExecutionWithDiagnosticOutputShouldProduceDiagnostics()
    {
        // Arrange
        var romPath = CreateTestROM("diagnostic-test.nes");

        // Act
        var result = await RunHeadlessEmulator(romPath, "--diagnostic", "--cycles", "100");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("CPU State:");
        result.Output.Should().Contain("PPU State:");
        result.Output.Should().Contain("Cycle Count:");
    }

    [Fact]
    public async Task HeadlessExecutionWithJSONOutputShouldProduceValidJSON()
    {
        // Arrange
        var romPath = CreateTestROM("json-test.nes");

        // Act
        var result = await RunHeadlessEmulator(romPath, "--output-format", "json", "--cycles", "50");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("\"cpu\":");
        result.Output.Should().Contain("\"ppu\":");
        result.Output.Should().Contain("\"cycleCount\":");
    }

    [Fact]
    public async Task HeadlessExecutionWithCycleLimitShouldStopAtLimit()
    {
        // Arrange
        var romPath = CreateTestROM("cycle-limit-test.nes");
        var cycleLimit = 1000;

        // Act
        var result = await RunHeadlessEmulator(romPath, "--cycles", cycleLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain($"Executed {cycleLimit} cycles");
    }

    [Fact]
    public async Task HeadlessExecutionWithTimeLimitShouldStopAtTimeLimit()
    {
        // Arrange
        var romPath = CreateTestROM("time-limit-test.nes");

        // Act
        var result = await RunHeadlessEmulator(romPath, "--time", "1s");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Time limit reached");
    }

    [Fact]
    public async Task HeadlessExecutionWithInvalidArgumentsShouldShowHelp()
    {
        // Act
        var result = await RunHeadlessEmulator("--invalid-argument");

        // Assert
        result.ExitCode.Should().Be(1, "Invalid arguments should return general error");
        result.ErrorOutput.Should().Contain("Usage:");
    }

    [Fact]
    public async Task HeadlessExecutionWithHelpFlagShouldShowHelp()
    {
        // Act
        var result = await RunHeadlessEmulator("--help");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("8Bitten NES Emulator - Headless Mode");
        result.Output.Should().Contain("Usage:");
        result.Output.Should().Contain("--cycles");
        result.Output.Should().Contain("--diagnostic");
    }

    private string CreateTestROM(string filename)
    {
        var rom = new byte[16 + 16384 + 8192]; // Header + 16KB PRG + 8KB CHR
        
        // iNES header
        rom[0] = 0x4E; // 'N'
        rom[1] = 0x45; // 'E'
        rom[2] = 0x53; // 'S'
        rom[3] = 0x1A; // EOF
        rom[4] = 1;    // 1 * 16KB PRG ROM
        rom[5] = 1;    // 1 * 8KB CHR ROM
        rom[6] = 0x00; // Mapper 0 (NROM)
        rom[7] = 0x00; // No special features
        
        // Add reset vector pointing to start of PRG ROM
        rom[16 + 16384 - 4] = 0x00; // Reset vector low
        rom[16 + 16384 - 3] = 0x80; // Reset vector high (0x8000)
        
        var path = Path.Combine(_testROMsPath, filename);
        File.WriteAllBytes(path, rom);
        return path;
    }

    private string CreateInvalidROM(string filename)
    {
        var invalidRom = new byte[100];
        // Invalid magic number
        invalidRom[0] = 0x00;
        invalidRom[1] = 0x00;
        invalidRom[2] = 0x00;
        invalidRom[3] = 0x00;
        
        var path = Path.Combine(_testROMsPath, filename);
        File.WriteAllBytes(path, invalidRom);
        return path;
    }

    private string CreateUnsupportedMapperROM(string filename)
    {
        var rom = new byte[16 + 16384 + 8192];
        
        // Valid iNES header but unsupported mapper
        rom[0] = 0x4E; // 'N'
        rom[1] = 0x45; // 'E'
        rom[2] = 0x53; // 'S'
        rom[3] = 0x1A; // EOF
        rom[4] = 1;    // 1 * 16KB PRG ROM
        rom[5] = 1;    // 1 * 8KB CHR ROM
        rom[6] = 0xFF; // Unsupported mapper (255)
        rom[7] = 0x00;
        
        var path = Path.Combine(_testROMsPath, filename);
        File.WriteAllBytes(path, rom);
        return path;
    }

    private async Task<ProcessResult> RunHeadlessEmulator(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _headlessExecutablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = startInfo };
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

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing && Directory.Exists(_testROMsPath))
        {
            Directory.Delete(_testROMsPath, true);
        }
    }

    private sealed class ProcessResult
    {
        public int ExitCode { get; set; }
        public string Output { get; set; } = string.Empty;
        public string ErrorOutput { get; set; } = string.Empty;
    }
}
