// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// Suppress CA1515 (Make types internal) warnings for CLI application
// CLI application types may need to be public for testing and extensibility
[assembly: SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "CLI application types may need to be public for testing and extensibility")]

// Suppress CA1848 (LoggerMessage) warnings for CLI application
// Performance optimization not critical for CLI application
[assembly: SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates for improved performance", Justification = "Performance optimization not critical for CLI application")]

// Suppress CA1031 (General exception types) warnings
// CLI application needs to catch general exceptions for user-friendly error handling
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "CLI application requires robust exception handling for user experience")]

// Suppress CA1849 (Synchronous blocking) warnings
// CLI application may need synchronous operations for simplicity
[assembly: SuppressMessage("Reliability", "CA1849:Call async methods when in an async context", Justification = "CLI application may require synchronous operations")]

// Suppress CA1725 (Parameter naming) warnings
// Interface implementations may have different parameter naming conventions
[assembly: SuppressMessage("Naming", "CA1725:Parameter names should match base declaration", Justification = "Interface implementations may use different naming for clarity")]

// Suppress CA1805 (Explicit initialization) warnings
// Explicit initialization improves code clarity in configuration classes
[assembly: SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "Explicit initialization improves code clarity")]

// Suppress CA1308 (ToUpperInvariant) warnings
// CLI applications may need case-insensitive string comparisons
[assembly: SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "CLI applications may require case-insensitive operations")]

// Suppress CA2007 (ConfigureAwait) warnings
// CLI applications typically don't need ConfigureAwait(false)
[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "CLI applications typically run on main thread")]

// Suppress CA2000 (Dispose objects) warnings for dependency injection scenarios
// Objects created for dependency injection are managed by the DI container
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Objects managed by dependency injection container")]
