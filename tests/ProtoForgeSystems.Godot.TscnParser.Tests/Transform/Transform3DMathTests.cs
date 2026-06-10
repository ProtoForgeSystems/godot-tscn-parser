using ProtoForgeSystems.Godot.TscnParser.Extraction;
using ProtoForgeSystems.Godot.TscnParser.Models;
using ProtoForgeSystems.Godot.TscnParser.Transform;

namespace ProtoForgeSystems.Godot.TscnParser.Tests.Transform;

public class Transform3DMathTests
{
    private static Transform3DValue MakeTranslation(double tx, double ty, double tz) =>
        new([1, 0, 0, 0, 1, 0, 0, 0, 1], tx, ty, tz);

    // 90° CCW around Y from above: local X → world -Z, local Z → world +X
    // Column-major basis: col0=(0,0,-1), col1=(0,1,0), col2=(1,0,0)
    private static Transform3DValue MakeRotate90Y() =>
        new([0, 0, -1, 0, 1, 0, 1, 0, 0], 0, 0, 0);

    [Fact]
    public void Identity_HasZeroOriginAndIdentityBasis()
    {
        var id = Transform3DMath.Identity;
        Assert.Equal(0, id.OriginX);
        Assert.Equal(0, id.OriginY);
        Assert.Equal(0, id.OriginZ);
        Assert.Equal([1, 0, 0, 0, 1, 0, 0, 0, 1], id.Basis);
    }

    [Fact]
    public void Apply_Translation_ShiftsPoint()
    {
        var t = MakeTranslation(10, 0, 20);
        var (x, y, z) = Transform3DMath.Apply(t, 1, 0, 2);
        Assert.Equal(11, x, 6);
        Assert.Equal(0, y, 6);
        Assert.Equal(22, z, 6);
    }

    [Fact]
    public void Apply_Identity_ReturnsLocalPoint()
    {
        var (x, y, z) = Transform3DMath.Apply(Transform3DMath.Identity, 3, 4, 5);
        Assert.Equal(3, x, 6);
        Assert.Equal(4, y, 6);
        Assert.Equal(5, z, 6);
    }

    [Fact]
    public void Apply_Rotation90Y_CorrectlyRotatesLocalXToWorldNegZ()
    {
        // Local (1, 0, 0) with parent rotated 90° around Y → world (0, 0, -1)
        // col0=(0,0,-1) is where local X points in world space
        var t = MakeRotate90Y();
        var (x, y, z) = Transform3DMath.Apply(t, 1, 0, 0);
        Assert.Equal(0, x, 6);
        Assert.Equal(0, y, 6);
        Assert.Equal(-1, z, 6);
    }

    [Fact]
    public void Apply_Rotation90Y_CorrectlyRotatesLocalZToWorldPosX()
    {
        // Local (0, 0, 1) with parent rotated 90° around Y → world (1, 0, 0)
        // col2=(1,0,0) is where local Z points in world space
        var t = MakeRotate90Y();
        var (x, y, z) = Transform3DMath.Apply(t, 0, 0, 1);
        Assert.Equal(1, x, 6);
        Assert.Equal(0, y, 6);
        Assert.Equal(0, z, 6);
    }

    [Fact]
    public void Compose_TwoTranslations_SumsOrigins()
    {
        var parent = MakeTranslation(10, 0, 0);
        var child = MakeTranslation(2, 1, 3);
        var result = Transform3DMath.Compose(parent, child);
        Assert.Equal(12, result.OriginX, 6);
        Assert.Equal(1, result.OriginY, 6);
        Assert.Equal(3, result.OriginZ, 6);
    }

    [Fact]
    public void Compose_RotatedParentAndTranslatedChild_RotatesChildOrigin()
    {
        // Room rotated 90° around Y, door at local (0, 0, 3) inside room → world (3, 0, 0)
        // col2=(1,0,0): local Z maps to world +X, so lz=3 → wx=3
        var parent = MakeRotate90Y();
        var child = MakeTranslation(0, 0, 3);
        var result = Transform3DMath.Compose(parent, child);
        Assert.Equal(3, result.OriginX, 4);
        Assert.Equal(0, result.OriginY, 4);
        Assert.Equal(0, result.OriginZ, 4);
    }

    [Fact]
    public void Compose_IdentityParent_ReturnsChild()
    {
        var child = new Transform3DValue([0, 0, -1, 0, 1, 0, 1, 0, 0], 5, 2, 3);
        var result = Transform3DMath.Compose(Transform3DMath.Identity, child);
        Assert.Equal(5, result.OriginX, 6);
        Assert.Equal(2, result.OriginY, 6);
        Assert.Equal(3, result.OriginZ, 6);
        Assert.Equal(child.Basis, result.Basis);
    }

