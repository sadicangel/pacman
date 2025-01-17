using Silk.NET.Input;

namespace Pacman.Services;
public sealed class InputManager(IInputContext inputContext, InputMapping inputMapping)
{
    public KeyState Forward { get; private set; }
    public KeyState Backwards { get; private set; }
    public KeyState Up { get; private set; }
    public KeyState Down { get; private set; }
    public KeyState Left { get; private set; }
    public KeyState Right { get; private set; }
    public KeyState Confirm { get; private set; }
    public KeyState Cancel { get; private set; }

    public void Update()
    {
        Forward = Forward.Update(inputContext.Keyboards.Any(k => k.IsKeyPressed(inputMapping.Forward)));
        Backwards = Backwards.Update(inputContext.Keyboards.Any(k => k.IsKeyPressed(inputMapping.Backwards)));
        Up = Up.Update(inputContext.Keyboards.Any(k => k.IsKeyPressed(inputMapping.Up)));
        Down = Down.Update(inputContext.Keyboards.Any(k => k.IsKeyPressed(inputMapping.Down)));
        Left = Left.Update(inputContext.Keyboards.Any(k => k.IsKeyPressed(inputMapping.Left)));
        Right = Right.Update(inputContext.Keyboards.Any(k => k.IsKeyPressed(inputMapping.Right)));
        Confirm = Confirm.Update(inputContext.Keyboards.Any(k => k.IsKeyPressed(inputMapping.Confirm)));
        Cancel = Cancel.Update(inputContext.Keyboards.Any(k => k.IsKeyPressed(inputMapping.Cancel)));
    }
}

public readonly record struct KeyState(KeyState.KeyStateFlags Flags = default)
{
    private KeyStateFlags Flags { get; init; } = Flags;

    public bool IsReleased => (Flags & KeyStateFlags.Up) != 0;
    public bool IsPressed => (Flags & KeyStateFlags.Down) != 0;
    public bool WasReleased => (Flags & KeyStateFlags.Released) != 0;
    public bool WasPressed => (Flags & KeyStateFlags.Pressed) != 0;

    public enum KeyStateFlags
    {
        Up = 0,
        Down = 1,
        Released = 2,
        Pressed = 4,
    }

    public KeyState Update(bool isDown)
    {
        var flags = isDown ? KeyStateFlags.Down : KeyStateFlags.Up;
        var wasPressed = !IsPressed && isDown;
        if (wasPressed)
            flags |= KeyStateFlags.Pressed;
        var wasReleased = IsPressed && !isDown;
        if (wasReleased)
            flags |= KeyStateFlags.Released;
        return new KeyState(flags);
    }
}
