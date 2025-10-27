using System;
using System.Collections.Generic;
using System.Linq;

namespace EightBitten.Core.Cartridge;

/// <summary>
/// Validates ROM files for proper iNES format and supported features
/// Performs header validation, size checks, and mapper support verification
/// </summary>
public static class ROMValidator
{
    private static readonly byte[] INESMagic = { 0x4E, 0x45, 0x53, 0x1A }; // "NES" + EOF
    
    private static readonly HashSet<byte> SupportedMappers = new()
    {
        0, // NROM
        1, // MMC1
        2, // UNROM
        3, // CNROM
        4  // MMC3
    };

    /// <summary>
    /// Validates a ROM file for proper iNES format and compatibility
    /// </summary>
    /// <param name="romData">The ROM file data to validate</param>
    /// <returns>Validation result with success status and error details</returns>
    public static ROMValidationResult ValidateROM(byte[] romData)
    {
        if (romData == null)
        {
            return ROMValidationResult.Error(ROMValidationErrorCode.NullData, "ROM data is null");
        }

        // Check minimum file size for iNES header
        if (romData.Length < 16)
        {
            return ROMValidationResult.Error(ROMValidationErrorCode.FileTooSmall, 
                "ROM file too small - minimum 16 bytes required for iNES header");
        }

        // Validate iNES magic number
        if (!ValidateINESHeader(romData))
        {
            return ROMValidationResult.Error(ROMValidationErrorCode.InvalidHeader, 
                "Invalid iNES header - magic number mismatch");
        }

        // Extract header information
        var prgRomSize = romData[4]; // Number of 16KB PRG ROM banks
        var chrRomSize = romData[5]; // Number of 8KB CHR ROM banks
        var flags6 = romData[6];
        var flags7 = romData[7];

        // Validate PRG ROM size
        if (prgRomSize == 0)
        {
            return ROMValidationResult.Error(ROMValidationErrorCode.InvalidPRGSize, 
                "Invalid PRG ROM size - must be at least 1 bank (16KB)");
        }

        // Extract mapper number
        var mapperNumber = (byte)(((flags7 & 0xF0) | (flags6 >> 4)) & 0xFF);

        // Check mapper support
        if (!SupportedMappers.Contains(mapperNumber))
        {
            return ROMValidationResult.Error(ROMValidationErrorCode.UnsupportedMapper, 
                $"Unsupported mapper {mapperNumber} - supported mappers: {string.Join(", ", SupportedMappers)}");
        }

        // Calculate expected file size
        var hasTrainer = (flags6 & 0x04) != 0;
        var expectedSize = CalculateExpectedFileSize(prgRomSize, chrRomSize, hasTrainer);

        // Validate file size
        if (romData.Length != expectedSize)
        {
            return ROMValidationResult.Error(ROMValidationErrorCode.FileSizeMismatch, 
                $"File size mismatch - expected {expectedSize} bytes, got {romData.Length} bytes");
        }

        return ROMValidationResult.Success();
    }

    private static bool ValidateINESHeader(byte[] romData)
    {
        for (int i = 0; i < INESMagic.Length; i++)
        {
            if (romData[i] != INESMagic[i])
            {
                return false;
            }
        }
        return true;
    }

    private static int CalculateExpectedFileSize(byte prgRomSize, byte chrRomSize, bool hasTrainer)
    {
        const int HeaderSize = 16;
        const int TrainerSize = 512;
        const int PrgBankSize = 16384; // 16KB
        const int ChrBankSize = 8192;  // 8KB

        var size = HeaderSize;
        
        if (hasTrainer)
        {
            size += TrainerSize;
        }
        
        size += prgRomSize * PrgBankSize;
        size += chrRomSize * ChrBankSize;

        return size;
    }
}

/// <summary>
/// Result of ROM validation operation
/// </summary>
public class ROMValidationResult
{
    public bool IsValid { get; private set; }
    public string? ErrorMessage { get; private set; }
    public ROMValidationErrorCode ErrorCode { get; private set; }

    private ROMValidationResult(bool isValid, ROMValidationErrorCode errorCode, string? errorMessage)
    {
        IsValid = isValid;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static ROMValidationResult Success()
    {
        return new ROMValidationResult(true, ROMValidationErrorCode.None, null);
    }

    public static ROMValidationResult Error(ROMValidationErrorCode errorCode, string errorMessage)
    {
        return new ROMValidationResult(false, errorCode, errorMessage);
    }
}

/// <summary>
/// Error codes for ROM validation failures
/// </summary>
public enum ROMValidationErrorCode
{
    None = 0,
    NullData = 1,
    FileTooSmall = 2,
    InvalidHeader = 3,
    InvalidPRGSize = 4,
    UnsupportedMapper = 5,
    FileSizeMismatch = 6
}
