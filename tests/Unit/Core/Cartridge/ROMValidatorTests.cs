using FluentAssertions;
using Xunit;
using System;
using System.IO;
using System.Text;
using EightBitten.Core.Cartridge;

namespace EightBitten.Tests.Unit.Core.Cartridge;

/// <summary>
/// Unit tests for ROM validation functionality
/// Tests cover iNES header validation, PRG/CHR ROM size checks, and mapper support verification
/// </summary>
public class ROMValidatorTests
{
    [Fact]
    public void ValidateROMWithValidINESHeaderShouldReturnSuccess()
    {
        // Arrange
        var validROM = CreateValidINESROM();

        // Act
        var result = ROMValidator.ValidateROM(validROM);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateROMWithInvalidMagicNumberShouldReturnError()
    {
        // Arrange
        var invalidROM = new byte[16];
        invalidROM[0] = 0x4E; // Should be 'N'
        invalidROM[1] = 0x45; // Should be 'E'
        invalidROM[2] = 0x53; // Should be 'S'
        invalidROM[3] = 0x00; // Should be 0x1A

        // Act
        var result = ROMValidator.ValidateROM(invalidROM);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid iNES header");
        result.ErrorCode.Should().Be(ROMValidationErrorCode.InvalidHeader);
    }

    [Fact]
    public void ValidateROMWithTooSmallFileShouldReturnError()
    {
        // Arrange
        var tooSmallROM = new byte[10]; // Less than 16 bytes for header

        // Act
        var result = ROMValidator.ValidateROM(tooSmallROM);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("ROM file too small");
        result.ErrorCode.Should().Be(ROMValidationErrorCode.FileTooSmall);
    }

    [Fact]
    public void ValidateROMWithUnsupportedMapperShouldReturnError()
    {
        // Arrange
        var romWithUnsupportedMapper = CreateValidINESROM();
        romWithUnsupportedMapper[6] = 0xFF; // Set unsupported mapper number

        // Act
        var result = ROMValidator.ValidateROM(romWithUnsupportedMapper);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unsupported mapper");
        result.ErrorCode.Should().Be(ROMValidationErrorCode.UnsupportedMapper);
    }

    [Theory]
    [InlineData(0)] // NROM
    [InlineData(1)] // MMC1
    [InlineData(2)] // UNROM
    [InlineData(3)] // CNROM
    [InlineData(4)] // MMC3
    public void ValidateROMWithSupportedMapperShouldReturnSuccess(byte mapperNumber)
    {
        // Arrange
        var rom = CreateValidINESROM();
        rom[6] = (byte)(mapperNumber << 4); // Set mapper number in lower nibble of byte 6

        // Act
        var result = ROMValidator.ValidateROM(rom);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateROMWithInvalidPRGROMSizeShouldReturnError()
    {
        // Arrange
        var rom = CreateValidINESROM();
        rom[4] = 0; // Set PRG ROM size to 0

        // Act
        var result = ROMValidator.ValidateROM(rom);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid PRG ROM size");
        result.ErrorCode.Should().Be(ROMValidationErrorCode.InvalidPRGSize);
    }

    [Fact]
    public void ValidateROMWithMismatchedFileSizeShouldReturnError()
    {
        // Arrange
        var rom = CreateValidINESROM();
        rom[4] = 2; // Set PRG ROM size to 2 * 16KB = 32KB
        rom[5] = 1; // Set CHR ROM size to 1 * 8KB = 8KB
        // Total expected size: 16 (header) + 32768 (PRG) + 8192 (CHR) = 40976 bytes
        // But our ROM is only 16 + 16384 + 8192 = 24592 bytes

        // Act
        var result = ROMValidator.ValidateROM(rom);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("File size mismatch");
        result.ErrorCode.Should().Be(ROMValidationErrorCode.FileSizeMismatch);
    }

    [Fact]
    public void ValidateROMWithValidNROMGameShouldReturnSuccess()
    {
        // Arrange
        var rom = CreateValidNROMGame();

        // Act
        var result = ROMValidator.ValidateROM(rom);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateROMWithTrainerPresentShouldReturnSuccess()
    {
        // Arrange
        var rom = CreateValidINESROM();
        rom[6] |= 0x04; // Set trainer bit
        var romWithTrainer = new byte[rom.Length + 512]; // Add 512 bytes for trainer
        Array.Copy(rom, 0, romWithTrainer, 0, 16); // Copy header
        // Skip 512 bytes for trainer
        Array.Copy(rom, 16, romWithTrainer, 528, rom.Length - 16); // Copy PRG/CHR data

        // Act
        var result = ROMValidator.ValidateROM(romWithTrainer);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    private static byte[] CreateValidINESROM()
    {
        var rom = new byte[16 + 16384 + 8192]; // Header + 16KB PRG + 8KB CHR
        
        // iNES header
        rom[0] = 0x4E; // 'N'
        rom[1] = 0x45; // 'E'
        rom[2] = 0x53; // 'S'
        rom[3] = 0x1A; // EOF
        rom[4] = 1;    // 1 * 16KB PRG ROM
        rom[5] = 1;    // 1 * 8KB CHR ROM
        rom[6] = 0x00; // Mapper 0 (NROM), horizontal mirroring
        rom[7] = 0x00; // No special features
        
        // Fill remaining header bytes with zeros
        for (int i = 8; i < 16; i++)
        {
            rom[i] = 0;
        }

        return rom;
    }

    private static byte[] CreateValidNROMGame()
    {
        var rom = CreateValidINESROM();
        
        // Add some dummy PRG ROM data (reset vector at end)
        var prgStart = 16;
        var prgEnd = prgStart + 16384;
        
        // Set reset vector to point to start of PRG ROM
        rom[prgEnd - 4] = 0x00; // Reset vector low byte
        rom[prgEnd - 3] = 0x80; // Reset vector high byte (0x8000)
        
        return rom;
    }
}
