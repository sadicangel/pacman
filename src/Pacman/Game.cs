using fennecs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pacman.Components;
using Pacman.Services;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using TrippyGL;
using Monitor = Silk.NET.Windowing.Monitor;

namespace Pacman;
public sealed class Game
{
    private readonly ServiceProvider _services;
    private readonly IWindow _window;

    private GraphicsDevice _graphicsDevice = null!;
    private SimpleShaderProgram _shader = null!;
    private InputManager _inputManager = null!;
    private Camera _camera = null!;
    private WorldManager _worldManager = null!;

    private Game()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        _services = new ServiceCollection()
            .AddLogging(builder => builder.AddSerilog())
            .AddSingleton(provider =>
            {
                var monitor = GetSecondMonitor();
                var windowSize = (monitor.VideoMode.Resolution ?? monitor.Bounds.Origin) * 2 / 3;
                var windowOpts = new WindowOptions()
                {
                    API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Debug, new APIVersion(3, 3)),
                    VSync = false,
                    UpdatesPerSecond = 60,
                    FramesPerSecond = 60,
                    Size = windowSize,
                    VideoMode = new VideoMode(windowSize),
                    PreferredDepthBufferBits = 24,
                    ShouldSwapAutomatically = true,
                    Title = "Pacman 3D",
                    Position = monitor.Bounds.Origin + new Vector2D<int>(50),
                    IsVisible = true
                };
                return Window.Create(windowOpts);

                static IMonitor GetSecondMonitor()
                {
                    var monitor = Monitor.GetMainMonitor(null);
                    monitor = Monitor.GetMonitors(null).FirstOrDefault(m => m.Index != monitor.Index) ?? monitor;
                    return monitor;
                }
            })
            .AddSingleton(provider => GL.GetApi(provider.GetRequiredService<IWindow>()))
            .AddSingleton(provider =>
            {
                var graphicsDevice = new GraphicsDevice(provider.GetRequiredService<GL>())
                {
                    DepthState = DepthState.Default,
                    BlendState = BlendState.Opaque,
                    ClearDepth = 1f,
                    ClearColor = Color4b.Teal
                };

                using var scope = provider.CreateScope();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<GraphicsDevice>>();

                graphicsDevice.DebugMessageReceived += (debugSource, debugType, messageId, debugSeverity, message) =>
                {
                    if (messageId != 131185 && messageId != 131186)
                        logger.LogInformation("OpenGL: source={debugSource} type={debugType} id={messageId} severity={debugSeverity} message=\"{message}\"", debugSource, debugType, messageId, debugSeverity, message);
                };
                graphicsDevice.ShaderCompiled += (GraphicsDevice sender, in ShaderProgramBuilder programBuilder, bool success) =>
                {
                    var hasVsLog = !string.IsNullOrEmpty(programBuilder.VertexShaderLog);
                    var hasGsLog = !string.IsNullOrEmpty(programBuilder.GeometryShaderLog);
                    var hasFsLog = !string.IsNullOrEmpty(programBuilder.FragmentShaderLog);
                    var hasProgramLog = !string.IsNullOrEmpty(programBuilder.ProgramLog);
                    var printLogs = hasVsLog || hasGsLog || hasFsLog || hasProgramLog;

                    if (!printLogs)
                    {
                        logger.LogInformation("Shader compiled successfully");
                        return;
                    }

                    var logLevel = success ? LogLevel.Warning : LogLevel.Error;

                    if (printLogs)
                    {
                        if (hasVsLog)
                            logger.Log(logLevel, "VertexShader: {VertexShaderLog}", programBuilder.VertexShaderLog);

                        if (hasGsLog)
                            logger.Log(logLevel, "GeometryShader: {GeometryShaderLog}", programBuilder.GeometryShaderLog);

                        if (hasFsLog)
                            logger.Log(logLevel, "FragmentShader: {FragmentShader}", programBuilder.FragmentShaderLog);

                        if (hasProgramLog)
                            logger.Log(logLevel, "ProgramLog: {ProgramLog}", programBuilder.ProgramLog);
                    }
                };

                return graphicsDevice;
            })
            .AddSingleton(provider => SimpleShaderProgram.Create<VertexNormalTexture>(provider.GetRequiredService<GraphicsDevice>()))
            .AddSingleton(provider =>
            {
                var inputContext = provider.GetRequiredService<IWindow>().CreateInput();

                using var scope = provider.CreateScope();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<IInputContext>>();

                foreach (var keyboard in inputContext.Keyboards)
                    OnInputContextConnectionChanged(keyboard, keyboard.IsConnected);
                foreach (var mouse in inputContext.Mice)
                    OnInputContextConnectionChanged(mouse, mouse.IsConnected);
                foreach (var gamepad in inputContext.Gamepads)
                    OnInputContextConnectionChanged(gamepad, gamepad.IsConnected);

                inputContext.ConnectionChanged += OnInputContextConnectionChanged;

                return inputContext;

                void OnInputContextConnectionChanged(IInputDevice device, bool isConnected)
                {
                    if (isConnected) logger.LogInformation("Connected {deviceName}", device.Name);
                    else logger.LogWarning("Disconnected {deviceName}", device.Name);
                }
            })
            .AddSingleton<InputMapping>()
            .AddSingleton<InputManager>()
            .AddSingleton<MeshFactory>()
            .AddSingleton<World>()
            .AddSingleton<WorldManager>()
            .BuildServiceProvider();

        _window = _services.GetRequiredService<IWindow>();
        _window.Load += Load;
        _window.Update += Update;
        _window.Render += Render;
        _window.Resize += Resize;
        _window.FramebufferResize += Resize;
        _window.Closing += Closing;
    }

    public static void Run() => new Game()._window.Run();

    private void Load()
    {
        _graphicsDevice = _services.GetRequiredService<GraphicsDevice>();
        _shader = _services.GetRequiredService<SimpleShaderProgram>();
        _inputManager = _services.GetRequiredService<InputManager>();
        _worldManager = _services.GetRequiredService<WorldManager>();

        _worldManager.SpawnCrates(new Vector3D<float>(-12, 0, 0));
        _worldManager.SpawnGhost("blinky", new Vector3D<float>(-8, 0, 0));
        _worldManager.SpawnGhost("pinky", new Vector3D<float>(-4, 0, 0));
        _worldManager.SpawnPacman(new Vector3D<float>(0, 0, 0));
        _worldManager.SpawnGhost("inky", new Vector3D<float>(4, 0, 0));
        _worldManager.SpawnGhost("clyde", new Vector3D<float>(8, 0, 0));
        _worldManager.SpawnCrates(new Vector3D<float>(12, 0, 0));

        _camera = new Camera(_window.Size.X / (float)_window.Size.Y);

        Resize(_window.Size);
    }

    private void Update(double deltaTime)
    {
        _inputManager.Update();
        _worldManager.World.Stream<Pacman.Components.Pacman, Transform>().For((_inputManager, deltaTime, _window.Time), static ((InputManager inputManager, double deltaTime, double elapsedTime) uniform, ref Pacman.Components.Pacman pacman, ref Transform transform) =>
        {
            const float Speed = 2.5f;
            var (inputManager, deltaTime, elapsedTime) = uniform;

            if (inputManager.Forward.IsPressed)
            {
                transform.Position += new Vector3D<float>(0, 0, -1) * (float)deltaTime * Speed;
            }
            if (inputManager.Backwards.IsPressed)
            {
                transform.Position += new Vector3D<float>(0, 0, 1) * (float)deltaTime * Speed;
            }
            if (inputManager.Up.IsPressed)
            {
                transform.Position += new Vector3D<float>(0, 1, 0) * (float)deltaTime * Speed;
            }
            if (inputManager.Down.IsPressed)
            {
                transform.Position += new Vector3D<float>(0, -1, 0) * (float)deltaTime * Speed;
            }
            if (inputManager.Left.IsPressed)
            {
                transform.Position += new Vector3D<float>(-1, 0, 0) * (float)deltaTime * Speed;
            }
            if (inputManager.Right.IsPressed)
            {
                transform.Position += new Vector3D<float>(1, 0, 0) * (float)deltaTime * Speed;
            }
        });

        _worldManager.World.Stream<Crate, Transform>().For(_window.Time, static (double elapsedTime, ref Crate crate, ref Transform transform) =>
        {
            transform.Rotation = Quaternion<float>.CreateFromAxisAngle(Vector3D<float>.UnitY, -(float)elapsedTime);
        });

        _worldManager.World.Stream<Ghost, Transform>().For(_window.Time, static (double elapsedTime, ref Ghost ghost, ref Transform transform) =>
        {
            transform.Position = transform.Position with
            {
                Y = 0.25f + 0.25f * MathF.Sin((float)elapsedTime)
            };
        });
    }

    private void Render(double deltaTime)
    {
        _graphicsDevice.Clear(ClearBuffers.Color | ClearBuffers.Depth);

        _shader.View = _camera.ViewMatrix;

        _worldManager.World.Stream<Transform, Mesh>().For(_shader, static (SimpleShaderProgram shader, ref Transform transform, ref Mesh mesh) =>
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

    private void Resize(Vector2D<int> size)
    {
        if (size.X == 0 || size.Y == 0)
            return;

        _graphicsDevice.SetViewport(0, 0, (uint)size.X, (uint)size.Y);

        _camera.AspectRatio = size.X / (float)size.Y;
        _shader.Projection = _camera.ProjectionMatrix;
    }

    private void Closing()
    {
        _shader.Dispose();
    }
}
