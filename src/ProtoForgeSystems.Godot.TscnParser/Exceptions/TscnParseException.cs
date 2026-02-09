using System;
using ProtoForgeSystems.Godot.TscnParser.Tokenization;

namespace ProtoForgeSystems.Godot.TscnParser.Exceptions;

/// <summary>
/// Base class for all TSCN parsing exceptions.
/// Provides common error message formatting with line/column information.
/// </summary>
public abstract class TscnParseException : Exception
{
    /// <summary>
    /// The token that caused the parsing failure, if available.
    /// </summary>
    public Token? FailedToken { get; }

    /// <summary>
    /// The line number where the error occurred.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// The column number where the error occurred.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Initializes a new instance with a token reference.
    /// </summary>
    /// <param name="errorType">The type of error (e.g., "Scene parse error").</param>
    /// <param name="message">The error message.</param>
    /// <param name="token">The token that caused the failure, if available.</param>
    protected TscnParseException(string errorType, string message, Token? token = null)
        : base(FormatErrorMessage(errorType, message, token?.Line, token?.Column))
    {
        FailedToken = token;
        Line = token?.Line ?? 0;
        Column = token?.Column ?? 0;
    }

    /// <summary>
    /// Initializes a new instance with a token reference and inner exception.
    /// </summary>
    /// <param name="errorType">The type of error (e.g., "Scene parse error").</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    protected TscnParseException(string errorType, string message, Exception? innerException)
        : base(FormatErrorMessage(errorType, message, null, null), innerException)
    {
        FailedToken = null;
        Line = 0;
        Column = 0;
    }

    /// <summary>
    /// Initializes a new instance with explicit line/column.
    /// </summary>
    /// <param name="errorType">The type of error (e.g., "Tokenization error").</param>
    /// <param name="message">The error message.</param>
    /// <param name="line">The line number where the error occurred.</param>
    /// <param name="column">The column number where the error occurred.</param>
    protected TscnParseException(string errorType, string message, int line, int column)
        : base(FormatErrorMessage(errorType, message, line, column))
    {
        FailedToken = null;
        Line = line;
        Column = column;
    }

    private static string FormatErrorMessage(string errorType, string message, int? line, int? column)
    {
        return line.HasValue && column.HasValue
            ? $"{errorType} at line {line}, column {column}: {message}"
            : $"{errorType}: {message}";
    }
}
