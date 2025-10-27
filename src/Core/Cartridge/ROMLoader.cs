using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EightBitten.Core.Cartridge;

/// <summary>
/// Loads ROM files and creates cartridge instances
/// Handles file I/O, validation, and error reporting with specific exit codes
/// </summary>
public static class ROMLoader
{

    /// <summary>
    /// Loads a ROM file asynchronously and creates a cartridge instance
    /// </summary>
    /// <param name="filePath">Path to the ROM file</param>
    /// <returns>Load result with cartridge or error information</returns>
    public static async Task<ROMLoadResult> LoadROMAsync(string filePath)
    {
        try
        {
            // Validate file path
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return ROMLoadResult.Error(ROMLoadErrorCode.InvalidPath, "File path is null or empty");
            }

            // Check file existence
            if (!File.Exists(filePath))
            {
                return ROMLoadResult.Error(ROMLoadErrorCode.FileNotFound, $"File not found: {filePath}");
            }

            // Validate file extension
            var extension = Path.GetExtension(filePath).ToUpperInvariant();
            if (extension != ".NES")
            {
                return ROMLoadResult.Error(ROMLoadErrorCode.InvalidExtension,
                    $"Invalid file extension '{extension}' - only .nes files are supported");
            }

            // Read ROM data
            byte[] romData;
            try
            {
                romData = await File.ReadAllBytesAsync(filePath).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                return ROMLoadResult.Error(ROMLoadErrorCode.IOError, $"I/O error reading file: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                return ROMLoadResult.Error(ROMLoadErrorCode.IOError, $"Access denied: {ex.Message}");
            }

            // Validate ROM data
            var validationResult = ROMValidator.ValidateROM(romData);
            if (!validationResult.IsValid)
            {
                return ROMLoadResult.Error(MapValidationErrorToLoadError(validationResult.ErrorCode), 
                    validationResult.ErrorMessage!);
            }

            // Create cartridge
            var cartridge = CreateCartridge(romData);
            return ROMLoadResult.Success(cartridge);
        }
        catch (InvalidDataException ex)
        {
            return ROMLoadResult.Error(ROMLoadErrorCode.InvalidHeader, ex.Message);
        }
        catch (NotSupportedException ex)
        {
            return ROMLoadResult.Error(ROMLoadErrorCode.UnsupportedMapper, ex.Message);
        }
        catch (OutOfMemoryException ex)
        {
            return ROMLoadResult.Error(ROMLoadErrorCode.UnknownError, $"Out of memory: {ex.Message}");
        }
    }

    private static GameCartridge CreateCartridge(byte[] romData)
    {
        // Create a GameCartridge with a null logger (for now)
        // In a full implementation, this would be injected via DI
        var logger = NullLogger.Instance;
        var cartridge = new GameCartridge(logger);

        if (!cartridge.LoadFromData(romData))
        {
            throw new InvalidDataException("Failed to load ROM data into cartridge");
        }

        return cartridge;
    }

    private static ROMLoadErrorCode MapValidationErrorToLoadError(ROMValidationErrorCode validationError)
    {
        return validationError switch
        {
            ROMValidationErrorCode.InvalidHeader => ROMLoadErrorCode.InvalidHeader,
            ROMValidationErrorCode.UnsupportedMapper => ROMLoadErrorCode.UnsupportedMapper,
            ROMValidationErrorCode.FileTooSmall => ROMLoadErrorCode.InvalidHeader,
            ROMValidationErrorCode.InvalidPRGSize => ROMLoadErrorCode.InvalidHeader,
            ROMValidationErrorCode.FileSizeMismatch => ROMLoadErrorCode.InvalidHeader,
            _ => ROMLoadErrorCode.UnknownError
        };
    }
}

/// <summary>
/// Result of ROM loading operation
/// </summary>
public class ROMLoadResult
{
    public bool IsSuccess { get; private set; }
    public ICartridge? Cartridge { get; private set; }
    public string? ErrorMessage { get; private set; }
    public ROMLoadErrorCode ErrorCode { get; private set; }

    private ROMLoadResult(bool isSuccess, ICartridge? cartridge, ROMLoadErrorCode errorCode, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Cartridge = cartridge;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static ROMLoadResult Success(ICartridge cartridge)
    {
        return new ROMLoadResult(true, cartridge, ROMLoadErrorCode.None, null);
    }

    public static ROMLoadResult Error(ROMLoadErrorCode errorCode, string errorMessage)
    {
        return new ROMLoadResult(false, null, errorCode, errorMessage);
    }
}

/// <summary>
/// Error codes for ROM loading failures
/// Maps to specific exit codes: 2=invalid ROM, 3=unsupported feature, 4=I/O error
/// </summary>
public enum ROMLoadErrorCode
{
    None = 0,
    UnknownError = 1,
    InvalidPath = 10,
    InvalidExtension = 11,
    InvalidHeader = 12,
    UnsupportedMapper = 3,
    FileNotFound = 20,
    IOError = 21
}


