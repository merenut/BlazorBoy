using GameBoy.Core;
using Xunit;

namespace GameBoy.Tests;

/// <summary>
/// Tests for LCDC register functionality and bit-level access.
/// </summary>
public class LcdcTests
{
    [Fact]
    public void Constructor_InitializesValue()
    {
        // Arrange & Act
        var lcdc = new Lcdc(0x91);

        // Assert
        Assert.Equal(0x91, (byte)lcdc);
    }

    [Theory]
    [InlineData(0x80, true)]   // Bit 7 set
    [InlineData(0x00, false)]  // Bit 7 clear
    [InlineData(0xFF, true)]   // All bits set
    [InlineData(0x7F, false)]  // Bit 7 clear, others set
    public void LcdEnable_ReturnsCorrectValue(byte value, bool expected)
    {
        // Arrange
        var lcdc = new Lcdc(value);

        // Act & Assert
        Assert.Equal(expected, lcdc.LcdEnable);
    }

    [Theory]
    [InlineData(0x40, true)]   // Bit 6 set
    [InlineData(0x00, false)]  // Bit 6 clear
    [InlineData(0xBF, false)]  // Bit 6 clear, others set
    public void WindowTileMapArea_ReturnsCorrectValue(byte value, bool expected)
    {
        // Arrange
        var lcdc = new Lcdc(value);

        // Act & Assert
        Assert.Equal(expected, lcdc.WindowTileMapArea);
    }

    [Theory]
    [InlineData(0x20, true)]   // Bit 5 set
    [InlineData(0x00, false)]  // Bit 5 clear
    [InlineData(0xDF, false)]  // Bit 5 clear, others set
    public void WindowEnable_ReturnsCorrectValue(byte value, bool expected)
    {
        // Arrange
        var lcdc = new Lcdc(value);

        // Act & Assert
        Assert.Equal(expected, lcdc.WindowEnable);
    }

    [Theory]
    [InlineData(0x10, true)]   // Bit 4 set
    [InlineData(0x00, false)]  // Bit 4 clear
    [InlineData(0xEF, false)]  // Bit 4 clear, others set
    public void BgWindowTileDataArea_ReturnsCorrectValue(byte value, bool expected)
    {
        // Arrange
        var lcdc = new Lcdc(value);

        // Act & Assert
        Assert.Equal(expected, lcdc.BgWindowTileDataArea);
    }

    [Theory]
    [InlineData(0x08, true)]   // Bit 3 set
    [InlineData(0x00, false)]  // Bit 3 clear
    [InlineData(0xF7, false)]  // Bit 3 clear, others set
    public void BgTileMapArea_ReturnsCorrectValue(byte value, bool expected)
    {
        // Arrange
        var lcdc = new Lcdc(value);

        // Act & Assert
        Assert.Equal(expected, lcdc.BgTileMapArea);
    }

    [Theory]
    [InlineData(0x04, true)]   // Bit 2 set
    [InlineData(0x00, false)]  // Bit 2 clear
    [InlineData(0xFB, false)]  // Bit 2 clear, others set
    public void ObjSize_ReturnsCorrectValue(byte value, bool expected)
    {
        // Arrange
        var lcdc = new Lcdc(value);

        // Act & Assert
        Assert.Equal(expected, lcdc.ObjSize);
    }

    [Theory]
    [InlineData(0x02, true)]   // Bit 1 set
    [InlineData(0x00, false)]  // Bit 1 clear
    [InlineData(0xFD, false)]  // Bit 1 clear, others set
    public void ObjEnable_ReturnsCorrectValue(byte value, bool expected)
    {
        // Arrange
        var lcdc = new Lcdc(value);

        // Act & Assert
        Assert.Equal(expected, lcdc.ObjEnable);
    }

    [Theory]
    [InlineData(0x01, true)]   // Bit 0 set
    [InlineData(0x00, false)]  // Bit 0 clear
    [InlineData(0xFE, false)]  // Bit 0 clear, others set
    public void BgWindowEnable_ReturnsCorrectValue(byte value, bool expected)
    {
        // Arrange
        var lcdc = new Lcdc(value);

        // Act & Assert
        Assert.Equal(expected, lcdc.BgWindowEnable);
    }

    [Theory]
    [InlineData(0x08, 0x9C00)]  // Bit 3 set -> 0x9C00
    [InlineData(0x00, 0x9800)]  // Bit 3 clear -> 0x9800
    public void BgTileMapBase_ReturnsCorrectAddress(byte value, ushort expected)
    {
        // Arrange
        var lcdc = new Lcdc(value);

        // Act & Assert
        Assert.Equal(expected, lcdc.BgTileMapBase);
    }

    [Theory]
    [InlineData(0x40, 0x9C00)]  // Bit 6 set -> 0x9C00
    [InlineData(0x00, 0x9800)]  // Bit 6 clear -> 0x9800
    public void WindowTileMapBase_ReturnsCorrectAddress(byte value, ushort expected)
    {
        // Arrange
        var lcdc = new Lcdc(value);

        // Act & Assert
        Assert.Equal(expected, lcdc.WindowTileMapBase);
    }

    [Theory]
    [InlineData(0x10, 0x8000)]  // Bit 4 set -> 0x8000
    [InlineData(0x00, 0x8800)]  // Bit 4 clear -> 0x8800
    public void TileDataBase_ReturnsCorrectAddress(byte value, ushort expected)
    {
        // Arrange
        var lcdc = new Lcdc(value);

        // Act & Assert
        Assert.Equal(expected, lcdc.TileDataBase);
    }

    [Theory]
    [InlineData(0x10, false)]  // Bit 4 set -> unsigned (false)
    [InlineData(0x00, true)]   // Bit 4 clear -> signed (true)
    public void UsesSignedTileIndexing_ReturnsCorrectValue(byte value, bool expected)
    {
        // Arrange
        var lcdc = new Lcdc(value);

        // Act & Assert
        Assert.Equal(expected, lcdc.UsesSignedTileIndexing);
    }

    [Theory]
    [InlineData(0x04, 16)]  // Bit 2 set -> 8x16 sprites
    [InlineData(0x00, 8)]   // Bit 2 clear -> 8x8 sprites
    public void SpriteHeight_ReturnsCorrectValue(byte value, int expected)
    {
        // Arrange
        var lcdc = new Lcdc(value);

        // Act & Assert
        Assert.Equal(expected, lcdc.SpriteHeight);
    }

    [Fact]
    public void ImplicitConversion_FromByte_Works()
    {
        // Arrange
        byte value = 0x91;

        // Act
        Lcdc lcdc = value;

        // Assert
        Assert.Equal(value, (byte)lcdc);
    }

    [Fact]
    public void ImplicitConversion_ToByte_Works()
    {
        // Arrange
        var lcdc = new Lcdc(0x91);

        // Act
        byte value = lcdc;

        // Assert
        Assert.Equal(0x91, value);
    }

    [Fact]
    public void ToString_ReturnsDescriptiveString()
    {
        // Arrange
        var lcdc = new Lcdc(0x91); // LCD on, BG on

        // Act
        string result = lcdc.ToString();

        // Assert
        Assert.Contains("LCDC", result);
        Assert.Contains("0x91", result);
        Assert.Contains("LCD:On", result);
        Assert.Contains("BG:On", result);
    }
}