namespace GameBoy.Core;

/// <summary>
/// Represents the Game Boy joypad input state.
/// </summary>
public sealed class Joypad
{
    public bool Right { get; set; }
    public bool Left { get; set; }
    public bool Up { get; set; }
    public bool Down { get; set; }
    public bool A { get; set; }
    public bool B { get; set; }
    public bool Select { get; set; }
    public bool Start { get; set; }
}
