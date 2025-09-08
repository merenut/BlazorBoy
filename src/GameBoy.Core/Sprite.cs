namespace GameBoy.Core;

/// <summary>
/// Represents a Game Boy sprite (OAM entry).
/// Each sprite is defined by 4 bytes in Object Attribute Memory (OAM).
/// </summary>
public readonly struct Sprite
{
    /// <summary>
    /// Y coordinate of the sprite on screen (offset by 16).
    /// Actual screen position is Y - 16.
    /// </summary>
    public byte Y { get; }

    /// <summary>
    /// X coordinate of the sprite on screen (offset by 8).
    /// Actual screen position is X - 8.
    /// </summary>
    public byte X { get; }

    /// <summary>
    /// Tile index for the sprite.
    /// For 8x16 sprites, the LSB is ignored.
    /// </summary>
    public byte TileIndex { get; }

    /// <summary>
    /// Sprite attributes byte containing flags.
    /// </summary>
    public byte Attributes { get; }

    public Sprite(byte y, byte x, byte tileIndex, byte attributes)
    {
        Y = y;
        X = x;
        TileIndex = tileIndex;
        Attributes = attributes;
    }

    /// <summary>
    /// Creates a sprite from a 4-byte OAM entry.
    /// </summary>
    /// <param name="oamData">4 bytes from OAM memory</param>
    /// <param name="offset">Starting offset in the OAM data</param>
    public Sprite(byte[] oamData, int offset)
    {
        if (oamData == null || offset + 3 >= oamData.Length)
            throw new ArgumentException("Invalid OAM data or offset");

        Y = oamData[offset];
        X = oamData[offset + 1];
        TileIndex = oamData[offset + 2];
        Attributes = oamData[offset + 3];
    }

    /// <summary>
    /// Gets the actual screen Y position (Y coordinate - 16).
    /// </summary>
    public int ScreenY => Y - 16;

    /// <summary>
    /// Gets the actual screen X position (X coordinate - 8).
    /// </summary>
    public int ScreenX => X - 8;

    /// <summary>
    /// Bit 7: Object-to-background priority.
    /// 0 = Object above background, 1 = Object behind background colors 1-3
    /// </summary>
    public bool BehindBackground => (Attributes & 0x80) != 0;

    /// <summary>
    /// Bit 6: Y flip flag.
    /// 0 = Normal, 1 = Vertically mirrored
    /// </summary>
    public bool FlipY => (Attributes & 0x40) != 0;

    /// <summary>
    /// Bit 5: X flip flag.
    /// 0 = Normal, 1 = Horizontally mirrored
    /// </summary>
    public bool FlipX => (Attributes & 0x20) != 0;

    /// <summary>
    /// Bit 4: Palette number.
    /// 0 = Use OBP0, 1 = Use OBP1
    /// </summary>
    public bool UseObp1 => (Attributes & 0x10) != 0;

    /// <summary>
    /// Gets the effective tile index for 8x16 sprites.
    /// For 8x16 sprites, LSB is ignored for the top tile.
    /// </summary>
    /// <param name="is8x16">True if sprite is 8x16 pixels</param>
    /// <returns>Tile index for top tile (8x16) or single tile (8x8)</returns>
    public byte GetTopTileIndex(bool is8x16)
    {
        return is8x16 ? (byte)(TileIndex & 0xFE) : TileIndex;
    }

    /// <summary>
    /// Gets the tile index for the bottom half of an 8x16 sprite.
    /// </summary>
    /// <returns>Tile index for bottom tile</returns>
    public byte GetBottomTileIndex()
    {
        return (byte)(TileIndex | 0x01);
    }

    /// <summary>
    /// Checks if the sprite is visible on the given scanline.
    /// </summary>
    /// <param name="scanline">Current scanline (0-143)</param>
    /// <param name="spriteHeight">Height of sprites (8 or 16 pixels)</param>
    /// <returns>True if sprite overlaps the scanline</returns>
    public bool IsVisibleOnScanline(int scanline, int spriteHeight)
    {
        int spriteY = ScreenY;
        return scanline >= spriteY && scanline < spriteY + spriteHeight;
    }

    /// <summary>
    /// Gets the relative Y coordinate within the sprite for the given scanline.
    /// </summary>
    /// <param name="scanline">Current scanline</param>
    /// <returns>Y coordinate within sprite (0 to height-1)</returns>
    public int GetRelativeY(int scanline)
    {
        int relativeY = scanline - ScreenY;
        return FlipY ? (7 - relativeY) : relativeY; // Handle Y flip
    }

    /// <summary>
    /// Gets the relative Y coordinate for 8x16 sprites, handling flip.
    /// </summary>
    /// <param name="scanline">Current scanline</param>
    /// <returns>Y coordinate within sprite (0-15)</returns>
    public int GetRelativeY16(int scanline)
    {
        int relativeY = scanline - ScreenY;
        return FlipY ? (15 - relativeY) : relativeY; // Handle Y flip for 8x16
    }

    /// <summary>
    /// Checks if the sprite is visible horizontally (X position in range).
    /// </summary>
    public bool IsVisibleHorizontally => ScreenX > -8 && ScreenX < 160;

    public override string ToString()
    {
        string flips = "";
        if (FlipX) flips += "X";
        if (FlipY) flips += "Y";
        string flipText = flips.Length > 0 ? $", Flip: {flips}" : "";

        return $"Sprite: Y={Y}, X={X}, Tile=0x{TileIndex:X2}, Attr=0x{Attributes:X2} " +
               $"(Pos: {ScreenX},{ScreenY}, Priority: {(BehindBackground ? "Behind" : "Above")}" +
               $"{flipText}, Palette: OBP{(UseObp1 ? "1" : "0")})";
    }
}