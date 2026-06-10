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
/// Full form: Transform3D(b0,b1,b2, b3,b4,b5, b6,b7,b8, tx,ty,tz)
///
/// Godot serializes the basis ROW-MAJOR (the nine numbers are rows[0][0..2], rows[1][0..2],
/// rows[2][0..2]) and applies a vector as world_i = rows[i] · local. The parser transposes those
/// numbers at parse time so that <see cref="Basis"/> here is stored COLUMN-MAJOR:
/// Basis[0..2] = col0 (local X axis in world space), Basis[3..5] = col1 (local Y axis),
/// Basis[6..8] = col2 (local Z axis). This column-major storage is what the application/composition
/// formulas below (and <see cref="Transform.Transform3DMath"/>) expect.
///
/// To transform a local point (lx, ly, lz) to world space:
///   wx = OriginX + Basis[0]*lx + Basis[3]*ly + Basis[6]*lz
///   wy = OriginY + Basis[1]*lx + Basis[4]*ly + Basis[7]*lz
///   wz = OriginZ + Basis[2]*lx + Basis[5]*ly + Basis[8]*lz
/// See <see cref="Transform.Transform3DMath"/> for correct composition helpers.
/// </summary>
public record Transform3DValue(
    double[] Basis,  // 9 elements, column-major: [col0.x, col0.y, col0.z, col1.x, col1.y, col1.z, col2.x, col2.y, col2.z]
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

/// <summary>PackedStringArray(...) - packed array of strings</summary>
public record PackedStringArrayValue(List<string> Values) : IGodotValue;


/// <summary>PackedVector3Array(...) - packed array of Vector3 values</summary>
public record PackedVector3ArrayValue(List<Vector3Value> Values) : IGodotValue;

/// <summary>Unknown typed value: TypeName(arg1, arg2, ...) - preserves type name for diagnostics</summary>
public record UnknownTypedValue(string TypeName, List<IGodotValue> Args) : IGodotValue;
