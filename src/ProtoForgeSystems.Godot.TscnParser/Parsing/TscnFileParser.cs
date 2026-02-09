using ProtoForgeSystems.Godot.TscnParser.Models;
using ProtoForgeSystems.Godot.TscnParser.Tokenization;

namespace ProtoForgeSystems.Godot.TscnParser.Parsing;

/// <summary>
/// High-level public API for parsing complete .tscn files end-to-end.
/// Convenience wrapper that tokenizes and parses in one step.
/// </summary>
public class TscnFileParser
{
    private readonly Tokenizer _tokenizer = new();
    private readonly SceneParser _sceneParser = new();
    
    /// <summary>
    /// Parse a .tscn file completely (tokenize + parse structure).
    /// </summary>
    public GodotScene ParseFile(string filePath)
    {
        var tokens = _tokenizer.Tokenize(filePath);
        return _sceneParser.Parse(tokens);
    }
    
    /// <summary>
    /// Parse from content string (for testing).
    /// </summary>
    public GodotScene ParseContent(string content)
    {
        var tokens = _tokenizer.TokenizeContent(content);
        return _sceneParser.Parse(tokens);
    }
}
