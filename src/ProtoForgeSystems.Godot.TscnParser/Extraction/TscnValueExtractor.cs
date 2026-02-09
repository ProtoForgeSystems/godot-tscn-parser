using ProtoForgeSystems.Godot.TscnParser.Models;
using ProtoForgeSystems.Godot.TscnParser.Tokenization;
using ProtoForgeSystems.Godot.TscnParser.Exceptions;

namespace ProtoForgeSystems.Godot.TscnParser.Extraction;

/// <summary>
/// Convenience helper to tokenize and parse Godot property values in one step.
/// </summary>
public static class TscnValueExtractor
{
    /// <summary>
    /// Tokenize and parse a property value in one call.
    /// Throws on any parsing error with clear error message.
    /// </summary>
    public static IGodotValue ExtractValue(string valueText)
    {
        var tokenizer = new Tokenizer();
        var tokens = tokenizer.TokenizeContent(valueText);
        var parser = new Parsing.ValueParser();
        var value = parser.ParseValue(tokens, 0, out var endIndex);
        
        // Verify we consumed all tokens (except EOF)
        if (endIndex < tokens.Count - 1)
        {
            var nextToken = tokens[endIndex];
            throw new ValueParseException($"Unexpected token after value: {nextToken.Value}", nextToken);
        }
        
        return value;
    }
}
