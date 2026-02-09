using ProtoForgeSystems.Godot.TscnParser.Exceptions;
using ProtoForgeSystems.Godot.TscnParser.Models;
using ProtoForgeSystems.Godot.TscnParser.Parsing;

namespace ProtoForgeSystems.Godot.TscnParser.Tests.Parsing;

public class SceneParserTests
{
    private readonly TscnFileParser _parser = new();

    [Fact]
    public void ParseContent_MinimalScene_ReturnsSceneWithHeader()
    {
        var content = """
            [gd_scene load_steps=1 format=3]

            [node name="Root" type="Node"]
            """;
        var scene = _parser.ParseContent(content);
        Assert.NotNull(scene);
        Assert.Equal(1, scene.Header.LoadSteps);
        Assert.Equal(3, scene.Header.Format);
        Assert.Single(scene.Nodes);
        Assert.Equal("Root", scene.Nodes[0].Name);
        Assert.Equal("Node", scene.Nodes[0].Type);
        Assert.Null(scene.Nodes[0].ParentName);
    }

    [Fact]
    public void ParseContent_SceneWithExtResource_ParsesResources()
    {
        var content = """
            [gd_scene load_steps=2 format=3 uid="uid://abc"]

            [ext_resource type="Script" path="res://script.gd" id="1_abc"]

            [node name="Root" type="Node"]
            """;
        var scene = _parser.ParseContent(content);
        Assert.Single(scene.ExternalResources);
        Assert.Equal("Script", scene.ExternalResources[0].Type);
        Assert.Equal("res://script.gd", scene.ExternalResources[0].Path);
        Assert.Equal("1_abc", scene.ExternalResources[0].Id);
        Assert.Equal("uid://abc", scene.Header.Uid);
    }

    [Fact]
    public void ParseContent_SceneWithSubResource_ParsesSubResources()
    {
        var content = """
            [gd_scene load_steps=2 format=3]

            [sub_resource type="StandardMaterial3D" id="0"]
            albedo_color = Color(1, 1, 1, 1)

            [node name="Root" type="Node"]
            """;
        var scene = _parser.ParseContent(content);
        Assert.Single(scene.SubResources);
        Assert.Equal("StandardMaterial3D", scene.SubResources[0].Type);
        Assert.Equal("0", scene.SubResources[0].Id);
        Assert.True(scene.SubResources[0].Properties.ContainsKey("albedo_color"));
    }

    [Fact]
    public void ParseContent_SceneWithNodeHierarchy_ParsesParent()
    {
        var content = """
            [gd_scene load_steps=1 format=3]

            [node name="Root" type="Node"]

            [node name="Child" type="Node" parent="."]
            """;
        var scene = _parser.ParseContent(content);
        Assert.Equal(2, scene.Nodes.Count);
        Assert.Null(scene.Nodes[0].ParentName);
        Assert.Equal(".", scene.Nodes[1].ParentName);
    }

    [Fact]
    public void ParseContent_SceneWithConnection_ParsesConnection()
    {
        var content = """
            [gd_scene load_steps=1 format=3]

            [node name="Root" type="Node"]

            [node name="Button" type="Button" parent="."]

            [connection signal="pressed" from="." to="." method="on_pressed"]
            """;
        var scene = _parser.ParseContent(content);
        Assert.Single(scene.Connections);
        Assert.Equal("pressed", scene.Connections[0].Signal);
        Assert.Equal(".", scene.Connections[0].FromNode);
        Assert.Equal(".", scene.Connections[0].ToNode);
        Assert.Equal("on_pressed", scene.Connections[0].Method);
    }

    [Fact]
    public void ParseContent_SceneWithEditable_ParsesEditable()
    {
        var content = """
            [gd_scene load_steps=1 format=3]

            [node name="Root" type="Node"]

            [editable path="SubScene/Child"]
            """;
        var scene = _parser.ParseContent(content);
        Assert.Single(scene.EditableInstances);
        Assert.Equal("SubScene/Child", scene.EditableInstances[0]);
    }

    [Fact]
    public void ParseContent_NodeWithGroups_ParsesGroups()
    {
        var content = """
            [gd_scene load_steps=1 format=3]

            [node name="Root" type="Node" groups=["group1", "group2"]]
            """;
        var scene = _parser.ParseContent(content);
        var node = scene.Nodes[0];
        Assert.Contains("group1", node.Groups);
        Assert.Contains("group2", node.Groups);
    }

    [Fact]
    public void ParseContent_MissingFormat_ThrowsSceneParseException()
    {
        var content = "[gd_scene load_steps=1]\n[node name=\"Root\" type=\"Node\"]";
        var ex = Assert.Throws<SceneParseException>(() => _parser.ParseContent(content));
        Assert.Contains("format", ex.Message);
    }

    [Fact]
    public void FindNodeByName_ReturnsMatchingNode()
    {
        var content = """
            [gd_scene load_steps=1 format=3]

            [node name="Root" type="Node"]
            [node name="Child" type="Node" parent="."]
            """;
        var scene = _parser.ParseContent(content);
        var child = scene.FindNodeByName("Child");
        Assert.NotNull(child);
        Assert.Equal("Child", child.Name);
    }

    [Fact]
    public void FindNodeByName_NoMatch_ReturnsNull()
    {
        var content = """
            [gd_scene load_steps=1 format=3]

            [node name="Root" type="Node"]
            """;
        var scene = _parser.ParseContent(content);
        Assert.Null(scene.FindNodeByName("Missing"));
    }

    [Fact]
    public void BuildNodeNameIndex_ReturnsLookup()
    {
        var content = """
            [gd_scene load_steps=1 format=3]

            [node name="Root" type="Node"]
            [node name="A" type="Node" parent="."]
            """;
        var scene = _parser.ParseContent(content);
        var index = scene.BuildNodeNameIndex();
        Assert.Equal(2, index.Count);
        Assert.True(index.ContainsKey("Root"));
        Assert.True(index.ContainsKey("A"));
    }

    [Fact]
    public void TotalResources_CountsExtAndSub()
    {
        var content = """
            [gd_scene load_steps=3 format=3]

            [ext_resource type="Script" path="res://a.gd" id="1"]
            [sub_resource type="Material" id="0"]

            [node name="Root" type="Node"]
            """;
        var scene = _parser.ParseContent(content);
        Assert.Equal(2, scene.TotalResources);
    }
}
