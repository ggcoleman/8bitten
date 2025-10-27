// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// Suppress CA1848 (LoggerMessage) warnings for Infrastructure project
// These are performance optimizations that are not critical for this project
[assembly: SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates for improved performance", Justification = "Performance optimization not critical for this infrastructure layer")]

// Suppress CA1031 (General exception types) warnings
// Infrastructure layer needs to catch general exceptions for robustness
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Infrastructure layer requires robust exception handling")]

// Suppress CA1024 (Use properties where appropriate) warnings
// Some methods are intentionally methods for clarity and consistency
[assembly: SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Methods are intentionally used for clarity and side effects")]

// Suppress CA1305 (Specify IFormatProvider) warnings for StringBuilder
// Culture-specific formatting is not a concern for internal diagnostic strings
[assembly: SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "Culture-specific formatting not required for internal diagnostic output")]

// Suppress CA1869 (Cache JsonSerializerOptions) warnings
// Configuration serialization is infrequent and caching is not necessary
[assembly: SuppressMessage("Performance", "CA1869:Cache and reuse 'JsonSerializerOptions' instances", Justification = "Configuration serialization is infrequent")]
