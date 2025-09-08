using GameBoy.Core;
using Xunit;

namespace GameBoy.Tests;

/// <summary>
/// Tests for Sprite functionality including OAM parsing, positioning, and attributes.
/// </summary>
public class SpriteTests
{
    [Fact]
    public void Constructor_WithIndividualValues_InitializesCorrectly()
    {
        // Arrange & Act
        var sprite = new Sprite(0x50, 0x48, 0x02, 0xA0);

        // Assert
        Assert.Equal(0x50, sprite.Y);
        Assert.Equal(0x48, sprite.X);
        Assert.Equal(0x02, sprite.TileIndex);
        Assert.Equal(0xA0, sprite.Attributes);
    }

    [Fact]
    public void Constructor_WithOamData_ParsesCorrectly()
    {
        // Arrange
        byte[] oamData = { 0x50, 0x48, 0x02, 0xA0, 0xFF, 0xFF };

        // Act
        var sprite = new Sprite(oamData, 0);

        // Assert
        Assert.Equal(0x50, sprite.Y);
        Assert.Equal(0x48, sprite.X);
        Assert.Equal(0x02, sprite.TileIndex);
        Assert.Equal(0xA0, sprite.Attributes);
    }

    [Fact]
    public void Constructor_WithOamDataAndOffset_ParsesCorrectly()
    {
        // Arrange
        byte[] oamData = { 0xFF, 0xFF, 0x50, 0x48, 0x02, 0xA0 };

        // Act
        var sprite = new Sprite(oamData, 2);

        // Assert
        Assert.Equal(0x50, sprite.Y);
        Assert.Equal(0x48, sprite.X);
        Assert.Equal(0x02, sprite.TileIndex);
        Assert.Equal(0xA0, sprite.Attributes);
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData(new byte[] { 0x50 }, 0)] // Too small
    [InlineData(new byte[] { 0x50, 0x48, 0x02, 0xA0 }, 1)] // Offset causes overrun
    public void Constructor_WithInvalidOamData_ThrowsException(byte[]? oamData, int offset)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Sprite(oamData!, offset));
    }

    [Theory]
    [InlineData(0x50, 64)]  // Y=80 -> ScreenY=64
    [InlineData(0x10, 0)]   // Y=16 -> ScreenY=0
    [InlineData(0x00, -16)] // Y=0 -> ScreenY=-16 (off-screen)
    public void ScreenY_ReturnsCorrectPosition(byte y, int expectedScreenY)
    {
        // Arrange
        var sprite = new Sprite(y, 0x48, 0x02, 0x00);

        // Act & Assert
        Assert.Equal(expectedScreenY, sprite.ScreenY);
    }

    [Theory]
    [InlineData(0x48, 64)]  // X=72 -> ScreenX=64
    [InlineData(0x08, 0)]   // X=8 -> ScreenX=0
    [InlineData(0x00, -8)]  // X=0 -> ScreenX=-8 (off-screen)
    public void ScreenX_ReturnsCorrectPosition(byte x, int expectedScreenX)
    {
        // Arrange
        var sprite = new Sprite(0x50, x, 0x02, 0x00);

        // Act & Assert
        Assert.Equal(expectedScreenX, sprite.ScreenX);
    }

    [Theory]
    [InlineData(0x80, true)]  // Bit 7 set
    [InlineData(0x00, false)] // Bit 7 clear
    [InlineData(0x7F, false)] // All other bits set
    public void BehindBackground_ReturnsCorrectValue(byte attributes, bool expected)
    {
        // Arrange
        var sprite = new Sprite(0x50, 0x48, 0x02, attributes);

        // Act & Assert
        Assert.Equal(expected, sprite.BehindBackground);
    }

    [Theory]
    [InlineData(0x40, true)]  // Bit 6 set
    [InlineData(0x00, false)] // Bit 6 clear
    [InlineData(0xBF, false)] // All other bits set
    public void FlipY_ReturnsCorrectValue(byte attributes, bool expected)
    {
        // Arrange
        var sprite = new Sprite(0x50, 0x48, 0x02, attributes);

        // Act & Assert
        Assert.Equal(expected, sprite.FlipY);
    }

    [Theory]
    [InlineData(0x20, true)]  // Bit 5 set
    [InlineData(0x00, false)] // Bit 5 clear
    [InlineData(0xDF, false)] // All other bits set
    public void FlipX_ReturnsCorrectValue(byte attributes, bool expected)
    {
        // Arrange
        var sprite = new Sprite(0x50, 0x48, 0x02, attributes);

        // Act & Assert
        Assert.Equal(expected, sprite.FlipX);
    }

    [Theory]
    [InlineData(0x10, true)]  // Bit 4 set -> OBP1
    [InlineData(0x00, false)] // Bit 4 clear -> OBP0
    [InlineData(0xEF, false)] // All other bits set
    public void UseObp1_ReturnsCorrectValue(byte attributes, bool expected)
    {
        // Arrange
        var sprite = new Sprite(0x50, 0x48, 0x02, attributes);

        // Act & Assert
        Assert.Equal(expected, sprite.UseObp1);
    }

    [Theory]
    [InlineData(0x02, false, 0x02)] // 8x8 mode: return as-is
    [InlineData(0x03, false, 0x03)] // 8x8 mode: return as-is
    [InlineData(0x02, true, 0x02)]  // 8x16 mode: even index
    [InlineData(0x03, true, 0x02)]  // 8x16 mode: odd index -> even
    [InlineData(0x05, true, 0x04)]  // 8x16 mode: odd index -> even
    public void GetTopTileIndex_ReturnsCorrectIndex(byte tileIndex, bool is8x16, byte expected)
    {
        // Arrange
        var sprite = new Sprite(0x50, 0x48, tileIndex, 0x00);

        // Act
        byte result = sprite.GetTopTileIndex(is8x16);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0x02, 0x03)] // Even index -> odd index
    [InlineData(0x03, 0x03)] // Odd index -> stay odd
    [InlineData(0x04, 0x05)] // Even index -> odd index
    public void GetBottomTileIndex_ReturnsOddIndex(byte tileIndex, byte expected)
    {
        // Arrange
        var sprite = new Sprite(0x50, 0x48, tileIndex, 0x00);

        // Act
        byte result = sprite.GetBottomTileIndex();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0x50, 68, 8, true)]   // Y=80, ScreenY=64, scanline 68 in range [64, 72)
    [InlineData(0x50, 63, 8, false)]  // Y=80, ScreenY=64, scanline 63 not in range
    [InlineData(0x50, 72, 8, false)]  // Y=80, ScreenY=64, scanline 72 not in range  
    [InlineData(0x50, 70, 16, true)]  // Y=80, ScreenY=64, scanline 70 in range [64, 80) for 8x16
    [InlineData(0x50, 80, 16, false)] // Y=80, ScreenY=64, scanline 80 not in range for 8x16
    public void IsVisibleOnScanline_ReturnsCorrectValue(byte y, int scanline, int spriteHeight, bool expected)
    {
        // Arrange
        var sprite = new Sprite(y, 0x48, 0x02, 0x00);

        // Act
        bool result = sprite.IsVisibleOnScanline(scanline, spriteHeight);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0x50, 68, 0x00, 4)]   // No Y flip: scanline 68, ScreenY=64, relative=4
    [InlineData(0x50, 68, 0x40, 3)]   // Y flip: scanline 68, ScreenY=64, relative=7-4=3
    [InlineData(0x50, 67, 0x00, 3)]   // No Y flip: scanline 67, ScreenY=64, relative=3
    [InlineData(0x50, 67, 0x40, 4)]   // Y flip: scanline 67, ScreenY=64, relative=7-3=4
    public void GetRelativeY_ReturnsCorrectValue(byte y, int scanline, byte attributes, int expected)
    {
        // Arrange
        var sprite = new Sprite(y, 0x48, 0x02, attributes);

        // Act
        int result = sprite.GetRelativeY(scanline);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0x50, 68, 0x00, 4)]    // No Y flip: scanline 68, ScreenY=64, relative=4
    [InlineData(0x50, 68, 0x40, 11)]   // Y flip: scanline 68, ScreenY=64, relative=15-4=11
    [InlineData(0x50, 75, 0x00, 11)]   // No Y flip: scanline 75, ScreenY=64, relative=11
    [InlineData(0x50, 75, 0x40, 4)]    // Y flip: scanline 75, ScreenY=64, relative=15-11=4
    public void GetRelativeY16_ReturnsCorrectValueFor8x16Sprites(byte y, int scanline, byte attributes, int expected)
    {
        // Arrange
        var sprite = new Sprite(y, 0x48, 0x02, attributes);

        // Act
        int result = sprite.GetRelativeY16(scanline);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0x48, true)]  // X=72, ScreenX=64, visible
    [InlineData(0x08, true)]  // X=8, ScreenX=0, visible
    [InlineData(0x00, false)] // X=0, ScreenX=-8, not visible (off-screen left)
    [InlineData(0xA8, false)] // X=168, ScreenX=160, not visible (off-screen right)
    public void IsVisibleHorizontally_ReturnsCorrectValue(byte x, bool expected)
    {
        // Arrange
        var sprite = new Sprite(0x50, x, 0x02, 0x00);

        // Act & Assert
        Assert.Equal(expected, sprite.IsVisibleHorizontally);
    }

    [Fact]
    public void ToString_ReturnsDescriptiveString()
    {
        // Arrange
        var sprite = new Sprite(0x50, 0x48, 0x02, 0xB0); // Behind background, Y flip, OBP1

        // Act
        string result = sprite.ToString();

        // Assert
        Assert.Contains("Sprite", result);
        Assert.Contains("Y=80", result);
        Assert.Contains("X=72", result);
        Assert.Contains("Tile=0x02", result);
        Assert.Contains("Attr=0xB0", result);
        Assert.Contains("Pos: 64,64", result);
        Assert.Contains("Behind", result);
        Assert.Contains("Y", result); // Y flip
        Assert.Contains("OBP1", result);
    }

    [Fact]
    public void ToString_WithNoFlip_ShowsCorrectAttributes()
    {
        // Arrange
        var sprite = new Sprite(0x50, 0x48, 0x02, 0x00); // No flips, above background, OBP0

        // Act
        string result = sprite.ToString();

        // Assert
        Assert.Contains("Above", result);
        Assert.Contains("OBP0", result);
        Assert.DoesNotContain("Flip:", result); // Should be empty when no flips
    }
}