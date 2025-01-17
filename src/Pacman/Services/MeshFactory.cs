using System.Globalization;
using Microsoft.Extensions.Logging;
using Pacman.Components;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TrippyGL;
using PrimitiveType = TrippyGL.PrimitiveType;

namespace Pacman.Services;

public sealed class MeshFactory(GraphicsDevice graphicsDevice, ILogger<MeshFactory> logger)
{
    private readonly Dictionary<string, Mesh> _meshes = [];

    private Texture2D LoadTexture(string textureName)
    {
        var path = Path.Combine("assets", "textures", $"{textureName}.png");
        using var image = Image.Load<Rgba32>(path);
        image.Mutate(img => img.Flip(FlipMode.Vertical));
        if (!image.DangerousTryGetSinglePixelMemory(out var pixels))
            throw new InvalidDataException("Failed to load image");
        var texture = new Texture2D(graphicsDevice, (uint)image.Width, (uint)image.Height);
        texture.SetData(pixels.Span, PixelFormat.Rgba);
        logger.LogInformation("Loaded {Path}", path);
        return texture;
    }

    private (PrimitiveType PrimitiveType, VertexBuffer<VertexNormalTexture> VertexBuffer) LoadObj(string objName, Quaternion<float>? rotation, Vector3D<float>? scale)
    {
        var path = Path.Combine("assets", "models", $"{objName}.obj");

        var vertices = new List<Vector3D<float>>();
        var normals = new List<Vector3D<float>>();
        var texCoords = new List<Vector2D<float>>();
        var faces = new List<(int v, int vt, int vn)>();

        foreach (var line in File.ReadLines(path))
        {
            switch (line)
            {
                case ['v', 'n', ..]:
                    normals.Add(ParseNormal(line, rotation));
                    break;
                case ['v', 't', ..]:
                    texCoords.Add(ParseTexCoord(line));
                    break;
                case ['v', ..]:
                    vertices.Add(ParseVertex(line, rotation, scale));
                    break;
                case ['f', ..]:
                    faces.AddRange(ParseFace(line));
                    break;
                default:
                    break;
            }
        }

        var vertexBuffer = new VertexBuffer<VertexNormalTexture>(
            graphicsDevice,
            faces.Select(f => new VertexNormalTexture(vertices[f.v], normals[f.vn], texCoords[f.vt])).ToArray(),
            BufferUsage.StaticCopy);

        return (PrimitiveType.Triangles, vertexBuffer);

        static Vector3D<float> ParseVertex(string line, Quaternion<float>? rotation, Vector3D<float>? scale)
        {
            var parts = line.Split(' ');
            var vertex = new Vector3D<float>(
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture),
                float.Parse(parts[3], CultureInfo.InvariantCulture)
            );
            if (rotation.HasValue)
                vertex = Vector3D.Transform(vertex, rotation.Value);
            if (scale.HasValue)
                vertex *= scale.Value;

            return vertex;
        }

        static Vector3D<float> ParseNormal(string line, Quaternion<float>? rotation) =>
            Vector3D.Normalize(ParseVertex(line, rotation, null));

