using ProtoForgeSystems.Godot.TscnParser.Exceptions;
using ProtoForgeSystems.Godot.TscnParser.Extraction;
using ProtoForgeSystems.Godot.TscnParser.Parsing;
using ProtoForgeSystems.Godot.TscnParser.Tokenization;

namespace ProtoForgeSystems.Godot.TscnParser.Tests.Exceptions;

public class ExceptionTests
{
    [Fact]
    public void TokenizationException_ContainsLineAndColumn()
    {
        var tokenizer = new Tokenizer();
        var ex = Assert.Throws<TokenizationException>(() => tokenizer.TokenizeContent("\"unclosed"));
        Assert.True(ex.Line > 0);
        Assert.True(ex.Column >= 0);
        Assert.IsAssignableFrom<TscnParseException>(ex);
    }

    [Fact]
    public void ValueParseException_ContainsMessageAndToken()
    {
        var ex = Assert.Throws<ValueParseException>(() =>
            TscnValueExtractor.ExtractValue("NotAType(1)"));
        Assert.NotNull(ex.Message);
        Assert.NotNull(ex.FailedToken);
        Assert.IsAssignableFrom<TscnParseException>(ex);
    }

    [Fact]
    public void ValueParseException_EmptyInput_HasLineInfoInMessage()
    {
        var ex = Assert.Throws<ValueParseException>(() =>
            TscnValueExtractor.ExtractValue(""));
        Assert.Contains("Unexpected", ex.Message);
        Assert.True(ex.Line >= 0);
        Assert.True(ex.Column >= 0);
    }

    [Fact]
    public void SceneParseException_MissingFormat_ContainsMessage()
    {
        var parser = new TscnFileParser();
        var ex = Assert.Throws<SceneParseException>(() =>
            parser.ParseContent("[gd_scene load_steps=1]\n[node name=\"Root\" type=\"Node\"]"));
        Assert.Contains("format", ex.Message);
        Assert.IsAssignableFrom<TscnParseException>(ex);
    }

    [Fact]
    public void TscnParseException_WithToken_FormatsMessageWithLineColumn()
    {
        var ex = Assert.Throws<ValueParseException>(() =>
            TscnValueExtractor.ExtractValue("1 2"));
        Assert.True(ex.Line >= 0);
        Assert.True(ex.Column >= 0);
        Assert.NotNull(ex.FailedToken);
        Assert.Contains("line", ex.Message);
        Assert.Contains("column", ex.Message);
    }
}
