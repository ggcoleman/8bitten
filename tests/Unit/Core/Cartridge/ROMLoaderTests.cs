using FluentAssertions;
using Xunit;
using System;
using System.IO;
using System.Threading.Tasks;
using EightBitten.Core.Cartridge;

namespace EightBitten.Tests.Unit.Core.Cartridge;

/// <summary>
/// Unit tests for ROM loading functionality
/// Tests cover file loading, error handling, and cartridge creation
/// </summary>
public sealed class ROMLoaderTests : IDisposable
{
    private readonly string _testROMsPath;

    public ROMLoaderTests()
    {
        _testROMsPath = Path.Combine(Path.GetTempPath(), "8bitten-test-roms");
        Directory.CreateDirectory(_testROMsPath);
    }

    [Fact]
    public async Task LoadROMAsyncWithValidFileShouldReturnCartridge()
    {
        // Arrange
        var romPath = CreateTestROMFile("valid-test.nes", CreateValidNROMData());

        // Act
        var result = await ROMLoader.LoadROMAsync(romPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Cartridge.Should().NotBeNull();
        result.ErrorMessage.Should().BeNull();
        result.ErrorCode.Should().Be(ROMLoadErrorCode.None);
    }

    [Fact]
    public async Task LoadROMAsyncWithNonExistentFileShouldReturnError()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testROMsPath, "non-existent.nes");

        // Act
        var result = await ROMLoader.LoadROMAsync(nonExistentPath);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Cartridge.Should().BeNull();
        result.ErrorMessage.Should().Contain("File not found");
        result.ErrorCode.Should().Be(ROMLoadErrorCode.FileNotFound);
    }

    [Fact]
    public async Task LoadROMAsyncWithInvalidExtensionShouldReturnError()
    {
        // Arrange
        var invalidPath = CreateTestROMFile("invalid.txt", CreateValidNROMData());

        // Act
        var result = await ROMLoader.LoadROMAsync(invalidPath);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Cartridge.Should().BeNull();
        result.ErrorMessage.Should().Contain("Invalid file extension");
        result.ErrorCode.Should().Be(ROMLoadErrorCode.InvalidExtension);
    }

    [Fact]
    public async Task LoadROMAsyncWithCorruptedHeaderShouldReturnError()
    {
        // Arrange
        var corruptedData = new byte[100];
        corruptedData[0] = 0x00; // Invalid magic number
        var romPath = CreateTestROMFile("corrupted.nes", corruptedData);

        // Act
        var result = await ROMLoader.LoadROMAsync(romPath);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Cartridge.Should().BeNull();
        result.ErrorMessage.Should().Contain("Invalid iNES header");
        result.ErrorCode.Should().Be(ROMLoadErrorCode.InvalidHeader);
    }

    [Fact]
    public async Task LoadROMAsyncWithUnsupportedMapperShouldReturnError()
    {
        // Arrange
        var unsupportedMapperData = CreateValidNROMData();
        unsupportedMapperData[6] = 0xFF; // Set unsupported mapper
        var romPath = CreateTestROMFile("unsupported-mapper.nes", unsupportedMapperData);

        // Act
        var result = await ROMLoader.LoadROMAsync(romPath);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Cartridge.Should().BeNull();
        result.ErrorMessage.Should().Contain("Unsupported mapper");
        result.ErrorCode.Should().Be(ROMLoadErrorCode.UnsupportedMapper);
    }

    [Fact]
    public async Task LoadROMAsyncWithIOErrorShouldReturnError()
    {
        // Arrange
        var romPath = CreateTestROMFile("locked.nes", CreateValidNROMData());
        
        // Lock the file to simulate I/O error
        using var fileStream = new FileStream(romPath, FileMode.Open, FileAccess.Read, FileShare.None);

        // Act
        var result = await ROMLoader.LoadROMAsync(romPath);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Cartridge.Should().BeNull();
        result.ErrorMessage.Should().Contain("I/O error");
        result.ErrorCode.Should().Be(ROMLoadErrorCode.IOError);
    }

    [Theory]
    [InlineData(0)] // NROM
    [InlineData(1)] // MMC1
    [InlineData(2)] // UNROM
    [InlineData(3)] // CNROM
    [InlineData(4)] // MMC3
    public async Task LoadROMAsyncWithSupportedMapperShouldReturnCartridge(byte mapperNumber)
    {
        // Arrange
        var romData = CreateValidNROMData();
        romData[6] = (byte)(mapperNumber << 4); // Set mapper number
        var romPath = CreateTestROMFile($"mapper-{mapperNumber}.nes", romData);

        // Act
        var result = await ROMLoader.LoadROMAsync(romPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Cartridge.Should().NotBeNull();
        result.Cartridge!.Header.MapperNumber.Should().Be(mapperNumber);
    }

    [Fact]
    public async Task LoadROMAsyncWithTrainerShouldLoadSuccessfully()
    {
        // Arrange
        var romData = CreateValidNROMData();
        romData[6] |= 0x04; // Set trainer bit
        var romWithTrainer = new byte[romData.Length + 512]; // Add trainer
        Array.Copy(romData, 0, romWithTrainer, 0, 16); // Copy header
        // Skip 512 bytes for trainer
        Array.Copy(romData, 16, romWithTrainer, 528, romData.Length - 16);
        
        var romPath = CreateTestROMFile("with-trainer.nes", romWithTrainer);

        // Act
        var result = await ROMLoader.LoadROMAsync(romPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Cartridge.Should().NotBeNull();
        result.Cartridge!.Header.HasTrainer.Should().BeTrue();
    }

    [Fact]
    public async Task LoadROMAsyncWithLargeROMShouldLoadSuccessfully()
    {
        // Arrange
        var largeRomData = CreateLargeROMData(32, 16); // 32 PRG banks, 16 CHR banks
        var romPath = CreateTestROMFile("large.nes", largeRomData);

        // Act
        var result = await ROMLoader.LoadROMAsync(romPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Cartridge.Should().NotBeNull();
        result.Cartridge!.Header.PRGROMSize.Should().Be(32 * 16384);
        result.Cartridge!.Header.CHRROMSize.Should().Be(16 * 8192);
    }

    private string CreateTestROMFile(string filename, byte[] data)
    {
        var path = Path.Combine(_testROMsPath, filename);
        File.WriteAllBytes(path, data);
        return path;
    }

    private static byte[] CreateValidNROMData()
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
        
        return rom;
    }

    private static byte[] CreateLargeROMData(byte prgBanks, byte chrBanks)
    {
        var rom = new byte[16 + (prgBanks * 16384) + (chrBanks * 8192)];
        
        // iNES header
        rom[0] = 0x4E; // 'N'
        rom[1] = 0x45; // 'E'
        rom[2] = 0x53; // 'S'
        rom[3] = 0x1A; // EOF
        rom[4] = prgBanks;
        rom[5] = chrBanks;
        rom[6] = 0x00; // Mapper 0 (NROM)
        rom[7] = 0x00; // No special features
        
        return rom;
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
}
