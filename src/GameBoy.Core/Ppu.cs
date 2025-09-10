namespace GameBoy.Core;

/// <summary>
/// LCD modes for the PPU state machine.
/// </summary>
public enum LcdMode : byte
{
    /// <summary>Mode 0: H-Blank (204 cycles)</summary>
    HBlank = 0,
    /// <summary>Mode 1: V-Blank (4560 cycles, 10 scanlines)</summary>
    VBlank = 1,
    /// <summary>Mode 2: OAM Scan (80 cycles)</summary>
    OamScan = 2,
    /// <summary>Mode 3: Drawing (172-289 cycles, variable)</summary>
    Drawing = 3
}

/// <summary>
/// Picture Processing Unit (PPU) responsible for rendering frames.
/// Implements complete LCD mode state machine and graphics pipeline.
/// </summary>
public sealed class Ppu
{
    public const int ScreenWidth = 160;
    public const int ScreenHeight = 144;

    // LCD timing constants
    private const int OamScanCycles = 80;
    private const int DrawingCycles = 172; // Minimum cycles for mode 3
    private const int HBlankCycles = 204;
    private const int ScanlineCycles = OamScanCycles + DrawingCycles + HBlankCycles; // 456 cycles
    private const int VBlankScanlines = 10;
    private const int TotalScanlines = 154; // 144 visible + 10 VBlank
    private const int CyclesPerFrame = TotalScanlines * ScanlineCycles; // 70224 cycles

    private readonly InterruptController _interruptController;

    // PPU state
    private int _cycleCounter = 0;
    private int _scanlineCycles = 0;
    private LcdMode _mode = LcdMode.OamScan;
    private byte _ly = 0;
    private byte _stat = 0x85; // Post-BIOS default

    // MMU reference for VRAM/OAM/register access (set via Mmu property)

    // Cached register values
    private Lcdc _lcdc = new(0x91); // Post-BIOS default
    private Palette _bgp = new(0xFC); // Post-BIOS default
    private Palette _obp0 = new(0x00);
    private Palette _obp1 = new(0x00);
    private byte _scy = 0x00;
    private byte _scx = 0x00;
    private byte _wy = 0x00;
    private byte _wx = 0x00;
    private byte _lyc = 0x00;

    /// <summary>
    /// 32-bit RGBA frame buffer. Length = 160*144.
    /// </summary>
    public int[] FrameBuffer { get; } = new int[ScreenWidth * ScreenHeight];

    /// <summary>
    /// Optimized frame buffer stored as RGBA bytes for efficient JavaScript interop.
    /// Layout: [R0, G0, B0, A0, R1, G1, B1, A1, ...]. Length = 160*144*4.
    /// </summary>
    public byte[] FrameBufferRgba { get; } = new byte[ScreenWidth * ScreenHeight * 4];

    /// <summary>
    /// Writes a pixel to both frame buffers efficiently.
    /// </summary>
    /// <param name="bufferIndex">Index in the frame buffer (0 to ScreenWidth*ScreenHeight-1)</param>
    /// <param name="argbColor">ARGB color value</param>
    private void WritePixel(int bufferIndex, int argbColor)
    {
        // Write to ARGB frame buffer (backward compatibility)
        FrameBuffer[bufferIndex] = argbColor;

        // Write to RGBA byte buffer (optimized for JavaScript)
        int rgbaIndex = bufferIndex * 4;
        FrameBufferRgba[rgbaIndex] = (byte)((argbColor >> 16) & 0xFF);     // R
        FrameBufferRgba[rgbaIndex + 1] = (byte)((argbColor >> 8) & 0xFF);  // G
        FrameBufferRgba[rgbaIndex + 2] = (byte)(argbColor & 0xFF);         // B
        FrameBufferRgba[rgbaIndex + 3] = (byte)((argbColor >> 24) & 0xFF); // A
    }

    /// <summary>
    /// Gets the current LCD mode.
    /// </summary>
    public LcdMode Mode => _mode;

    /// <summary>
    /// Gets the current scanline (LY register).
    /// </summary>
    public byte LY => _ly;

