using ProtoForgeSystems.Godot.TscnParser.Models;
using ProtoForgeSystems.Godot.TscnParser.Parsing;

namespace ProtoForgeSystems.Godot.TscnParser.Tests.Models;

public class GodotSceneTests
{
    private static GodotScene ParseScene(string content)
    {
        var parser = new TscnFileParser();
        return parser.ParseContent(content);
    }

    [Fact]
    public void FindExternalResource_ById_ReturnsResource()
    {
        var scene = ParseScene("""
            [gd_scene load_steps=2 format=3]
            [ext_resource type="Script" path="res://script.gd" id="1_xyz"]
            [node name="Root" type="Node"]
            """);
        var res = scene.FindExternalResource("1_xyz");
        Assert.NotNull(res);
        Assert.Equal("1_xyz", res.Id);
    }

    [Fact]
    public void FindExternalResource_NoMatch_ReturnsNull()
    {
        var scene = ParseScene("""
            [gd_scene load_steps=1 format=3]
            [node name="Root" type="Node"]
            """);
        Assert.Null(scene.FindExternalResource("99"));
    }

    [Fact]
    public void FindSubResource_ById_ReturnsResource()
    {
        var scene = ParseScene("""
            [gd_scene load_steps=2 format=3]
            [sub_resource type="Material" id="0"]
            [node name="Root" type="Node"]
            """);
        var res = scene.FindSubResource("0");
        Assert.NotNull(res);
        Assert.Equal("0", res.Id);
    }

    [Fact]
    public void BuildExternalResourceIndex_ReturnsLookup()
    {
        var scene = ParseScene("""
            [gd_scene load_steps=3 format=3]
            [ext_resource type="Script" path="res://a.gd" id="1"]
            [ext_resource type="Texture" path="res://t.png" id="2"]
            [node name="Root" type="Node"]
            """);
        var index = scene.BuildExternalResourceIndex();
        Assert.Equal(2, index.Count);
        Assert.True(index.ContainsKey("1"));
        Assert.True(index.ContainsKey("2"));
    }

    [Fact]
    public void BuildSubResourceIndex_ReturnsLookup()
    {
        var scene = ParseScene("""
            [gd_scene load_steps=3 format=3]
            [sub_resource type="A" id="0"]
            [sub_resource type="B" id="1"]
            [node name="Root" type="Node"]
            """);
        var index = scene.BuildSubResourceIndex();
        Assert.Equal(2, index.Count);
        Assert.True(index.ContainsKey("0"));
        Assert.True(index.ContainsKey("1"));
    }

    [Fact]
    public void BuildParentIndex_ReturnsChildrenByParent()
    {
        var scene = ParseScene("""
            [gd_scene load_steps=1 format=3]
            [node name="Root" type="Node"]
            [node name="A" type="Node" parent="."]
            [node name="B" type="Node" parent="."]
            [node name="A1" type="Node" parent="A"]
            """);
        var index = scene.BuildParentIndex();
        Assert.True(index.ContainsKey("."));
        Assert.Equal(2, index["."].Count);
        Assert.True(index.ContainsKey("A"));
        Assert.Single(index["A"]);
        Assert.Equal("A1", index["A"][0].Name);
    }

    [Fact]
    public void Node_WithNumericProperty_ParsesProperty()
    {
        var scene = ParseScene("""
            [gd_scene load_steps=1 format=3]
            [node name="Root" type="Node"]
            some_int = 42
            """);
        Assert.True(scene.Nodes[0].Properties.ContainsKey("some_int"));
        var lit = Assert.IsType<LiteralValue>(scene.Nodes[0].Properties["some_int"]);
        Assert.Equal(42.0, lit.Value);
    }

    [Fact]
    public void Connection_WithBinds_ParsesBinds()
    {
        var scene = ParseScene("""
            [gd_scene load_steps=1 format=3]
            [node name="Root" type="Node"]
            [connection signal="pressed" from="." to="." method="handler" binds=[1, 2]]
            """);
        Assert.NotNull(scene.Connections[0].Binds);
        Assert.Equal(2, scene.Connections[0].Binds!.Count);
    }
}
