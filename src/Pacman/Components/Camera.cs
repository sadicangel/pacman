using Silk.NET.Maths;
using TrippyGL.Utils;

namespace Pacman.Components;

public sealed class Camera(float aspectRatio)
{
    // Those vectors are directions pointing outwards from the camera to define how it rotated.
    private Vector3D<float> _front = -Vector3D<float>.UnitZ;

    private Vector3D<float> _up = Vector3D<float>.UnitY;

    private Vector3D<float> _right = Vector3D<float>.UnitX;

    // Rotation around the X axis (radians)
    private float _pitch;

    // Rotation around the Y axis (radians)
    private float _yaw = 0;//-TrippyMath.PiOver2; // Without this, you would be started rotated 90 degrees right.

    // The field of view of the camera (radians)
    private float _fov = TrippyMath.PiOver2;

    private Transform _transform = Transform.Identity with { Position = new Vector3D<float>(0, 1, 10) };

    public ref Transform Transform => ref _transform;

    // This is simply the aspect ratio of the viewport, used for the projection matrix.
    public float AspectRatio { private get; set; } = aspectRatio;

    public Vector3D<float> Front => _front;

    public Vector3D<float> Up => _up;

    public Vector3D<float> Right => _right;

    // We convert from degrees to radians as soon as the property is set to improve performance.
    public float Pitch
    {
        get => Scalar.RadiansToDegrees(_pitch);
        set
        {
            // We clamp the pitch value between -89 and 89 to prevent the camera from going upside down, and a bunch
            // of weird "bugs" when you are using euler angles for rotation.
            // If you want to read more about this you can try researching a topic called gimbal lock
            var angle = Math.Clamp(value, -89f, 89f);
            _pitch = Scalar.DegreesToRadians(angle);
            UpdateVectors();
        }
    }

    // We convert from degrees to radians as soon as the property is set to improve performance.
    public float Yaw
    {
        get => Scalar.RadiansToDegrees(_yaw);
        set
        {
            _yaw = Scalar.DegreesToRadians(value);
            UpdateVectors();
        }
    }

    // The field of view (FOV) is the vertical angle of the camera view.
    // This has been discussed more in depth in a previous tutorial,
    // but in this tutorial, you have also learned how we can use this to simulate a zoom feature.
    // We convert from degrees to radians as soon as the property is set to improve performance.
    public float Fov
    {
        get => Scalar.RadiansToDegrees(_fov);
        set
        {
            var angle = Math.Clamp(value, 1f, 90f);
            _fov = Scalar.DegreesToRadians(angle);
        }
    }

    // Get the view matrix using the amazing LookAt function described more in depth on the web tutorials
    public Matrix4X4<float> ViewMatrix => Matrix4X4.CreateLookAt(Transform.Position, Transform.Position + _front, _up);

    // Get the projection matrix using the same method we have used up until this point
    public Matrix4X4<float> ProjectionMatrix => Matrix4X4.CreatePerspectiveFieldOfView(_fov, AspectRatio, 0.01f, 100f);

    // This function is going to update the direction vertices using some of the math learned in the web tutorials.
    private void UpdateVectors()
    {
        // First, the front matrix is calculated using some basic trigonometry.
        _front.X = MathF.Cos(_pitch) * MathF.Cos(_yaw);
        _front.Y = MathF.Sin(_pitch);
        _front.Z = MathF.Cos(_pitch) * MathF.Sin(_yaw);

        // We need to make sure the vectors are all normalized, as otherwise we would get some funky results.
        _front = Vector3D.Normalize(_front);

        // Calculate both the right and the up vector using cross product.
        // Note that we are calculating the right from the global up; this behaviour might
        // not be what you need for all cameras so keep this in mind if you do not want a FPS camera.
        _right = Vector3D.Normalize(Vector3D.Cross(_front, Vector3D<float>.UnitY));
        _up = Vector3D.Normalize(Vector3D.Cross(_right, _front));
    }
}
