using System;

namespace ProtoForgeSystems.Godot.TscnParser.Tokenization;

/// <summary>
/// A single token emitted by the tokenizer.
/// Includes position information for error diagnostics.
/// </summary>
public record Token(
    TokenType Type,
    string Value,       // Token text; for TokenType.String this is already unescaped by the tokenizer
    int Line,          // 1-indexed line number
    int Column         // 0-indexed column number (for diagnostics)
)
{
    /// <summary>
    /// For numeric tokens, parse Value as double.
    /// Returns null if token is not numeric.
    /// </summary>
    public double? AsNumber()
    {
        if (Type != TokenType.Number)
            return null;
        
        if (double.TryParse(Value, System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;

        // Handle hex notation: 0xFF
        if (Value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
            long.TryParse(Value.AsSpan(2), System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out var hexResult))
        {
            return hexResult;
        }
        
        return null;
    }
    
    /// <summary>
    /// For string tokens, return the string value.
    /// Value is already unescaped by the tokenizer during ReadString().
    /// </summary>
    public string AsStringValue()
    {
        if (Type != TokenType.String)
            throw new InvalidOperationException("Token is not a string");

        return Value;
    }
}
