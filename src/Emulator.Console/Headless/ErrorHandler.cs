using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace EightBitten.Console.Headless;

/// <summary>
/// Graceful error handling with specific exit codes
/// Exit codes: 0=success, 1=general error, 2=invalid ROM, 3=unsupported feature, 4=I/O error
/// </summary>
internal static class ErrorHandler
{
    /// <summary>
    /// Exit codes for different error conditions
    /// </summary>
    internal static class ExitCodes
    {
        public const int Success = 0;
        public const int GeneralError = 1;
        public const int InvalidROM = 2;
        public const int UnsupportedFeature = 3;
        public const int IOError = 4;
    }

    /// <summary>
    /// Handle application errors and return appropriate exit code
    /// </summary>
    /// <param name="exception">Exception to handle</param>
    /// <param name="logger">Logger for error reporting</param>
    /// <returns>Appropriate exit code</returns>
    public static int HandleError(Exception exception, ILogger? logger = null)
    {
        var errorInfo = ClassifyError(exception);
        
        // Log the error if logger is available
        if (logger != null)
        {
            LogError(logger, errorInfo, exception);
        }
        else
        {
            // Fallback to console output
            System.Console.WriteLine($"Error: {errorInfo.UserMessage}");
            if (errorInfo.ShowDetails)
            {
                System.Console.WriteLine($"Details: {exception.Message}");
            }
        }

        return errorInfo.ExitCode;
    }

    /// <summary>
    /// Handle application errors with custom message and return appropriate exit code
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="exitCode">Exit code</param>
    /// <param name="logger">Logger for error reporting</param>
    /// <returns>Specified exit code</returns>
    public static int HandleError(string message, int exitCode, ILogger? logger = null)
    {
        if (logger != null)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            logger.LogError("Application error (Exit Code {ExitCode}): {Message}", exitCode, message);
            #pragma warning restore CA1848
        }
        else
        {
            System.Console.WriteLine($"Error: {message}");
        }

