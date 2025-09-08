namespace GameBoy.Core;

/// <summary>
/// Represents the LCD Control (LCDC) register at 0xFF40.
/// Provides bit-level access to LCD configuration flags.
/// </summary>
public readonly struct Lcdc
{
    private readonly byte _value;

    public Lcdc(byte value)
    {
        _value = value;
    }

    /// <summary>
    /// Bit 7: LCD and PPU enable flag.
    /// 0 = LCD off, 1 = LCD on
    /// </summary>
    public bool LcdEnable => (_value & 0x80) != 0;

    /// <summary>
    /// Bit 6: Window tile map area.
    /// 0 = 9800-9BFF, 1 = 9C00-9FFF
    /// </summary>
    public bool WindowTileMapArea => (_value & 0x40) != 0;

    /// <summary>
    /// Bit 5: Window enable flag.
    /// 0 = Window off, 1 = Window on
    /// </summary>
    public bool WindowEnable => (_value & 0x20) != 0;

    /// <summary>
    /// Bit 4: Background and window tile data area.
    /// 0 = 8800-97FF (signed indexing), 1 = 8000-8FFF (unsigned indexing)
    /// </summary>
    public bool BgWindowTileDataArea => (_value & 0x10) != 0;

    /// <summary>
    /// Bit 3: Background tile map area.
    /// 0 = 9800-9BFF, 1 = 9C00-9FFF
    /// </summary>
    public bool BgTileMapArea => (_value & 0x08) != 0;

    /// <summary>
    /// Bit 2: Object (sprite) size flag.
    /// 0 = 8x8 pixels, 1 = 8x16 pixels
    /// </summary>
    public bool ObjSize => (_value & 0x04) != 0;

    /// <summary>
    /// Bit 1: Object (sprite) enable flag.
    /// 0 = Objects off, 1 = Objects on
    /// </summary>
    public bool ObjEnable => (_value & 0x02) != 0;

    /// <summary>
    /// Bit 0: Background and window enable flag.
    /// 0 = Background and window off, 1 = Background and window on
    /// </summary>
    public bool BgWindowEnable => (_value & 0x01) != 0;

    /// <summary>
    /// Gets the background tile map base address.
    /// </summary>
    public ushort BgTileMapBase => BgTileMapArea ? (ushort)0x9C00 : (ushort)0x9800;

    /// <summary>
    /// Gets the window tile map base address.
    /// </summary>
    public ushort WindowTileMapBase => WindowTileMapArea ? (ushort)0x9C00 : (ushort)0x9800;

    /// <summary>
    /// Gets the tile data base address.
    /// </summary>
    public ushort TileDataBase => BgWindowTileDataArea ? (ushort)0x8000 : (ushort)0x8800;

    /// <summary>
    /// Gets whether tile indexing uses signed addressing mode.
    /// True when using 0x8800-0x97FF range with signed tile indices (-128 to 127).
    /// </summary>
    public bool UsesSignedTileIndexing => !BgWindowTileDataArea;

    /// <summary>
    /// Gets the sprite height in pixels (8 or 16).
    /// </summary>
    public int SpriteHeight => ObjSize ? 16 : 8;

    public static implicit operator byte(Lcdc lcdc) => lcdc._value;
    public static implicit operator Lcdc(byte value) => new(value);

    public override string ToString() => $"LCDC: 0x{_value:X2} (LCD:{(LcdEnable ? "On" : "Off")}, BG:{(BgWindowEnable ? "On" : "Off")}, Obj:{(ObjEnable ? "On" : "Off")}, Win:{(WindowEnable ? "On" : "Off")})";
}