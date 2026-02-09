namespace ProtoForgeSystems.Godot.TscnParser.Tokenization;

/// <summary>
/// Token types emitted by the TSCN tokenizer.
/// Based on Godot's VariantParser token types.
/// </summary>
public enum TokenType
{
    /// <summary>Opening bracket '['</summary>
    BracketOpen,
    /// <summary>Closing bracket ']'</summary>
    BracketClose,
    /// <summary>Opening parenthesis '('</summary>
    ParenOpen,
    /// <summary>Closing parenthesis ')'</summary>
    ParenClose,
    /// <summary>Opening brace '{'</summary>
    BraceOpen,
    /// <summary>Closing brace '}'</summary>
    BraceClose,
    
    /// <summary>Equal sign '='</summary>
    Equal,
    /// <summary>Colon ':'</summary>
    Colon,
    /// <summary>Comma ','</summary>
    Comma,
    /// <summary>Period '.'</summary>
    Period,
    /// <summary>Minus sign '-' (for negative numbers)</summary>
    Minus,
    
    /// <summary>Identifier (node names, property names, keywords)</summary>
    Identifier,
    /// <summary>String literal with escape sequence handling</summary>
    String,
    /// <summary>Number (integers and floats: 1, 1.5, -5, 0xFF, 1e-5)</summary>
    Number,
    /// <summary>Color value Color(r,g,b,a)</summary>
    Color,
    
    /// <summary>End of file marker</summary>
    Eof,
    /// <summary>Invalid token (includes error message in Value)</summary>
    Error
}
