namespace GameBoy.Core;

/// <summary>
/// Picture Processing Unit (PPU) responsible for rendering frames.
/// </summary>
public sealed class Ppu
{
    public const int ScreenWidth = 160;
    public const int ScreenHeight = 144;

    private readonly InterruptController _interruptController;
    private int _cycleCounter = 0;
    private const int CyclesPerFrame = 70224; // Game Boy cycles per frame

    /// <summary>
    /// 32-bit RGBA frame buffer. Length = 160*144.
    /// </summary>
    public int[] FrameBuffer { get; } = new int[ScreenWidth * ScreenHeight];

    /// <summary>
    /// Initializes a new instance of the PPU.
    /// </summary>
    /// <param name="interruptController">The interrupt controller to request interrupts through.</param>
    public Ppu(InterruptController interruptController)
    {
        _interruptController = interruptController ?? throw new ArgumentNullException(nameof(interruptController));
    }

    /// <summary>
    /// Steps the PPU by the specified CPU cycles.
    /// </summary>
    public bool Step(int cycles)
    {
        _cycleCounter += cycles;
        
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

        // Request VBlank interrupt only when a full frame has completed
        if (_cycleCounter >= CyclesPerFrame)
        {
            _cycleCounter -= CyclesPerFrame;
            _interruptController.Request(InterruptType.VBlank);
            return true; // A frame is ready
        }

        return false; // Frame not ready yet
    }
}