    /// <summary>
    /// Gets the STAT register value.
    /// </summary>
    public byte STAT => _stat;

    /// <summary>
    /// Sets the MMU reference for register and memory access.
    /// </summary>
    public Mmu? Mmu { get; set; }

    /// <summary>
    /// Initializes a new instance of the PPU.
    /// </summary>
    /// <param name="interruptController">The interrupt controller to request interrupts through.</param>
    public Ppu(InterruptController interruptController)
    {
        _interruptController = interruptController ?? throw new ArgumentNullException(nameof(interruptController));

        // Initialize to post-BIOS defaults
        Reset();
    }

    /// <summary>
    /// Resets the PPU to post-BIOS state.
    /// </summary>
    public void Reset()
    {
        _cycleCounter = 0;
        _scanlineCycles = 0;
        _mode = LcdMode.OamScan;
        _ly = 0;
        _stat = 0x85;

        // Reset register cache to post-BIOS defaults
        _lcdc = new(0x91);
        _bgp = new(0xFC);
        _obp0 = new(0x00);
        _obp1 = new(0x00);
        _scy = 0x00;
        _scx = 0x00;
        _wy = 0x00;
        _wx = 0x00;
        _lyc = 0x00;

        // Clear frame buffer with default background color (lightest green)
        int defaultColor = Palette.ToRgba(0); // Lightest green with full opacity
        Array.Fill(FrameBuffer, defaultColor);

        // Clear RGBA buffer efficiently
        byte r = (byte)((defaultColor >> 16) & 0xFF);
        byte g = (byte)((defaultColor >> 8) & 0xFF);
        byte b = (byte)(defaultColor & 0xFF);
        byte a = (byte)((defaultColor >> 24) & 0xFF);

        for (int i = 0; i < FrameBufferRgba.Length; i += 4)
        {
            FrameBufferRgba[i] = r;
            FrameBufferRgba[i + 1] = g;
            FrameBufferRgba[i + 2] = b;
            FrameBufferRgba[i + 3] = a;
        }
    }

    /// <summary>
    /// Steps the PPU by the specified CPU cycles.
    /// </summary>
    public bool Step(int cycles)
    {
        // Update register cache from MMU if available
        RefreshRegisterCache();

        if (!_lcdc.LcdEnable)
        {
            // LCD is disabled - just return without processing
            return false;
        }

        int remainingCycles = cycles;
        bool frameCompleted = false;

        while (remainingCycles > 0)
        {
            int cyclesToNextTransition = GetCyclesToNextModeTransition();
            int cyclesToProcess = Math.Min(remainingCycles, cyclesToNextTransition);

            _cycleCounter += cyclesToProcess;
            _scanlineCycles += cyclesToProcess;
            remainingCycles -= cyclesToProcess;

            // Check if we've reached a mode transition
            switch (_mode)
            {
                case LcdMode.OamScan:
                    if (_scanlineCycles >= OamScanCycles)
                    {
                        _mode = LcdMode.Drawing;
                        UpdateStatRegister();
                    }
                    break;

                case LcdMode.Drawing:
                    if (_scanlineCycles >= OamScanCycles + DrawingCycles)
                    {
                        // Render the current scanline
                        RenderScanline(_ly);

                        _mode = LcdMode.HBlank;
                        UpdateStatRegister();
                        CheckStatInterrupt();
                    }
                    break;

                case LcdMode.HBlank:
                    if (_scanlineCycles >= ScanlineCycles)
                    {
                        _scanlineCycles = 0;
                        _ly++;

                        // Write LY back to MMU so ROMs can read current scanline
                        if (Mmu != null)
                        {
                            Mmu.SetLY(_ly);
                        }

                        if (_ly >= ScreenHeight)
                        {
                            // Enter VBlank
                            _mode = LcdMode.VBlank;
                            _interruptController.Request(InterruptType.VBlank);
                            frameCompleted = true;
                        }
                        else
                        {
                            // Next scanline
                            _mode = LcdMode.OamScan;
                        }

                        UpdateStatRegister();
                        CheckStatInterrupt();
                    }
                    break;

                case LcdMode.VBlank:
                    if (_scanlineCycles >= ScanlineCycles)
                    {
                        _scanlineCycles = 0;
                        _ly++;

                        // Write LY back to MMU so ROMs can read current scanline
                        if (Mmu != null)
                        {
                            Mmu.SetLY(_ly);
                        }

                        if (_ly >= TotalScanlines)
                        {
                            // Frame complete - restart at scanline 0
                            _ly = 0;
                            _mode = LcdMode.OamScan;
                            frameCompleted = true;

                            // Write LY reset back to MMU
                            if (Mmu != null)
                            {
                                Mmu.SetLY(_ly);
                            }
                        }

                        UpdateStatRegister();
                        CheckStatInterrupt();
                    }
                    break;
            }
        }

        return frameCompleted;
    }

