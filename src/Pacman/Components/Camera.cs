using Silk.NET.Maths;
using Silk.NET.Windowing;
using TrippyGL.Utils;

namespace Pacman.Components;

public sealed class Camera(IWindow window)
{
    public Vector3D<float> Position { get; set; } = new Vector3D<float>(0, 1, 10);

    public Vector3D<float> Front { get; private set; } = -Vector3D<float>.UnitZ;

    public Vector3D<float> Up { get; private set; } = Vector3D<float>.UnitY;

    public Vector3D<float> Right { get; private set; } = Vector3D<float>.UnitX;

    public float Pitch { get; set { field = float.Clamp(value, -1.55334303f, 1.55334303f); UpdateVectors(); } }

    public float Yaw { get; set { field = value; UpdateVectors(); } } = -MathF.PI / 2;

    public float FieldOfView { get; set => field = Math.Clamp(value, 1f, 90f); } = TrippyMath.PiOver2;
    public float AspectRatio { get; set; } = window.Size.X / (float)window.Size.Y;
    public float NearPlaneDistance { get; set; } = 0.01f;
    public float FarPlaneDistance { get; set; } = 100f;

    // Get the view matrix using the amazing LookAt function described more in depth on the web tutorials
    public Matrix4X4<float> ViewMatrix => Matrix4X4.CreateLookAt(Position, Position + Front, Up);

    // Get the projection matrix using the same method we have used up until this point
    public Matrix4X4<float> ProjectionMatrix => Matrix4X4.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlaneDistance, FarPlaneDistance);

    // This function is going to update the direction vertices using some of the math learned in the web tutorials.
    private void UpdateVectors()
    {
        Front = Vector3D.Normalize(new Vector3D<float>
        {
            X = MathF.Cos(Pitch) * MathF.Cos(Yaw),
            Y = MathF.Sin(Pitch),
            Z = MathF.Cos(Pitch) * MathF.Sin(Yaw)
        });
        Right = Vector3D.Normalize(Vector3D.Cross(Front, Vector3D<float>.UnitY));
        Up = Vector3D.Normalize(Vector3D.Cross(Right, Front));
    }
}
