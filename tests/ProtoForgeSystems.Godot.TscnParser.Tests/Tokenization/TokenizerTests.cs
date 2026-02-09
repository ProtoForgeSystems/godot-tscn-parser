using ProtoForgeSystems.Godot.TscnParser.Exceptions;
using ProtoForgeSystems.Godot.TscnParser.Tokenization;

namespace ProtoForgeSystems.Godot.TscnParser.Tests.Tokenization;

public class TokenizerTests
{
    private readonly Tokenizer _tokenizer = new();

    [Fact]
    public void TokenizeContent_Empty_ReturnsOnlyEof()
    {
        var tokens = _tokenizer.TokenizeContent("");
        Assert.Single(tokens);
        Assert.Equal(TokenType.Eof, tokens[0].Type);
    }

    [Fact]
    public void TokenizeContent_SingleCharTokens_RecognizesAll()
    {
        var tokens = _tokenizer.TokenizeContent("[ ] ( ) { } = : , . -");
        Assert.Equal(TokenType.BracketOpen, tokens[0].Type);
        Assert.Equal(TokenType.BracketClose, tokens[1].Type);
        Assert.Equal(TokenType.ParenOpen, tokens[2].Type);
        Assert.Equal(TokenType.ParenClose, tokens[3].Type);
        Assert.Equal(TokenType.BraceOpen, tokens[4].Type);
        Assert.Equal(TokenType.BraceClose, tokens[5].Type);
        Assert.Equal(TokenType.Equal, tokens[6].Type);
        Assert.Equal(TokenType.Colon, tokens[7].Type);
        Assert.Equal(TokenType.Comma, tokens[8].Type);
        Assert.Equal(TokenType.Period, tokens[9].Type);
        Assert.Equal(TokenType.Minus, tokens[10].Type);
        Assert.Equal(TokenType.Eof, tokens[11].Type);
    }

    [Theory]
    [InlineData("0", "0")]
    [InlineData("42", "42")]
    [InlineData("-7", "-7")]
    [InlineData("3.14", "3.14")]
    [InlineData(".5", ".5")]
    [InlineData("1e10", "1e10")]
    [InlineData("1e-5", "1e-5")]
    [InlineData("0xFF", "0xFF")]
    [InlineData("0x1a", "0x1a")]
    public void TokenizeContent_NumberFormats_Recognized(string input, string expectedValue)
    {
        var tokens = _tokenizer.TokenizeContent(input);
        Assert.Equal(2, tokens.Count); // Number + Eof
        Assert.Equal(TokenType.Number, tokens[0].Type);
        Assert.Equal(expectedValue, tokens[0].Value);
    }

    [Fact]
    public void TokenizeContent_StringLiteral_UnescapesValue()
    {
        var tokens = _tokenizer.TokenizeContent("\"hello\"");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenType.String, tokens[0].Type);
        Assert.Equal("hello", tokens[0].Value);
    }

    [Fact]
    public void TokenizeContent_StringWithEscapes_UnescapesCorrectly()
    {
        var tokens = _tokenizer.TokenizeContent("\"a\\\\b\\\"c\\nt\\r\\t\"");
        Assert.Equal(TokenType.String, tokens[0].Type);
        var v = tokens[0].Value;
        Assert.Equal('a', v[0]);
        Assert.Equal('\\', v[1]);
        Assert.Equal('b', v[2]);
        Assert.Equal('"', v[3]);
        Assert.Equal('c', v[4]);
        Assert.Equal('\n', v[5]); // \\n -> newline
        Assert.Equal('t', v[6]);   // literal t after \\n
        Assert.Equal('\r', v[7]);
        Assert.Equal('\t', v[8]);
    }

    [Fact]
    public void TokenizeContent_UnterminatedString_ThrowsTokenizationException()
    {
        var ex = Assert.Throws<TokenizationException>(() => _tokenizer.TokenizeContent("\"unclosed"));
        Assert.Contains("Unterminated", ex.Message);
    }

    [Fact]
    public void TokenizeContent_CommentLines_Skipped()
    {
        var tokens = _tokenizer.TokenizeContent("; comment\n[");
        Assert.Equal(TokenType.BracketOpen, tokens[0].Type);
        Assert.Equal(TokenType.Eof, tokens[1].Type);
    }

    [Fact]
    public void TokenizeContent_Identifier_Recognized()
    {
        var tokens = _tokenizer.TokenizeContent("node_name");
        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal("node_name", tokens[0].Value);
    }

    [Fact]
    public void TokenizeContent_IdentifierWithSlash_Recognized()
    {
        var tokens = _tokenizer.TokenizeContent("tracks/0/type");
        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal("tracks/0/type", tokens[0].Value);
    }

    [Fact]
    public void TokenizeContent_ColorKeywordBeforeParen_EmitsColorToken()
    {
        var tokens = _tokenizer.TokenizeContent("Color(");
        Assert.Equal(TokenType.Color, tokens[0].Type);
        Assert.Equal("Color", tokens[0].Value);
        Assert.Equal(TokenType.ParenOpen, tokens[1].Type);
    }

    [Fact]
    public void TokenizeContent_ColorAsIdentifier_WhenNotFollowedByParen()
    {
        var tokens = _tokenizer.TokenizeContent("Color");
        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal("Color", tokens[0].Value);
    }

    [Fact]
    public void TokenizeContent_UnrecognizedChar_EmitsErrorToken()
    {
        var tokens = _tokenizer.TokenizeContent("@");
        Assert.Equal(TokenType.Error, tokens[0].Type);
        Assert.Contains("Unrecognized", tokens[0].Value);
    }

    [Fact]
    public void TokenizeContent_LineAndColumn_TrackedCorrectly()
    {
        var tokens = _tokenizer.TokenizeContent("[\n [");
        Assert.Equal(1, tokens[0].Line);
        Assert.Equal(0, tokens[0].Column);
        Assert.Equal(2, tokens[1].Line);
        Assert.Equal(1, tokens[1].Column);
    }

    [Fact]
    public void TokenizeContent_WhitespaceBetweenTokens_Ignored()
    {
        var tokens = _tokenizer.TokenizeContent("  [   ]  ");
        Assert.Equal(TokenType.BracketOpen, tokens[0].Type);
        Assert.Equal(TokenType.BracketClose, tokens[1].Type);
        Assert.Equal(TokenType.Eof, tokens[2].Type);
    }
}