        return exitCode;
    }

    /// <summary>
    /// Handle successful completion
    /// </summary>
    /// <param name="message">Success message</param>
    /// <param name="logger">Logger for success reporting</param>
    /// <returns>Success exit code (0)</returns>
    public static int HandleSuccess(string message, ILogger? logger = null)
    {
        if (logger != null)
        {
            #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
            logger.LogInformation("Application completed successfully: {Message}", message);
            #pragma warning restore CA1848
        }
        else
        {
            System.Console.WriteLine($"Success: {message}");
        }

        return ExitCodes.Success;
    }

    /// <summary>
    /// Classify exception and determine appropriate error response
    /// </summary>
    /// <param name="exception">Exception to classify</param>
    /// <returns>Error classification information</returns>
    private static ErrorClassification ClassifyError(Exception exception)
    {
        return exception switch
        {
            // I/O related errors
            System.IO.FileNotFoundException => new ErrorClassification(
                ExitCodes.IOError,
                "ROM file not found",
                "Verify the ROM file path is correct and the file exists",
                true
            ),
            System.IO.DirectoryNotFoundException => new ErrorClassification(
                ExitCodes.IOError,
                "Directory not found",
                "Verify the directory path is correct",
                true
            ),
            UnauthorizedAccessException => new ErrorClassification(
                ExitCodes.IOError,
                "Access denied",
                "Check file permissions and ensure the file is not locked by another process",
                true
            ),
            System.IO.IOException => new ErrorClassification(
                ExitCodes.IOError,
                "I/O error occurred",
                "Check disk space, file permissions, and hardware connectivity",
                true
            ),

            // ROM validation errors
            ArgumentException argEx when argEx.Message.Contains("ROM", StringComparison.OrdinalIgnoreCase) => new ErrorClassification(
                ExitCodes.InvalidROM,
                "Invalid ROM file",
                "Ensure the ROM file is a valid .nes file with proper iNES header",
                true
            ),
            InvalidDataException => new ErrorClassification(
                ExitCodes.InvalidROM,
                "Corrupted ROM data",
                "The ROM file appears to be corrupted or in an unsupported format",
                true
            ),

            // Unsupported features
            NotSupportedException => new ErrorClassification(
                ExitCodes.UnsupportedFeature,
                "Unsupported feature",
                "This ROM uses features not yet implemented in 8Bitten",
                true
            ),
            NotImplementedException => new ErrorClassification(
                ExitCodes.UnsupportedFeature,
                "Feature not implemented",
                "This functionality is planned but not yet available",
                false
            ),

            // Configuration errors
            InvalidOperationException opEx when opEx.Message.Contains("configuration", StringComparison.OrdinalIgnoreCase) => new ErrorClassification(
                ExitCodes.GeneralError,
                "Configuration error",
                "Check application settings and configuration files",
                true
            ),

            // Argument errors
            ArgumentNullException => new ErrorClassification(
                ExitCodes.GeneralError,
                "Invalid arguments",
                "Required argument is missing. Use --help for usage information",
                false
            ),
            ArgumentException => new ErrorClassification(
                ExitCodes.GeneralError,
                "Invalid arguments",
                "One or more arguments are invalid. Use --help for usage information",
                true
            ),

            // Cancellation (user requested)
            OperationCanceledException => new ErrorClassification(
                ExitCodes.Success,
                "Operation cancelled by user",
                "Emulation was stopped by user request (Ctrl+C)",
                false
            ),

            // Generic fallback
            _ => new ErrorClassification(
                ExitCodes.GeneralError,
                "Unexpected error occurred",
                "An unexpected error occurred during execution",
                true
            )
        };
    }

    /// <summary>
    /// Log error information using structured logging
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="errorInfo">Error classification information</param>
    /// <param name="exception">Original exception</param>
    private static void LogError(ILogger logger, ErrorClassification errorInfo, Exception exception)
    {
        var logLevel = errorInfo.ExitCode switch
        {
            ExitCodes.Success => LogLevel.Information,
            ExitCodes.GeneralError => LogLevel.Error,
            ExitCodes.InvalidROM => LogLevel.Warning,
            ExitCodes.UnsupportedFeature => LogLevel.Warning,
            ExitCodes.IOError => LogLevel.Error,
            _ => LogLevel.Error
        };

        #pragma warning disable CA1848 // Use LoggerMessage delegates for performance
        logger.Log(logLevel, exception,
            "Application error (Exit Code {ExitCode}): {UserMessage} - {TechnicalMessage}",
            errorInfo.ExitCode, errorInfo.UserMessage, errorInfo.TechnicalMessage);
        #pragma warning restore CA1848
    }

    /// <summary>
    /// Get user-friendly error message for exit code
    /// </summary>
    /// <param name="exitCode">Exit code</param>
    /// <returns>User-friendly description</returns>
    public static string GetExitCodeDescription(int exitCode)
    {
        return exitCode switch
        {
            ExitCodes.Success => "Success",
            ExitCodes.GeneralError => "General error",
            ExitCodes.InvalidROM => "Invalid or corrupted ROM file",
            ExitCodes.UnsupportedFeature => "Unsupported feature or mapper",
            ExitCodes.IOError => "File I/O error",
            _ => $"Unknown error (code {exitCode})"
        };
    }

    /// <summary>
    /// Error classification information
    /// </summary>
    private sealed record ErrorClassification(
        int ExitCode,
        string UserMessage,
        string TechnicalMessage,
        bool ShowDetails
    );
}

/// <summary>
/// Custom exception for invalid ROM data
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for exception handling")]
internal sealed class InvalidDataException : Exception
{
    public InvalidDataException() { }
    public InvalidDataException(string message) : base(message) { }
    public InvalidDataException(string message, Exception innerException) : base(message, innerException) { }
}
