using GameBoy.Core;
using Xunit;

namespace GameBoy.Tests;

/// <summary>
/// Tests for Palette functionality including color mapping and RGB conversion.
/// </summary>
public class PaletteTests
{
    [Fact]
    public void Constructor_InitializesValue()
    {
        // Arrange & Act
        var palette = new Palette(0xE4);

        // Assert
        Assert.Equal(0xE4, (byte)palette);
    }

    [Theory]
    [InlineData(0xE4, 0, 1, 2, 3)] // Identity mapping: 11 10 01 00
    [InlineData(0x1B, 3, 2, 1, 0)] // Reverse mapping: 00 01 10 11
    [InlineData(0x00, 0, 0, 0, 0)] // All zeros
    [InlineData(0xFF, 3, 3, 3, 3)] // All threes
    public void ColorProperties_ReturnCorrectValues(byte value, int color0, int color1, int color2, int color3)
    {
        // Arrange
        var palette = new Palette(value);

        // Act & Assert
        Assert.Equal(color0, palette.Color0);
        Assert.Equal(color1, palette.Color1);
        Assert.Equal(color2, palette.Color2);
        Assert.Equal(color3, palette.Color3);
    }

    [Theory]
    [InlineData(0xE4, 0, 0)] // Color 0 -> mapped to 0
    [InlineData(0xE4, 1, 1)] // Color 1 -> mapped to 1
    [InlineData(0xE4, 2, 2)] // Color 2 -> mapped to 2
    [InlineData(0xE4, 3, 3)] // Color 3 -> mapped to 3
    [InlineData(0x1B, 0, 3)] // Reverse palette: Color 0 -> mapped to 3
    [InlineData(0x1B, 3, 0)] // Reverse palette: Color 3 -> mapped to 0
    public void MapColor_ReturnsCorrectMappedValue(byte paletteValue, int colorIndex, int expectedMapped)
    {
        // Arrange
        var palette = new Palette(paletteValue);

        // Act
        int result = palette.MapColor(colorIndex);

        // Assert
        Assert.Equal(expectedMapped, result);
    }

    [Theory]
    [InlineData(-1, 0)] // Out of range -> default to 0
    [InlineData(4, 0)]  // Out of range -> default to 0
    [InlineData(5, 0)]  // Out of range -> default to 0
    public void MapColor_WithInvalidIndex_ReturnsDefault(int colorIndex, int expectedMapped)
    {
        // Arrange
        var palette = new Palette(0xE4);

        // Act
        int result = palette.MapColor(colorIndex);

        // Assert
        Assert.Equal(expectedMapped, result);
    }

    [Theory]
    [InlineData(0, -6571249)] // Lightest green
    [InlineData(1, -7623665)] // Light green
    [InlineData(2, -13606352)] // Dark green
    [InlineData(3, -15779825)] // Darkest green
    public void ToRgba_ReturnsCorrectColors(int colorValue, int expectedRgba)
    {
        // Act
        int result = Palette.ToRgba(colorValue);

        // Assert
        Assert.Equal(expectedRgba, result);
    }

    [Theory]
    [InlineData(-1, -6571249)] // Out of range -> default to lightest
    [InlineData(4, -6571249)]  // Out of range -> default to lightest
    public void ToRgba_WithInvalidValue_ReturnsDefault(int colorValue, int expectedRgba)
    {
        // Act
        int result = Palette.ToRgba(colorValue);

        // Assert
        Assert.Equal(expectedRgba, result);
    }

    [Theory]
    [InlineData(0xE4, 0, -6571249)] // Identity palette: color 0 -> lightest green
    [InlineData(0xE4, 3, -15779825)] // Identity palette: color 3 -> darkest green
    [InlineData(0x1B, 0, -15779825)] // Reverse palette: color 0 -> darkest green
    [InlineData(0x1B, 3, -6571249)] // Reverse palette: color 3 -> lightest green
    public void GetRgbaColor_CombinesMappingAndConversion(byte paletteValue, int colorIndex, int expectedRgba)
    {
        // Arrange
        var palette = new Palette(paletteValue);

        // Act
        int result = palette.GetRgbaColor(colorIndex);

        // Assert
        Assert.Equal(expectedRgba, result);
    }

    [Fact]
    public void DefaultBackground_HasCorrectValue()
    {
        // Act
        var palette = Palette.DefaultBackground;

        // Assert
        Assert.Equal(0xE4, (byte)palette);
        Assert.Equal(0, palette.Color0);
        Assert.Equal(1, palette.Color1);
        Assert.Equal(2, palette.Color2);
        Assert.Equal(3, palette.Color3);
    }

    [Fact]
    public void DefaultObject_HasCorrectValue()
    {
        // Act
        var palette = Palette.DefaultObject;

        // Assert
        Assert.Equal(0xE4, (byte)palette);
        Assert.Equal(0, palette.Color0);
        Assert.Equal(1, palette.Color1);
        Assert.Equal(2, palette.Color2);
        Assert.Equal(3, palette.Color3);
    }

    [Fact]
    public void ImplicitConversion_FromByte_Works()
    {
        // Arrange
        byte value = 0xE4;

        // Act
        Palette palette = value;

        // Assert
        Assert.Equal(value, (byte)palette);
    }

    [Fact]
    public void ImplicitConversion_ToByte_Works()
    {
        // Arrange
        var palette = new Palette(0xE4);

        // Act
        byte value = palette;

        // Assert
        Assert.Equal(0xE4, value);
    }

    [Fact]
    public void ToString_ReturnsDescriptiveString()
    {
        // Arrange
        var palette = new Palette(0xE4);

        // Act
        string result = palette.ToString();

        // Assert
        Assert.Contains("Palette", result);
        Assert.Contains("0xE4", result);
        Assert.Contains("3-2-1-0", result); // Color order should be displayed
    }

    [Fact]
    public void RgbaColors_HaveCorrectAlphaChannel()
    {
        // Act & Assert - All colors should have full opacity (alpha = 0xFF)
        for (int i = 0; i < 4; i++)
        {
            int color = Palette.ToRgba(i);
            int alpha = (color >> 24) & 0xFF;
            Assert.Equal(0xFF, alpha);
        }
    }

    [Fact]
    public void RgbaColors_AreInCorrectBrightnessOrder()
    {
        // Act
        int color0 = Palette.ToRgba(0); // Lightest
        int color1 = Palette.ToRgba(1);
        int color2 = Palette.ToRgba(2);
        int color3 = Palette.ToRgba(3); // Darkest

        // Assert - Extract green channel (Game Boy colors are green-tinted)
        int green0 = (color0 >> 8) & 0xFF;
        int green1 = (color1 >> 8) & 0xFF;
        int green2 = (color2 >> 8) & 0xFF;
        int green3 = (color3 >> 8) & 0xFF;

        // Colors should get progressively darker
        Assert.True(green0 > green1);
        Assert.True(green1 > green2);
        Assert.True(green2 > green3);
    }
}