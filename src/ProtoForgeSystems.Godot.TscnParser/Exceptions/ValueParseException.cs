using System;
using ProtoForgeSystems.Godot.TscnParser.Tokenization;

namespace ProtoForgeSystems.Godot.TscnParser.Exceptions;

/// <summary>
/// Thrown when value parsing fails.
/// Includes token position for error reporting.
/// </summary>
public class ValueParseException : TscnParseException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValueParseException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="token">The token that caused the failure, if available.</param>
    public ValueParseException(string message, Token? token = null)
        : base("Value parse error", message, token)
    {
    }
}
