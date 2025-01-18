using fennecs;
using Pacman.Components;
using Silk.NET.Maths;
using TrippyGL;

namespace Pacman.Systems;
public sealed class RenderSystem(SimpleShaderProgram shader, World world, Camera camera)
{
    public void Render()
    {
        shader.GraphicsDevice.Clear(ClearBuffers.Color | ClearBuffers.Depth);
        shader.View = camera.ViewMatrix;

        world.Stream<Transform, Mesh>().For(shader, static (SimpleShaderProgram shader, ref Transform transform, ref Mesh mesh) =>
        {
            shader.GraphicsDevice.VertexArray = mesh.VertexArray;
            shader.Texture = mesh.Texture;
            shader.World = transform.World;
            if (mesh.VertexArray.IndexBuffer is not null)
                shader.GraphicsDevice.DrawElements(mesh.PrimitiveType, 0, mesh.StorageLength);
            else
                shader.GraphicsDevice.DrawArrays(mesh.PrimitiveType, 0, mesh.StorageLength);
        });
    }

    public void Resize(Vector2D<int> size)
    {
        if (size.X == 0 || size.Y == 0)
            return;

        shader.GraphicsDevice.SetViewport(0, 0, (uint)size.X, (uint)size.Y);

        camera.AspectRatio = size.X / (float)size.Y;
        shader.Projection = camera.ProjectionMatrix;
    }
}