    /// <summary>
    /// Gets the number of cycles until the next mode transition.
    /// </summary>
    private int GetCyclesToNextModeTransition()
    {
        return _mode switch
        {
            LcdMode.OamScan => OamScanCycles - _scanlineCycles,
            LcdMode.Drawing => (OamScanCycles + DrawingCycles) - _scanlineCycles,
            LcdMode.HBlank => ScanlineCycles - _scanlineCycles,
            LcdMode.VBlank => ScanlineCycles - _scanlineCycles,
            _ => 1
        };
    }

    /// <summary>
    /// Renders a single scanline to the frame buffer.
    /// </summary>
    private void RenderScanline(int scanline)
    {
        if (scanline >= ScreenHeight)
            return;

        // Render background if enabled
        if (_lcdc.BgWindowEnable)
        {
            RenderBackgroundScanline(scanline);
        }
        else
        {
            // Clear scanline to white
            ClearScanline(scanline, Palette.ToRgba(0));
        }

        // Render window if enabled
        if (_lcdc.WindowEnable && _lcdc.BgWindowEnable)
        {
            RenderWindowScanline(scanline);
        }

        // Render sprites if enabled
        if (_lcdc.ObjEnable)
        {
            RenderSpriteScanline(scanline);
        }
    }

    /// <summary>
    /// Renders the background layer for a scanline.
    /// </summary>
    private void RenderBackgroundScanline(int scanline)
    {
        int y = (scanline + _scy) & 0xFF; // Wrap around 256 pixel boundary
        int tileY = y / 8;
        int pixelY = y % 8;

        for (int x = 0; x < ScreenWidth; x++)
        {
            int scrolledX = (x + _scx) & 0xFF; // Wrap around 256 pixel boundary
            int tileX = scrolledX / 8;
            int pixelX = scrolledX % 8;

            // Get tile index from tile map
            byte tileIndex = GetBackgroundTileIndex(tileX, tileY);

            // Get pixel color from tile data
            int colorIndex = GetTilePixel(tileIndex, pixelX, pixelY);
            int color = _bgp.GetRgbaColor(colorIndex);

            // Write to frame buffer
            int bufferIndex = scanline * ScreenWidth + x;
            WritePixel(bufferIndex, color);
        }
    }

    /// <summary>
    /// Renders the window layer for a scanline.
    /// </summary>
    private void RenderWindowScanline(int scanline)
    {
        // Window is only visible if WY <= scanline and WX <= 166
        if (_wy > scanline || _wx >= 167)
            return;

        int windowY = scanline - _wy;
        int tileY = windowY / 8;
        int pixelY = windowY % 8;

        for (int x = 0; x < ScreenWidth; x++)
        {
            int windowX = x - (_wx - 7);
            if (windowX < 0)
                continue;

            int tileX = windowX / 8;
            int pixelX = windowX % 8;

            // Get tile index from window tile map
            byte tileIndex = GetWindowTileIndex(tileX, tileY);

            // Get pixel color from tile data
            int colorIndex = GetTilePixel(tileIndex, pixelX, pixelY);
            int color = _bgp.GetRgbaColor(colorIndex);

            // Write to frame buffer
            int bufferIndex = scanline * ScreenWidth + x;
            WritePixel(bufferIndex, color);
        }
    }