        static Vector2D<float> ParseTexCoord(string line)
        {
            var parts = line.Split(' ');
            return new Vector2D<float>(
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture)
            );
        }

        static IEnumerable<(int v, int vt, int vn)> ParseFace(string line)
        {
            var parts = line.Split(' ');
            for (var i = 1; i < parts.Length; i++)
            {
                var faceParts = parts[i].Split('/');
                yield return (
                    int.Parse(faceParts[0], CultureInfo.InvariantCulture) - 1,
                    int.Parse(faceParts[1], CultureInfo.InvariantCulture) - 1,
                    int.Parse(faceParts[2], CultureInfo.InvariantCulture) - 1
                );
            }
        }
    }

    private (PrimitiveType PrimitiveType, VertexBuffer<VertexNormalTexture> VertexBuffer) CreateCube(float side)
    {
        var half = side * 0.5f;
        Span<VertexNormalTexture> bufferData = [
            new VertexNormalTexture(new Vector3D<float>(-half, -half, -half), new Vector3D<float>(0.0f, 0.0f, -side), new Vector2D<float>(0.0f, 0.0f)),
            new VertexNormalTexture(new Vector3D<float>(half, -half, -half), new Vector3D<float>(0.0f, 0.0f, -side), new Vector2D<float>(side, 0.0f)),
            new VertexNormalTexture(new Vector3D<float>(half, half, -half), new Vector3D<float>(0.0f, 0.0f, -side), new Vector2D<float>(side, side)),
            new VertexNormalTexture(new Vector3D<float>(half, half, -half), new Vector3D<float>(0.0f, 0.0f, -side), new Vector2D<float>(side, side)),
            new VertexNormalTexture(new Vector3D<float>(-half, half, -half), new Vector3D<float>(0.0f, 0.0f, -side), new Vector2D<float>(0.0f, side)),
            new VertexNormalTexture(new Vector3D<float>(-half, -half, -half), new Vector3D<float>(0.0f, 0.0f, -side), new Vector2D<float>(0.0f, 0.0f)),

            new VertexNormalTexture(new Vector3D<float>(-half, -half, half), new Vector3D<float>(0.0f, 0.0f, side), new Vector2D<float>(0.0f, 0.0f)),
            new VertexNormalTexture(new Vector3D<float>(half, -half, half), new Vector3D<float>(0.0f, 0.0f, side), new Vector2D<float>(side, 0.0f)),
            new VertexNormalTexture(new Vector3D<float>(half, half, half), new Vector3D<float>(0.0f, 0.0f, side), new Vector2D<float>(side, side)),
            new VertexNormalTexture(new Vector3D<float>(half, half, half), new Vector3D<float>(0.0f, 0.0f, side), new Vector2D<float>(side, side)),
            new VertexNormalTexture(new Vector3D<float>(-half, half, half), new Vector3D<float>(0.0f, 0.0f, side), new Vector2D<float>(0.0f, side)),
            new VertexNormalTexture(new Vector3D<float>(-half, -half, half), new Vector3D<float>(0.0f, 0.0f, side), new Vector2D<float>(0.0f, 0.0f)),

            new VertexNormalTexture(new Vector3D<float>(-half, half, half), new Vector3D<float>(-side, 0.0f, 0.0f), new Vector2D<float>(side, 0.0f)),
            new VertexNormalTexture(new Vector3D<float>(-half, half, -half), new Vector3D<float>(-side, 0.0f, 0.0f), new Vector2D<float>(side, side)),
            new VertexNormalTexture(new Vector3D<float>(-half, -half, -half), new Vector3D<float>(-side, 0.0f, 0.0f), new Vector2D<float>(0.0f, side)),
            new VertexNormalTexture(new Vector3D<float>(-half, -half, -half), new Vector3D<float>(-side, 0.0f, 0.0f), new Vector2D<float>(0.0f, side)),
            new VertexNormalTexture(new Vector3D<float>(-half, -half, half), new Vector3D<float>(-side, 0.0f, 0.0f), new Vector2D<float>(0.0f, 0.0f)),
            new VertexNormalTexture(new Vector3D<float>(-half, half, half), new Vector3D<float>(-side, 0.0f, 0.0f), new Vector2D<float>(side, 0.0f)),

            new VertexNormalTexture(new Vector3D<float>(half, half, half), new Vector3D<float>(side, 0.0f, 0.0f), new Vector2D<float>(side, 0.0f)),
            new VertexNormalTexture(new Vector3D<float>(half, half, -half), new Vector3D<float>(side, 0.0f, 0.0f), new Vector2D<float>(side, side)),
            new VertexNormalTexture(new Vector3D<float>(half, -half, -half), new Vector3D<float>(side, 0.0f, 0.0f), new Vector2D<float>(0.0f, side)),
            new VertexNormalTexture(new Vector3D<float>(half, -half, -half), new Vector3D<float>(side, 0.0f, 0.0f), new Vector2D<float>(0.0f, side)),
            new VertexNormalTexture(new Vector3D<float>(half, -half, half), new Vector3D<float>(side, 0.0f, 0.0f), new Vector2D<float>(0.0f, 0.0f)),
            new VertexNormalTexture(new Vector3D<float>(half, half, half), new Vector3D<float>(side, 0.0f, 0.0f), new Vector2D<float>(side, 0.0f)),

            new VertexNormalTexture(new Vector3D<float>(-half, -half, -half), new Vector3D<float>(0.0f, -side, 0.0f), new Vector2D<float>(0.0f, side)),
            new VertexNormalTexture(new Vector3D<float>(half, -half, -half), new Vector3D<float>(0.0f, -side, 0.0f), new Vector2D<float>(side, side)),
            new VertexNormalTexture(new Vector3D<float>(half, -half, half), new Vector3D<float>(0.0f, -side, 0.0f), new Vector2D<float>(side, 0.0f)),
            new VertexNormalTexture(new Vector3D<float>(half, -half, half), new Vector3D<float>(0.0f, -side, 0.0f), new Vector2D<float>(side, 0.0f)),
            new VertexNormalTexture(new Vector3D<float>(-half, -half, half), new Vector3D<float>(0.0f, -side, 0.0f), new Vector2D<float>(0.0f, 0.0f)),
            new VertexNormalTexture(new Vector3D<float>(-half, -half, -half), new Vector3D<float>(0.0f, -side, 0.0f), new Vector2D<float>(0.0f, side)),

            new VertexNormalTexture(new Vector3D<float>(-half, half, -half), new Vector3D<float>(0.0f, side, 0.0f), new Vector2D<float>(0.0f, side)),
            new VertexNormalTexture(new Vector3D<float>(half, half, -half), new Vector3D<float>(0.0f, side, 0.0f), new Vector2D<float>(side, side)),
            new VertexNormalTexture(new Vector3D<float>(half, half, half), new Vector3D<float>(0.0f, side, 0.0f), new Vector2D<float>(side, 0.0f)),
            new VertexNormalTexture(new Vector3D<float>(half, half, half), new Vector3D<float>(0.0f, side, 0.0f), new Vector2D<float>(side, 0.0f)),
            new VertexNormalTexture(new Vector3D<float>(-half, half, half), new Vector3D<float>(0.0f, side, 0.0f), new Vector2D<float>(0.0f, 0.0f)),
            new VertexNormalTexture(new Vector3D<float>(-half, half, -half), new Vector3D<float>(0.0f, side, 0.0f), new Vector2D<float>(0.0f, side))
        ];
        var vertexBuffer = new VertexBuffer<VertexNormalTexture>(graphicsDevice, bufferData, BufferUsage.StaticCopy);
        return (PrimitiveType.Triangles, vertexBuffer);
    }

    private (PrimitiveType PrimitiveType, VertexBuffer<VertexNormalTexture> VertexBuffer) CreateSphere(float radius)
    {
        const float StepS = 186 / 2048f;
        const float StepT = 322 / 1024f;

        var vertices = new List<VertexNormalTexture>(60);
        var indices = new List<uint>(60);

        Span<Vector3D<float>> tmpVertices = stackalloc Vector3D<float>[12];
        ComputeIcosahedronVertices(radius, tmpVertices);
        // Vertices
        VertexNormalTexture v0 = new(), v1 = new(), v2 = new(), v3 = new(), v4 = new(), v11 = new();
        uint index = 0;

        // compute and add 20 triangles of icosahedron first
        v0.Position = tmpVertices[0];   // 1st vertex
        v11.Position = tmpVertices[11]; // 12th vertex

        for (var i = 1; i <= 5; ++i, index += 12)
        {
            // 4 vertices in the 2nd row.
            v1.Position = tmpVertices[i];
            v2.Position = i < 5 ? tmpVertices[i + 1] : tmpVertices[1];
            v3.Position = tmpVertices[i + 5];
            v4.Position = i + 5 < 10 ? v4.Position = tmpVertices[i + 6] : tmpVertices[6];

            // Texture coords,
            v0.TexCoords = new Vector2D<float>((2 * i - 1) * StepS, 0);
            v1.TexCoords = new Vector2D<float>((2 * i - 2) * StepS, StepT);
            v2.TexCoords = new Vector2D<float>((2 * i - 0) * StepS, StepT);
            v3.TexCoords = new Vector2D<float>((2 * i - 1) * StepS, StepT * 2);
            v4.TexCoords = new Vector2D<float>((2 * i + 1) * StepS, StepT * 2);
            v11.TexCoords = new Vector2D<float>(2 * i * StepS, StepT * 3);

            // Add a triangle in 1st row.
            ComputeNormals(ref v0, ref v1, ref v2);
            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
            indices.Add(index);
            indices.Add(index + 1);
            indices.Add(index + 2);

            // Add 2 triangles in 2nd row.
            ComputeNormals(ref v1, ref v3, ref v2);
            vertices.Add(v1);
            vertices.Add(v3);
            vertices.Add(v2);
            indices.Add(index + 3);
            indices.Add(index + 4);
            indices.Add(index + 5);

            ComputeNormals(ref v2, ref v3, ref v4);
            vertices.Add(v2);
            vertices.Add(v3);
            vertices.Add(v4);
            indices.Add(index + 6);
            indices.Add(index + 7);
            indices.Add(index + 8);

            // Add a triangle in 3rd row.
            ComputeNormals(ref v3, ref v11, ref v4);
            vertices.Add(v3);
            vertices.Add(v11);
            vertices.Add(v4);
            indices.Add(index + 9);
            indices.Add(index + 10);
            indices.Add(index + 11);
        }

        // Subdivide icosahedron.
        SubdivideVertices(radius, subdivision: 3, vertices, indices);

        Span<VertexNormalTexture> bufferData = vertices.ToArray();
        Span<uint> indexData = indices.ToArray();

        foreach (ref var vertex in bufferData)
        {
            vertex.TexCoords.X = 0.5f * (1.0f + MathF.Atan2(vertex.Position.Z, vertex.Position.X) * (1 / MathF.PI));
            vertex.TexCoords.Y = MathF.Acos(vertex.Position.Y) * (1 / MathF.PI);
        }

        var vertexBuffer = new VertexBuffer<VertexNormalTexture>(graphicsDevice, (uint)bufferData.Length, (uint)indexData.Length, ElementType.UnsignedInt, BufferUsage.StaticCopy, bufferData);
        vertexBuffer.VertexArray.IndexBuffer!.SetData(indexData);

        return (PrimitiveType.Triangles, vertexBuffer);

        static void ComputeIcosahedronVertices(float radius, Span<Vector3D<float>> vertices)
        {
            if (vertices.Length != 12)
                throw new ArgumentException("Invalid number of vertices. Must be 12", nameof(vertices));

            var H_ANGLE = MathF.PI / 180 * 72;   // 72 degree = 360 / 5
            var V_ANGLE = MathF.Atan(1.0f / 2);  // elevation = 26.565 degree

            var hAngle1 = -MathF.PI / 2 - H_ANGLE / 2;  // start from -126 deg at 2nd row
            var hAngle2 = -MathF.PI / 2;                // start from -90 deg at 3rd row

            // the first top vertex (0, 0, r)
            vertices[0] = new Vector3D<float>(0, 0, radius);

            // 10 vertices at 2nd and 3rd rows
            for (var i = 1; i <= 5; ++i, hAngle1 += H_ANGLE, hAngle2 += H_ANGLE)
            {
                // elevation
                var z = radius * MathF.Sin(V_ANGLE);
                var xy = radius * MathF.Cos(V_ANGLE);

                vertices[i] = new Vector3D<float>(xy * MathF.Cos(hAngle1), xy * MathF.Sin(hAngle1), z);
                vertices[i + 5] = new Vector3D<float>(xy * MathF.Cos(hAngle2), xy * MathF.Sin(hAngle2), -z);
            }

            // the last bottom vertex (0, 0, -r)
            vertices[11] = new Vector3D<float>(0, 0, -radius);
        }

        static void SubdivideVertices(float radius, int subdivision, List<VertexNormalTexture> vertices, List<uint> indices)
        {
            List<VertexNormalTexture> tmpVertices;
            List<uint> tmpIndices;
            int indexCount;
            VertexNormalTexture v1, v2, v3;          // ptr to original vertices of a triangle
            VertexNormalTexture newV1, newV2, newV3; // new vertex positions
            uint index = 0;                          // new index value
            int i, j;

            // iteration
            for (i = 1; i <= subdivision; ++i)
            {
                // copy prev arrays
                tmpVertices = new(vertices);
                tmpIndices = new(indices);

                // clear prev arrays
                vertices.Clear();
                indices.Clear();

                index = 0;
                indexCount = tmpIndices.Count;
                for (j = 0; j < indexCount; j += 3)
                {
                    // get 3 vertice and texcoords of a triangle
                    v1 = tmpVertices[(int)tmpIndices[j]];
                    v2 = tmpVertices[(int)tmpIndices[j + 1]];
                    v3 = tmpVertices[(int)tmpIndices[j + 2]];

                    // get 3 new vertices by spliting half on each edge
                    newV1 = ComputeHalfVertex(ref v1, ref v2, radius);
                    newV2 = ComputeHalfVertex(ref v2, ref v3, radius);
                    newV3 = ComputeHalfVertex(ref v1, ref v3, radius);

                    // add 4 new triangles
                    ComputeNormals(ref v1, ref newV1, ref newV3);
                    vertices.Add(v1);
                    vertices.Add(newV1);
                    vertices.Add(newV3);
                    indices.Add(index);
                    indices.Add(index + 1);
                    indices.Add(index + 2);

                    ComputeNormals(ref newV1, ref v2, ref newV2);
                    vertices.Add(newV1);
                    vertices.Add(v2);
                    vertices.Add(newV2);
                    indices.Add(index + 3);
                    indices.Add(index + 4);
                    indices.Add(index + 5);

                    ComputeNormals(ref newV1, ref newV2, ref newV3);
                    vertices.Add(newV1);
                    vertices.Add(newV2);
                    vertices.Add(newV3);
                    indices.Add(index + 6);
                    indices.Add(index + 7);
                    indices.Add(index + 8);

                    ComputeNormals(ref newV3, ref newV2, ref v3);
                    vertices.Add(newV3);
                    vertices.Add(newV2);
                    vertices.Add(v3);
                    indices.Add(index + 9);
                    indices.Add(index + 10);
                    indices.Add(index + 11);

                    // next index
                    index += 12;
                }
            }
        }

        static VertexNormalTexture ComputeHalfVertex(ref VertexNormalTexture v1, ref VertexNormalTexture v2, float length)
        {
            var half = v1.Position + v2.Position;
            return new VertexNormalTexture()
            {
                Position = half * (length / half.Length),
                TexCoords = (v1.TexCoords + v2.TexCoords) / 2f
            };
        }
    }

    private static void ComputeNormals(ref VertexNormalTexture v1, ref VertexNormalTexture v2, ref VertexNormalTexture v3)
    {
        var e1 = v2.Position - v1.Position;
        var e2 = v3.Position - v1.Position;

        var c = Vector3D.Cross(e1, e2);
        var n = c.LengthSquared > float.Epsilon ? Vector3D.Normalize(c) : Vector3D<float>.Zero;
        v1.Normal = n;
        v2.Normal = n;
        v3.Normal = n;
    }

    public Mesh LoadCrate()
    {
        const string Key = "crate";

        if (!_meshes.TryGetValue(Key, out var mesh))
        {
            var (primitiveType, vertexBuffer) = CreateCube(1);
            var texture = LoadTexture(Key);
            _meshes[Key] = mesh = new Mesh(primitiveType, vertexBuffer, texture);
        }
        return mesh;
    }

    public Mesh LoadModel(string modelName, Quaternion<float>? rotation = null, Vector3D<float>? scale = null)
    {
        if (!_meshes.TryGetValue(modelName, out var mesh))
        {
            var (primitiveType, vertexBuffer) = LoadObj(modelName, rotation, scale);
            _meshes[modelName] = mesh = new Mesh(primitiveType, vertexBuffer, LoadTexture(modelName));
        }
        return mesh;
    }
}
