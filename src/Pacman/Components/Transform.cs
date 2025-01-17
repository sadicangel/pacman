using Silk.NET.Maths;

namespace Pacman.Components;

public record struct Transform(Vector3D<float> Position, Vector3D<float> Scale, Quaternion<float> Rotation)
{
    public static Transform Identity { get; } = new Transform(Vector3D<float>.Zero, Vector3D<float>.One, Quaternion<float>.Identity);

    public readonly Matrix4X4<float> World => Matrix4X4.Transform(Matrix4X4<float>.Identity, Rotation) * Matrix4X4.CreateScale(Scale) * Matrix4X4.CreateTranslation(Position);

    public readonly Vector3D<float> Right => Vector3D.Normalize(Vector3D.Transform(Vector3D<float>.UnitX, Rotation));

    public readonly Vector3D<float> Up => Vector3D.Normalize(Vector3D.Transform(Vector3D<float>.UnitY, Rotation));

    public readonly Vector3D<float> Forward => Vector3D.Normalize(Vector3D.Transform(Vector3D<float>.UnitZ, Rotation));
}