    /// <summary>
    /// Renders sprites for a scanline.
    /// </summary>
    private void RenderSpriteScanline(int scanline)
    {
        if (Mmu == null)
            return;

        // Find up to 10 sprites that overlap this scanline
        var sprites = new List<Sprite>();

        for (int i = 0; i < 40 && sprites.Count < 10; i++)
        {
            int oamAddr = 0xFE00 + (i * 4);
            var sprite = new Sprite(
                Mmu.ReadByte((ushort)oamAddr),
                Mmu.ReadByte((ushort)(oamAddr + 1)),
                Mmu.ReadByte((ushort)(oamAddr + 2)),
                Mmu.ReadByte((ushort)(oamAddr + 3))
            );

            if (sprite.IsVisibleOnScanline(scanline, _lcdc.SpriteHeight))
            {
                sprites.Add(sprite);
            }
        }

        // Render sprites in reverse order (lower index = higher priority)
        for (int i = sprites.Count - 1; i >= 0; i--)
        {
            RenderSprite(sprites[i], scanline);
        }
    }

    /// <summary>
    /// Renders a single sprite on a scanline.
    /// </summary>
    private void RenderSprite(Sprite sprite, int scanline)
    {
        if (Mmu == null)
            return;

        int spriteY = _lcdc.SpriteHeight == 16 ? sprite.GetRelativeY16(scanline) : sprite.GetRelativeY(scanline);
        if (spriteY < 0 || spriteY >= _lcdc.SpriteHeight)
            return;

        // Determine which tile to use for 8x16 sprites
        byte tileIndex = _lcdc.SpriteHeight == 16 && spriteY >= 8
            ? sprite.GetBottomTileIndex()
            : sprite.GetTopTileIndex(_lcdc.SpriteHeight == 16);

        int tileY = _lcdc.SpriteHeight == 16 ? spriteY % 8 : spriteY;

        for (int x = 0; x < 8; x++)
        {
            int screenX = sprite.ScreenX + x;
            if (screenX < 0 || screenX >= ScreenWidth)
                continue;

            int pixelX = sprite.FlipX ? (7 - x) : x;
            int colorIndex = GetTilePixel(tileIndex, pixelX, tileY);

            // Color 0 is transparent for sprites
            if (colorIndex == 0)
                continue;

            Palette spritePalette = sprite.UseObp1 ? _obp1 : _obp0;
            int color = spritePalette.GetRgbaColor(colorIndex);

            // Check sprite priority vs background
            int bufferIndex = scanline * ScreenWidth + screenX;
            if (!sprite.BehindBackground || IsBackgroundColorZero(bufferIndex))
            {
                WritePixel(bufferIndex, color);
            }
        }
    }

    /// <summary>
    /// Gets a tile index from the background tile map.
    /// </summary>
    private byte GetBackgroundTileIndex(int tileX, int tileY)
    {
        if (Mmu == null)
            return 0;

        int tileMapAddr = _lcdc.BgTileMapBase + (tileY * 32) + tileX;
        return Mmu.ReadByte((ushort)tileMapAddr);
    }

    /// <summary>
    /// Gets a tile index from the window tile map.
    /// </summary>
    private byte GetWindowTileIndex(int tileX, int tileY)
    {
        if (Mmu == null)
            return 0;

        int tileMapAddr = _lcdc.WindowTileMapBase + (tileY * 32) + tileX;
        return Mmu.ReadByte((ushort)tileMapAddr);
    }

    /// <summary>
    /// Gets a pixel from tile data.
    /// </summary>
    private int GetTilePixel(byte tileIndex, int x, int y)
    {
        if (Mmu == null)
            return 0;

        // Calculate tile data address
        int tileDataAddr;
        if (_lcdc.UsesSignedTileIndexing)
        {
            // 0x8800-0x97FF range with signed indexing
            sbyte signedIndex = (sbyte)tileIndex;
            tileDataAddr = 0x9000 + (signedIndex * 16);
        }
        else
        {
            // 0x8000-0x8FFF range with unsigned indexing
            tileDataAddr = 0x8000 + (tileIndex * 16);
        }

        // Each tile is 16 bytes (8x8 pixels, 2 bits per pixel)
        // Each row is 2 bytes (low bit plane + high bit plane)
        int rowAddr = tileDataAddr + (y * 2);
        byte lowBits = Mmu.ReadByte((ushort)rowAddr);
        byte highBits = Mmu.ReadByte((ushort)(rowAddr + 1));

        // Extract 2-bit color for this pixel
        int bitPos = 7 - x;
        int lowBit = (lowBits >> bitPos) & 1;
        int highBit = (highBits >> bitPos) & 1;

        return (highBit << 1) | lowBit;
    }