    [Fact]
    public void MultiplyBasis_IdentityLeft_ReturnsRight()
    {
        double[] identity = [1, 0, 0, 0, 1, 0, 0, 0, 1];
        double[] rotation = [0, 0, -1, 0, 1, 0, 1, 0, 0];
        var result = Transform3DMath.MultiplyBasis(identity, rotation);
        Assert.Equal(rotation, result);
    }

    [Fact]
    public void MultiplyBasis_RotationTwice90Y_GivesRotation180Y()
    {
        // Composing two 90° Y rotations → 180° Y rotation
        // 90°: col0=(0,0,-1), col1=(0,1,0), col2=(1,0,0)
        // 180°: col0=(-1,0,0), col1=(0,1,0), col2=(0,0,-1)
        double[] rot90 = [0, 0, -1, 0, 1, 0, 1, 0, 0];
        double[] result = Transform3DMath.MultiplyBasis(rot90, rot90);
        Assert.Equal(-1, result[0], 6);  // col0.x
        Assert.Equal(0, result[1], 6);   // col0.y
        Assert.Equal(0, result[2], 6);   // col0.z
        Assert.Equal(1, result[4], 6);   // col1.y
        Assert.Equal(-1, result[8], 6);  // col2.z
    }

    // ── Parser-boundary regression tests ────────────────────────────────────────
    // Godot serializes a Transform3D basis ROW-MAJOR and applies vectors as world_i = rows[i]·local.
    // The parser transposes that into column-major Transform3DValue.Basis so that Apply/Compose are
    // correct. These tests parse REAL Godot serialized transforms (text → Transform3DValue) and
    // verify the world-space result matches Godot's own runtime, catching the transpose regression
    // that silently corrupted rotated nested instances.

    private static Transform3DValue ParseTransform(string text) =>
        (Transform3DValue)TscnValueExtractor.ExtractValue(text);

    [Fact]
    public void Parse_Rotation90Y_AppliesLocalXToWorldNegZ_MatchingGodot()
    {
        // A real +90° Y rotation as Godot serializes it (row-major): rows = (0,0,1),(0,1,0),(-1,0,0).
        // Godot applies local (1,0,0) → world (rows[0]·x, rows[1]·x, rows[2]·x) = (0, 0, -1).
        var t = ParseTransform("Transform3D(0, 0, 1, 0, 1, 0, -1, 0, 0, 0, 0, 0)");
        var (x, y, z) = Transform3DMath.Apply(t, 1, 0, 0);
        Assert.Equal(0, x, 6);
        Assert.Equal(0, y, 6);
        Assert.Equal(-1, z, 6);
    }

    [Fact]
    public void Parse_NestedRotatedInstance_ComposesToGodotRuntimePosition()
    {
        // Regression for the CollapsedBunker door bug. SpawnRoom's BunkerDoorWall1 is rotated ~180°
        // about Y; the HermeticDoor sits at local (0.73, 0.022, -0.1) inside it. Godot places the
        // door node at world (2.6, 0.022, 0.73) at runtime. Before the parser transpose fix this
        // composed to the transposed-wrong (2.4, 0.022, -0.73).
        var wall = ParseTransform(
            "Transform3D(-4.371139e-08, 0, -1, 0, 1, 0, 1, 0, -4.371139e-08, 2.5, 0, 0)");
        var doorLocal = MakeTranslation(0.73, 0.022, -0.1);

        var world = Transform3DMath.Compose(wall, doorLocal);

        Assert.Equal(2.6, world.OriginX, 4);
        Assert.Equal(0.022, world.OriginY, 4);
        Assert.Equal(0.73, world.OriginZ, 4);
    }

    [Fact]
    public void Parse_IdentityBasis_IsTransposeInvariant()
    {
        // Translation-only content (identity basis) must be unaffected by the transpose.
        var t = ParseTransform("Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 5, 6, 7)");
        Assert.Equal([1, 0, 0, 0, 1, 0, 0, 0, 1], t.Basis);
        var (x, y, z) = Transform3DMath.Apply(t, 1, 2, 3);
        Assert.Equal(6, x, 6);
        Assert.Equal(8, y, 6);
        Assert.Equal(10, z, 6);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    public void MultiplyBasis_InvalidLengthA_ThrowsArgumentException(int length)
    {
        var a = new double[length];
        var b = new double[9];
        var ex = Assert.Throws<ArgumentException>(() => Transform3DMath.MultiplyBasis(a, b));
        Assert.Equal("a", ex.ParamName);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    public void MultiplyBasis_InvalidLengthB_ThrowsArgumentException(int length)
    {
        var a = new double[9];
        var b = new double[length];
        var ex = Assert.Throws<ArgumentException>(() => Transform3DMath.MultiplyBasis(a, b));
        Assert.Equal("b", ex.ParamName);
    }
}
