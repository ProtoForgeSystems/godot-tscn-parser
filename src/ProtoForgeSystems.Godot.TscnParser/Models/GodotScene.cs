using System.Collections.Generic;
using System.Linq;

namespace ProtoForgeSystems.Godot.TscnParser.Models;

/// <summary>
/// Complete representation of a parsed .tscn scene file.
/// Mirrors Godot's PackedScene structure.
/// </summary>
public record GodotScene(
    SceneHeader Header,
    List<ExternalResource> ExternalResources,
    List<SubResource> SubResources,
    List<GodotNode> Nodes,
    List<Connection> Connections,
    List<string> EditableInstances,
    List<string> ParsingWarnings
)
{
    /// <summary>
    /// Gets the total count of external and sub-resources combined.
    /// </summary>
    /// <remarks>
    /// Useful for diagnostics and determining scene complexity.
    /// </remarks>
    public int TotalResources => ExternalResources.Count + SubResources.Count;

    /// <summary>
    /// Finds a node by its name using linear search.
    /// </summary>
    /// <param name="name">The name of the node to find.</param>
    /// <returns>The matching node, or null if not found.</returns>
    /// <remarks>
    /// This method performs a linear search (O(n)). For repeated lookups
    /// in large scenes, consider using <see cref="BuildNodeNameIndex"/>
    /// to create an indexed lookup.
    /// </remarks>
    public GodotNode? FindNodeByName(string name)
        => Nodes.FirstOrDefault(n => n.Name == name);

    /// <summary>
    /// Finds an external resource by its identifier using linear search.
    /// </summary>
    /// <param name="id">The unique identifier of the external resource.</param>
    /// <returns>The matching external resource, or null if not found.</returns>
    /// <remarks>
    /// This method performs a linear search (O(n)). For repeated lookups
    /// in large scenes, consider using <see cref="BuildExternalResourceIndex"/>
    /// to create an indexed lookup.
    /// </remarks>
    public ExternalResource? FindExternalResource(string id)
        => ExternalResources.FirstOrDefault(r => r.Id == id);

    /// <summary>
    /// Finds a sub-resource by its identifier using linear search.
    /// </summary>
    /// <param name="id">The unique identifier of the sub-resource.</param>
    /// <returns>The matching sub-resource, or null if not found.</returns>
    /// <remarks>
    /// This method performs a linear search (O(n)). For repeated lookups
    /// in large scenes, consider using <see cref="BuildSubResourceIndex"/>
    /// to create an indexed lookup.
    /// </remarks>
    public SubResource? FindSubResource(string id)
        => SubResources.FirstOrDefault(r => r.Id == id);

    /// <summary>
    /// Builds a dictionary for fast O(1) node lookups by name.
    /// </summary>
    /// <returns>A dictionary mapping node names to node instances.</returns>
    /// <remarks>
    /// Use this for performance-critical scenarios where you need to
    /// perform multiple node lookups in a large scene.
    /// If multiple nodes share the same name, only the first encountered
    /// node is included in the index; subsequent duplicates are ignored.
    /// </remarks>
    public Dictionary<string, GodotNode> BuildNodeNameIndex()
    {
        var index = new Dictionary<string, GodotNode>(StringComparer.Ordinal);

        foreach (var node in Nodes)
        {
            if (string.IsNullOrEmpty(node.Name))
                continue;

            index.TryAdd(node.Name, node);
        }

        return index;
    }

    /// <summary>
    /// Builds a dictionary for fast O(1) external resource lookups by identifier.
    /// </summary>
    /// <returns>A dictionary mapping resource IDs to external resource instances.</returns>
    /// <remarks>
    /// Use this for performance-critical scenarios where you need to
    /// perform multiple resource lookups in a large scene.
    /// If multiple resources share the same ID, only the first encountered
    /// resource is included in the index; subsequent duplicates are ignored.
    /// </remarks>
    public Dictionary<string, ExternalResource> BuildExternalResourceIndex()
    {
        var index = new Dictionary<string, ExternalResource>(StringComparer.Ordinal);

        foreach (var resource in ExternalResources)
        {
            if (string.IsNullOrEmpty(resource.Id))
                continue;

            index.TryAdd(resource.Id, resource);
        }

        return index;
    }

    /// <summary>
    /// Builds a dictionary for fast O(1) sub-resource lookups by identifier.
    /// </summary>
    /// <returns>A dictionary mapping resource IDs to sub-resource instances.</returns>
    /// <remarks>
    /// Use this for performance-critical scenarios where you need to
    /// perform multiple resource lookups in a large scene.
    /// If multiple resources share the same ID, only the first encountered
    /// resource is included in the index; subsequent duplicates are ignored.
    /// </remarks>
    public Dictionary<string, SubResource> BuildSubResourceIndex()
    {
        var index = new Dictionary<string, SubResource>(StringComparer.Ordinal);

        foreach (var resource in SubResources)
        {
            if (string.IsNullOrEmpty(resource.Id))
                continue;

            index.TryAdd(resource.Id, resource);
        }

        return index;
    }

    /// <summary>
    /// Builds a parent-child index for efficient hierarchy queries.
    /// </summary>
    /// <returns>A dictionary mapping parent node names to their child nodes.</returns>
    /// <remarks>
    /// Use this to efficiently query node hierarchies without traversing
    /// the entire node list repeatedly. Root nodes (with null parent)
    /// are not included in the returned dictionary.
    /// </remarks>
    public Dictionary<string, List<GodotNode>> BuildParentIndex()
    {
        var index = new Dictionary<string, List<GodotNode>>();
        foreach (var node in Nodes.Where(n => n.ParentName != null))
        {
            var parentName = node.ParentName!;
            if (!index.ContainsKey(parentName))
                index[parentName] = new();
            index[parentName].Add(node);
        }
        return index;
    }
}

/// <summary>Scene file header</summary>
public record SceneHeader(
    int LoadSteps,
    int Format,
    string? Uid
);

/// <summary>External resource reference</summary>
public record ExternalResource(
    string Type,
    string Path,
    string? Uid,
    string Id  // Unique identifier within scene
);

/// <summary>Internal sub-resource (e.g., materials, meshes)</summary>
public record SubResource(
    string Type,
    string Id,
    Dictionary<string, IGodotValue> Properties
);

/// <summary>Scene node (hierarchy element)</summary>
public record GodotNode(
    string Name,
    string? Type,           // e.g., "Node3D", "StaticBody3D", null if inheriting
    string? ParentName,     // null if root
    string? Instance,       // for scene inheritance: ExtResource("id")
    string? InstancePlaceholder,
    List<string> Groups,    // node groups (tags)
    Dictionary<string, IGodotValue> Properties,
    int? UniqueId           // unique_id= attribute
);

/// <summary>Signal connection between nodes</summary>
public record Connection(
    string Signal,
    string FromNode,
    string ToNode,
    string Method,
    List<IGodotValue>? Binds,
    int Flags,              // connection flags (default: Object.CONNECT_PERSIST)
    int? Unbinds
);
