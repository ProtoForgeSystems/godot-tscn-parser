using ProtoForgeSystems.Godot.TscnParser.Models;

namespace ProtoForgeSystems.Godot.TscnParser.Transform;

/// <summary>
/// Column-major transform math for Godot .tscn Transform3D values.
/// Godot stores the basis as column vectors: Basis[0..2] = col0, Basis[3..5] = col1, Basis[6..8] = col2.
/// All methods here use this convention consistently.
/// </summary>
public static class Transform3DMath
{
    private static readonly double[] IdentityBasis = [1, 0, 0, 0, 1, 0, 0, 0, 1];

    /// <summary>The identity transform (no rotation, no translation).</summary>
    public static Transform3DValue Identity => new((double[])IdentityBasis.Clone(), 0, 0, 0);

    /// <summary>
    /// Apply a transform to a local point, returning the world-space position.
    /// </summary>
    public static (double X, double Y, double Z) Apply(Transform3DValue t, double lx, double ly, double lz)
    {
        double[] b = t.Basis;
        return (
            t.OriginX + b[0] * lx + b[3] * ly + b[6] * lz,
            t.OriginY + b[1] * lx + b[4] * ly + b[7] * lz,
            t.OriginZ + b[2] * lx + b[5] * ly + b[8] * lz
        );
    }

    /// <summary>
    /// Compose two transforms: parent followed by child (child origin is in parent's local space).
    /// </summary>
    public static Transform3DValue Compose(Transform3DValue parent, Transform3DValue child)
    {
        (double wx, double wy, double wz) = Apply(parent, child.OriginX, child.OriginY, child.OriginZ);
        return new Transform3DValue(MultiplyBasis(parent.Basis, child.Basis), wx, wy, wz);
    }

    /// <summary>
    /// Multiply two 3×3 bases stored column-major. Returns a new 9-element column-major array.
    /// </summary>
    public static double[] MultiplyBasis(double[] a, double[] b)
    {
        return
        [
            a[0]*b[0]+a[3]*b[1]+a[6]*b[2],  a[1]*b[0]+a[4]*b[1]+a[7]*b[2],  a[2]*b[0]+a[5]*b[1]+a[8]*b[2],
            a[0]*b[3]+a[3]*b[4]+a[6]*b[5],  a[1]*b[3]+a[4]*b[4]+a[7]*b[5],  a[2]*b[3]+a[5]*b[4]+a[8]*b[5],
            a[0]*b[6]+a[3]*b[7]+a[6]*b[8],  a[1]*b[6]+a[4]*b[7]+a[7]*b[8],  a[2]*b[6]+a[5]*b[7]+a[8]*b[8],
        ];
    }
}
