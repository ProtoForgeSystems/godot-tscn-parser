using ProtoForgeSystems.Godot.TscnParser.Exceptions;
using ProtoForgeSystems.Godot.TscnParser.Extraction;
using ProtoForgeSystems.Godot.TscnParser.Models;

namespace ProtoForgeSystems.Godot.TscnParser.Tests.Extraction;

public class TscnValueExtractorTests
{
    [Theory]
    [InlineData("0", 0.0)]
    [InlineData("42", 42.0)]
    [InlineData("-1", -1.0)]
    [InlineData("3.14", 3.14)]
    [InlineData("1e2", 100.0)]
    public void ExtractValue_Number_ReturnsLiteralValue(string input, double expected)
    {
        var value = TscnValueExtractor.ExtractValue(input);
        var lit = Assert.IsType<LiteralValue>(value);
        Assert.Equal(expected, Convert.ToDouble(lit.Value));
    }

    [Fact]
    public void ExtractValue_String_ReturnsLiteralValue()
    {
        var value = TscnValueExtractor.ExtractValue("\"hello\"");
        var lit = Assert.IsType<LiteralValue>(value);
        Assert.Equal("hello", lit.Value);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void ExtractValue_Boolean_ReturnsLiteralValue(string input, bool expected)
    {
        var value = TscnValueExtractor.ExtractValue(input);
        var lit = Assert.IsType<LiteralValue>(value);
        Assert.Equal(expected, lit.Value);
    }

    [Fact]
    public void ExtractValue_Null_ReturnsLiteralValue()
    {
        var value = TscnValueExtractor.ExtractValue("null");
        var lit = Assert.IsType<LiteralValue>(value);
        Assert.Null(lit.Value);
    }

    [Fact]
    public void ExtractValue_Vector2_ReturnsVector2Value()
    {
        var value = TscnValueExtractor.ExtractValue("Vector2(1.0, 2.0)");
        var v = Assert.IsType<Vector2Value>(value);
        Assert.Equal(1.0, v.X);
        Assert.Equal(2.0, v.Y);
    }

    [Fact]
    public void ExtractValue_Vector3_ReturnsVector3Value()
    {
        var value = TscnValueExtractor.ExtractValue("Vector3(1.0, 2.0, 3.0)");
        var v = Assert.IsType<Vector3Value>(value);
        Assert.Equal(1.0, v.X);
        Assert.Equal(2.0, v.Y);
        Assert.Equal(3.0, v.Z);
    }

    [Fact]
    public void ExtractValue_Vector4_ReturnsVector4Value()
    {
        var value = TscnValueExtractor.ExtractValue("Vector4(1, 2, 3, 4)");
        var v = Assert.IsType<Vector4Value>(value);
        Assert.Equal(1, v.X);
        Assert.Equal(2, v.Y);
        Assert.Equal(3, v.Z);
        Assert.Equal(4, v.W);
    }

    [Fact]
    public void ExtractValue_Color_ReturnsColorValue()
    {
        var value = TscnValueExtractor.ExtractValue("Color(0.5, 0.5, 0.5, 1.0)");
        var c = Assert.IsType<ColorValue>(value);
        Assert.Equal(0.5, c.R);
        Assert.Equal(0.5, c.G);
        Assert.Equal(0.5, c.B);
        Assert.Equal(1.0, c.A);
    }

    [Fact]
    public void ExtractValue_ExtResource_ReturnsExtResourceValue()
    {
        var value = TscnValueExtractor.ExtractValue("ExtResource(\"1_abc\")");
        var ext = Assert.IsType<ExtResourceValue>(value);
        Assert.Equal("1_abc", ext.Id);
    }

    [Fact]
    public void ExtractValue_SubResource_ReturnsSubResourceValue()
    {
        var value = TscnValueExtractor.ExtractValue("SubResource(\"0\")");
        var sub = Assert.IsType<SubResourceValue>(value);
        Assert.Equal("0", sub.Id);
    }

    [Fact]
    public void ExtractValue_NodePath_ReturnsNodePathValue()
    {
        var value = TscnValueExtractor.ExtractValue("NodePath(\"../Sibling\")");
        var np = Assert.IsType<NodePathValue>(value);
        Assert.Equal("../Sibling", np.Path);
    }

    [Fact]
    public void ExtractValue_Array_ReturnsArrayValue()
    {
        var value = TscnValueExtractor.ExtractValue("[1, 2, 3]");
        var arr = Assert.IsType<ArrayValue>(value);
        Assert.Equal(3, arr.Items.Count);
        Assert.Equal(1.0, ((LiteralValue)arr.Items[0]).Value);
        Assert.Equal(2.0, ((LiteralValue)arr.Items[1]).Value);
        Assert.Equal(3.0, ((LiteralValue)arr.Items[2]).Value);
    }

    [Fact]
    public void ExtractValue_EmptyArray_ReturnsArrayValue()
    {
        var value = TscnValueExtractor.ExtractValue("[]");
        var arr = Assert.IsType<ArrayValue>(value);
        Assert.Empty(arr.Items);
    }

    [Fact]
    public void ExtractValue_Dictionary_ReturnsDictionaryValue()
    {
        var value = TscnValueExtractor.ExtractValue("{\"key\": \"value\"}");
        var dict = Assert.IsType<DictionaryValue>(value);
        Assert.Single(dict.Items);
        var lit = Assert.IsType<LiteralValue>(dict.Items["key"]);
        Assert.Equal("value", lit.Value);
    }

    [Fact]
    public void ExtractValue_Transform3D_ReturnsTransform3DValue()
    {
        var value = TscnValueExtractor.ExtractValue(
            "Transform3D(1,0,0, 0,1,0, 0,0,1, 0,0,0)");
        var t = Assert.IsType<Transform3DValue>(value);
        Assert.Equal(9, t.Basis.Length);
        Assert.Equal(0, t.OriginX);
        Assert.Equal(0, t.OriginY);
        Assert.Equal(0, t.OriginZ);
    }

    [Fact]
    public void ExtractValue_Quaternion_ReturnsQuaternionValue()
    {
        var value = TscnValueExtractor.ExtractValue("Quaternion(0, 0, 0, 1)");
        var q = Assert.IsType<QuaternionValue>(value);
        Assert.Equal(0, q.X);
        Assert.Equal(0, q.Y);
        Assert.Equal(0, q.Z);
        Assert.Equal(1, q.W);
    }

    [Fact]
    public void ExtractValue_Plane_ReturnsPlaneValue()
    {
        var value = TscnValueExtractor.ExtractValue("Plane(Vector3(1, 0, 0), 5.0)");
        var p = Assert.IsType<PlaneValue>(value);
        Assert.Equal(1, p.Normal.X);
        Assert.Equal(0, p.Normal.Y);
        Assert.Equal(0, p.Normal.Z);
        Assert.Equal(5.0, p.Distance);
    }

    [Fact]
    public void ExtractValue_PackedInt32Array_ReturnsPackedInt32ArrayValue()
    {
        var value = TscnValueExtractor.ExtractValue("PackedInt32Array(1, 2, 3)");
        var arr = Assert.IsType<PackedInt32ArrayValue>(value);
        Assert.Equal(3, arr.Values.Count);
        Assert.Equal(1, arr.Values[0]);
        Assert.Equal(2, arr.Values[1]);
        Assert.Equal(3, arr.Values[2]);
    }

    [Fact]
    public void ExtractValue_TypedArray_ReturnsArrayValue()
    {
        var value = TscnValueExtractor.ExtractValue("Array[int]([1, 2, 3])");
        var arr = Assert.IsType<ArrayValue>(value);
        Assert.Equal(3, arr.Items.Count);
    }

    [Fact]
    public void ExtractValue_ExtraTokens_ThrowsValueParseException()
    {
        var ex = Assert.Throws<ValueParseException>(() =>
            TscnValueExtractor.ExtractValue("1 2"));
        Assert.Contains("Unexpected", ex.Message);
    }

    [Fact]
    public void ExtractValue_InvalidValue_Throws()
    {
        Assert.Throws<ValueParseException>(() =>
            TscnValueExtractor.ExtractValue("NotAType(1)"));
    }

    [Fact]
    public void ExtractValue_EmptyString_Throws()
    {
        var ex = Assert.Throws<ValueParseException>(() =>
            TscnValueExtractor.ExtractValue(""));
        Assert.Contains("Unexpected", ex.Message);
    }
}
