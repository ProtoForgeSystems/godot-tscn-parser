using System;
using ProtoForgeSystems.Godot.TscnParser.Tokenization;

namespace ProtoForgeSystems.Godot.TscnParser.Exceptions;

/// <summary>
/// Thrown when scene structure parsing fails.
/// </summary>
public class SceneParseException : TscnParseException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SceneParseException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="token">The token that caused the failure, if available.</param>
    public SceneParseException(string message, Token? token = null)
        : base("Scene parse error", message, token)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneParseException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public SceneParseException(string message, Exception innerException)
        : base("Scene parse error", message, innerException)
    {
    }
}