    /// <summary>
    /// Clears a scanline to a solid color.
    /// </summary>
    private void ClearScanline(int scanline, int color)
    {
        int startIndex = scanline * ScreenWidth;
        for (int i = 0; i < ScreenWidth; i++)
        {
            WritePixel(startIndex + i, color);
        }
    }

    /// <summary>
    /// Checks if the background color at a frame buffer position is color 0.
    /// </summary>
    private bool IsBackgroundColorZero(int bufferIndex)
    {
        // This is a simplified check - in a full implementation, we'd track
        // the original color indices instead of just the final RGBA values
        int color = FrameBuffer[bufferIndex];
        return color == Palette.ToRgba(0);
    }

    /// <summary>
    /// Refreshes register cache from MMU.
    /// </summary>
    private void RefreshRegisterCache()
    {
        if (Mmu == null)
            return;

        _lcdc = new(Mmu.ReadByte(IoRegs.LCDC));
        _bgp = new(Mmu.ReadByte(IoRegs.BGP));
        _obp0 = new(Mmu.ReadByte(IoRegs.OBP0));
        _obp1 = new(Mmu.ReadByte(IoRegs.OBP1));
        _scy = Mmu.ReadByte(IoRegs.SCY);
        _scx = Mmu.ReadByte(IoRegs.SCX);
        _wy = Mmu.ReadByte(IoRegs.WY);
        _wx = Mmu.ReadByte(IoRegs.WX);
        _lyc = Mmu.ReadByte(IoRegs.LYC);
    }

    /// <summary>
    /// Updates the STAT register with current mode and LY=LYC comparison.
    /// </summary>
    private void UpdateStatRegister()
    {
        // Preserve upper 4 bits (interrupt enable flags)
        _stat = (byte)((_stat & 0xF8) | (byte)_mode);

        // Set LY=LYC flag (bit 2)
        if (_ly == _lyc)
        {
            _stat |= 0x04;
        }
        else
        {
            _stat &= 0xFB;
        }

        // Write STAT back to MMU so ROMs can read current status
        if (Mmu != null)
        {
            Mmu.SetSTAT(_stat);
        }
    }

    /// <summary>
    /// Forces a complete frame render from VRAM data.
    /// This is used for testing purposes to render VRAM patterns immediately.
    /// </summary>
    public void ForceRenderFrame()
    {
        // Update register cache before rendering
        RefreshRegisterCache();

        // Only render if LCD is enabled
        if (!_lcdc.LcdEnable)
        {
            return;
        }

        // Render all scanlines
        for (int scanline = 0; scanline < ScreenHeight; scanline++)
        {
            RenderScanline(scanline);
        }
    }

    /// <summary>
    /// Checks for STAT interrupt conditions.
    /// </summary>
    private void CheckStatInterrupt()
    {
        bool shouldInterrupt = false;

        // Check LY=LYC interrupt (bit 6)
        if ((_stat & 0x40) != 0 && (_stat & 0x04) != 0)
        {
            shouldInterrupt = true;
        }

        // Check mode interrupt enables (bits 3-5)
        switch (_mode)
        {
            case LcdMode.HBlank when (_stat & 0x08) != 0:
            case LcdMode.VBlank when (_stat & 0x10) != 0:
            case LcdMode.OamScan when (_stat & 0x20) != 0:
                shouldInterrupt = true;
                break;
        }

        if (shouldInterrupt)
        {
            _interruptController.Request(InterruptType.LCDStat);
        }
    }
}
