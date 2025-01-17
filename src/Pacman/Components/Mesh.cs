using TrippyGL;

namespace Pacman.Components;

public readonly record struct Mesh(
    PrimitiveType PrimitiveType,
    uint StorageLength,
    VertexArray VertexArray,
    Texture2D Texture) : IDisposable
{
    public Mesh(PrimitiveType primitiveType, VertexBuffer<VertexNormalTexture> vertexBuffer, Texture2D texture)
        : this(primitiveType, vertexBuffer.IndexSubset?.StorageLength ?? vertexBuffer.StorageLength, vertexBuffer, texture)
    {

    }

    public void Dispose()
    {
        VertexArray.Dispose();
        Texture.Dispose();
    }
}
