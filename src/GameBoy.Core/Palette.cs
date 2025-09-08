namespace GameBoy.Core;

/// <summary>
/// Represents a Game Boy palette for converting 2-bit color indices to RGB values.
/// Handles background palette (BGP) and object palettes (OBP0, OBP1).
/// </summary>
public readonly struct Palette
{
    private readonly byte _value;

    public Palette(byte value)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the color mapping for shade 0 (lightest).
    /// </summary>
    public int Color0 => _value & 0x03;

    /// <summary>
    /// Gets the color mapping for shade 1.
    /// </summary>
    public int Color1 => (_value >> 2) & 0x03;

    /// <summary>
    /// Gets the color mapping for shade 2.
    /// </summary>
    public int Color2 => (_value >> 4) & 0x03;

    /// <summary>
    /// Gets the color mapping for shade 3 (darkest).
    /// </summary>
    public int Color3 => (_value >> 6) & 0x03;

    /// <summary>
    /// Maps a 2-bit color index (0-3) to the palette's color value (0-3).
    /// </summary>
    /// <param name="colorIndex">2-bit color index from tile data</param>
    /// <returns>Mapped color value (0-3)</returns>
    public int MapColor(int colorIndex) => colorIndex switch
    {
        0 => Color0,
        1 => Color1,
        2 => Color2,
        3 => Color3,
        _ => 0
    };

    /// <summary>
    /// Converts a palette color value (0-3) to a 32-bit RGBA color.
    /// Uses classic Game Boy green-tinted grayscale colors.
    /// </summary>
    /// <param name="colorValue">Palette color value (0-3)</param>
    /// <returns>32-bit RGBA color value</returns>
    public static int ToRgba(int colorValue) => colorValue switch
    {
        0 => unchecked((int)0xFF9BBB0F), // Lightest green
        1 => unchecked((int)0xFF8BAC0F), // Light green
        2 => unchecked((int)0xFF306230), // Dark green
        3 => unchecked((int)0xFF0F380F), // Darkest green
        _ => unchecked((int)0xFF9BBB0F)  // Default to lightest
    };

    /// <summary>
    /// Maps a 2-bit color index through the palette and converts to RGBA.
    /// </summary>
    /// <param name="colorIndex">2-bit color index from tile data</param>
    /// <returns>32-bit RGBA color value</returns>
    public int GetRgbaColor(int colorIndex)
    {
        int mappedColor = MapColor(colorIndex);
        return ToRgba(mappedColor);
    }

    /// <summary>
    /// Creates a default background palette (identity mapping: 0→0, 1→1, 2→2, 3→3).
    /// </summary>
    public static Palette DefaultBackground => new(0xE4); // 11 10 01 00

    /// <summary>
    /// Creates a default object palette with color 0 as transparent.
    /// </summary>
    public static Palette DefaultObject => new(0xE4); // 11 10 01 00

    public static implicit operator byte(Palette palette) => palette._value;
    public static implicit operator Palette(byte value) => new(value);

    public override string ToString() => $"Palette: 0x{_value:X2} ({Color3}-{Color2}-{Color1}-{Color0})";
}