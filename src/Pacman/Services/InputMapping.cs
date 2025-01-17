using Silk.NET.Input;

namespace Pacman.Services;

public sealed class InputMapping
{
    public Key Forward { get; set; } = Key.W;
    public Key Backwards { get; set; } = Key.S;
    public Key Up { get; set; } = Key.E;
    public Key Down { get; set; } = Key.Q;
    public Key Left { get; set; } = Key.A;
    public Key Right { get; set; } = Key.D;

    public Key Confirm { get; set; } = Key.Enter;
    public Key Cancel { get; set; } = Key.Escape;
}
