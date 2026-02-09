using ProtoForgeSystems.Godot.TscnParser.Tokenization;

namespace ProtoForgeSystems.Godot.TscnParser.Tests.Tokenization;

public class TokenTests
{
    [Fact]
    public void AsNumber_NumberToken_ReturnsParsedDouble()
    {
        var token = new Token(TokenType.Number, "42", 1, 0);
        Assert.Equal(42, token.AsNumber());
    }

    [Fact]
    public void AsNumber_FloatToken_ReturnsParsedDouble()
    {
        var token = new Token(TokenType.Number, "3.14", 1, 0);
        Assert.Equal(3.14, token.AsNumber());
    }

    [Fact]
    public void AsNumber_HexToken_ReturnsParsedDouble()
    {
        var token = new Token(TokenType.Number, "0xFF", 1, 0);
        Assert.Equal(255, token.AsNumber());
    }

    [Fact]
    public void AsNumber_NonNumberToken_ReturnsNull()
    {
        var token = new Token(TokenType.Identifier, "foo", 1, 0);
        Assert.Null(token.AsNumber());
    }

    [Fact]
    public void AsStringValue_StringToken_ReturnsValue()
    {
        var token = new Token(TokenType.String, "hello", 1, 0);
        Assert.Equal("hello", token.AsStringValue());
    }

    [Fact]
    public void AsStringValue_NonStringToken_Throws()
    {
        var token = new Token(TokenType.Identifier, "foo", 1, 0);
        Assert.Throws<InvalidOperationException>(() => token.AsStringValue());
    }
}
