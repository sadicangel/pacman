using fennecs;
using Microsoft.Extensions.Logging;
using Pacman.Components;
using Silk.NET.Maths;

namespace Pacman.Services;
public sealed class WorldManager(World world, MeshFactory meshFactory, ILogger<WorldManager> logger)
{
    public World World => world;

    public void SpawnCrates(params ReadOnlySpan<Vector3D<float>> positions)
    {
        var crateSpawner = world.Entity()
            .Add<Crate>()
            .Add(Transform.Identity)
            .Add(meshFactory.LoadCrate());

        foreach (var position in positions)
        {
            crateSpawner.Add(Transform.Identity with { Position = position });
            crateSpawner.Spawn();
            logger.LogInformation("Spawned {T} at {Position}", "Crate", position);
        }
    }

    public void SpawnGhost(string name, Vector3D<float> position)
    {
        world.Entity()
            .Add<Ghost>()
            .Add(Transform.Identity with { Position = position })
            .Add(meshFactory.LoadModel(
                name,
                Quaternion<float>.CreateFromYawPitchRoll(-MathF.PI / 2, -MathF.PI / 2, 0),
                new Vector3D<float>(0.35f)))
            .Spawn();

        logger.LogInformation("Spawned {T} at {Position}", name, position);
    }

    public void SpawnPacman(Vector3D<float> position)
    {
        world.Entity()
            .Add<Pacman.Components.Pacman>()
            .Add(Transform.Identity with { Position = position })
            .Add(meshFactory.LoadModel(
                "pacman",
                Quaternion<float>.CreateFromYawPitchRoll(-MathF.PI / 2, -MathF.PI / 2, 0),
                new Vector3D<float>(0.35f)))
            .Spawn();

        logger.LogInformation("Spawned {T} at {Position}", "pacman", position);
    }
}
