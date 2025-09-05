namespace GameBoy.Core;

/// <summary>
/// Picture Processing Unit (PPU) responsible for rendering frames.
/// </summary>
public sealed class Ppu
{
    public const int ScreenWidth = 160;
    public const int ScreenHeight = 144;

    /// <summary>
    /// 32-bit RGBA frame buffer. Length = 160*144.
    /// </summary>
    public int[] FrameBuffer { get; } = new int[ScreenWidth * ScreenHeight];

    /// <summary>
    /// Steps the PPU by the specified CPU cycles.
    /// </summary>
    public bool Step(int cycles)
    {
        // Placeholder: produce a simple pattern to prove rendering.
        for (int y = 0; y < ScreenHeight; y++)
        {
            for (int x = 0; x < ScreenWidth; x++)
            {
                int idx = y * ScreenWidth + x;
                byte shade = (byte)((x ^ y) & 0xFF);
                FrameBuffer[idx] = (255 << 24) | (shade << 16) | (shade << 8) | shade;
            }
        }
        return true; // A frame is ready every call for placeholder
    }
}
