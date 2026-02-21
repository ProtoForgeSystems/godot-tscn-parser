using System.Collections.Generic;

namespace ProtoForgeSystems.Godot.TscnParser.Models;

/// <summary>
/// Base interface for all Godot property values.
/// Enables type-safe parsing of nested structures.
/// </summary>
public interface IGodotValue { }

/// <summary>Literal values: float, int, bool, string, null</summary>
public record LiteralValue(object? Value) : IGodotValue;

/// <summary>Vector3(x, y, z)</summary>
public record Vector3Value(double X, double Y, double Z) : IGodotValue;

/// <summary>Vector2(x, y)</summary>
public record Vector2Value(double X, double Y) : IGodotValue;

/// <summary>Vector4(x, y, z, w)</summary>
public record Vector4Value(double X, double Y, double Z, double W) : IGodotValue;

/// <summary>
/// Transform3D with basis (3x3 rotation/scale) and origin (translation).
/// Full form: Transform3D(r1,r2,r3, r4,r5,r6, r7,r8,r9, tx,ty,tz)
/// Basis is stored as 9 numbers (3x3 matrix), origin is 3 numbers.
/// </summary>
public record Transform3DValue(
    double[] Basis,  // 9 elements: [r1,r2,r3, r4,r5,r6, r7,r8,r9]
    double OriginX, double OriginY, double OriginZ
) : IGodotValue;

/// <summary>Color(r, g, b, a)</summary>
public record ColorValue(double R, double G, double B, double A) : IGodotValue;

/// <summary>ExtResource("id") - reference to external resource</summary>
public record ExtResourceValue(string Id) : IGodotValue;

/// <summary>SubResource("id") - reference to internal sub-resource</summary>
public record SubResourceValue(string Id) : IGodotValue;

/// <summary>Array of values: [item1, item2, ...]</summary>
public record ArrayValue(List<IGodotValue> Items) : IGodotValue;

/// <summary>Dictionary of key-value pairs: {key: value, ...}</summary>
public record DictionaryValue(Dictionary<string, IGodotValue> Items) : IGodotValue;

/// <summary>NodePath("path/to/node") - path to scene node</summary>
public record NodePathValue(string Path) : IGodotValue;

/// <summary>Basis (3x3 rotation/scale matrix)</summary>
public record BasisValue(double[] Rows) : IGodotValue;  // 9 elements

/// <summary>Quaternion(x, y, z, w) - rotation quaternion</summary>
public record QuaternionValue(double X, double Y, double Z, double W) : IGodotValue;

/// <summary>AABB(position, size) - axis-aligned bounding box</summary>
public record AABBValue(Vector3Value Position, Vector3Value Size) : IGodotValue;

/// <summary>Plane(normal, distance)</summary>
public record PlaneValue(Vector3Value Normal, double Distance) : IGodotValue;

/// <summary>PackedInt32Array(...) - packed array of 32-bit integers</summary>
public record PackedInt32ArrayValue(List<int> Values) : IGodotValue;

/// <summary>PackedVector3Array(...) - packed array of Vector3 values</summary>
public record PackedVector3ArrayValue(List<Vector3Value> Values) : IGodotValue;
