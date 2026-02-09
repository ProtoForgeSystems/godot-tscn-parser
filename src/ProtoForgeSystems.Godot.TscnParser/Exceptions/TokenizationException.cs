namespace ProtoForgeSystems.Godot.TscnParser.Exceptions;

/// <summary>
/// Thrown when tokenization fails.
/// Includes line/column for error reporting.
/// </summary>
public class TokenizationException : TscnParseException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenizationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="line">The line number where tokenization failed.</param>
    /// <param name="column">The column number where tokenization failed.</param>
    public TokenizationException(string message, int line, int column)
        : base("Tokenization error", message, line, column)
    {
    }
}
