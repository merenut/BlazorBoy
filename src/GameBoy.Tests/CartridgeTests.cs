using System;
using GameBoy.Core;
using Xunit;

namespace GameBoy.Tests;

/// <summary>
/// Tests for cartridge detection and MBC functionality.
/// </summary>
public class CartridgeTests
{
    [Fact]
    public void Detect_Mbc0Rom_ReturnsCorrectCartridge()
    {
        var rom = CreateRom(0x00, 0x00, 0x00); // ROM only, 32KB, no RAM

        var cartridge = Cartridge.Detect(rom);

        Assert.IsType<Mbc0>(cartridge);
    }

    [Fact]
    public void Detect_Mbc1Rom_ReturnsCorrectCartridge()
    {
        var rom = CreateRom(0x01, 0x01, 0x02); // MBC1, 64KB, 8KB RAM

        var cartridge = Cartridge.Detect(rom);

        Assert.IsType<Mbc1>(cartridge);
    }

    [Fact]
    public void Detect_Mbc3Rom_ReturnsCorrectCartridge()
    {
        var rom = CreateRom(0x11, 0x02, 0x03); // MBC3, 128KB, 32KB RAM

        var cartridge = Cartridge.Detect(rom);

        Assert.IsType<Mbc3>(cartridge);
    }

    [Fact]
    public void Detect_Mbc5Rom_ReturnsCorrectCartridge()
    {
        var rom = CreateRom(0x19, 0x03, 0x03); // MBC5, 256KB, 32KB RAM

        var cartridge = Cartridge.Detect(rom);

        Assert.IsType<Mbc5>(cartridge);
    }

    [Fact]
    public void Detect_UnsupportedMbc_ThrowsException()
    {
        var rom = CreateRom(0x05, 0x00, 0x01); // MBC2 (unsupported)

        var exception = Assert.Throws<NotSupportedException>(() => Cartridge.Detect(rom));
        Assert.Contains("MBC2 is not yet supported", exception.Message);
    }

    [Fact]
    public void Detect_UnknownCartridgeType_ThrowsException()
    {
        var rom = CreateRom(0x99, 0x00, 0x00); // Truly unknown type

        var exception = Assert.Throws<NotSupportedException>(() => Cartridge.Detect(rom));
        Assert.Contains("Unknown or unsupported cartridge type", exception.Message);
    }

    [Fact]
    public void Detect_TooSmallRom_ThrowsException()
    {
        var smallRom = new byte[0x100];

        Assert.Throws<ArgumentException>(() => Cartridge.Detect(smallRom));
    }

    [Theory]
    [InlineData(0x01)] // MBC1
    [InlineData(0x02)] // MBC1+RAM
    [InlineData(0x03)] // MBC1+RAM+BATTERY
    public void Detect_AllMbc1Variants_ReturnsCorrectCartridge(byte cartridgeType)
    {
        var rom = CreateRom(cartridgeType, 0x01, 0x02);

        var cartridge = Cartridge.Detect(rom);

        Assert.IsType<Mbc1>(cartridge);
    }

    [Theory]
    [InlineData(0x0F)] // MBC3+TIMER+BATTERY
    [InlineData(0x10)] // MBC3+TIMER+RAM+BATTERY
    [InlineData(0x11)] // MBC3
    [InlineData(0x12)] // MBC3+RAM
    [InlineData(0x13)] // MBC3+RAM+BATTERY
    public void Detect_AllMbc3Variants_ReturnsCorrectCartridge(byte cartridgeType)
    {
        var rom = CreateRom(cartridgeType, 0x02, 0x03);

        var cartridge = Cartridge.Detect(rom);

        Assert.IsType<Mbc3>(cartridge);
    }

    [Theory]
    [InlineData(0x19)] // MBC5
    [InlineData(0x1A)] // MBC5+RAM
    [InlineData(0x1B)] // MBC5+RAM+BATTERY
    [InlineData(0x1C)] // MBC5+RUMBLE
    [InlineData(0x1D)] // MBC5+RUMBLE+RAM
    [InlineData(0x1E)] // MBC5+RUMBLE+RAM+BATTERY
    public void Detect_AllMbc5Variants_ReturnsCorrectCartridge(byte cartridgeType)
    {
        var rom = CreateRom(cartridgeType, 0x03, 0x03);

        var cartridge = Cartridge.Detect(rom);

        Assert.IsType<Mbc5>(cartridge);
    }

    [Theory]
    [InlineData(0x05)] // MBC2
    [InlineData(0x06)] // MBC2+BATTERY
    public void Detect_AllMbc2Variants_ThrowsNotSupportedException(byte cartridgeType)
    {
        var rom = CreateRom(cartridgeType, 0x00, 0x01);

        var exception = Assert.Throws<NotSupportedException>(() => Cartridge.Detect(rom));
        Assert.Contains("MBC2 is not yet supported", exception.Message);
        Assert.Contains($"0x{cartridgeType:X2}", exception.Message);
    }

    [Theory]
    [InlineData(0x15)] // MBC4
    [InlineData(0x16)] // MBC4+RAM
    [InlineData(0x17)] // MBC4+RAM+BATTERY
    public void Detect_AllMbc4Variants_ThrowsNotSupportedException(byte cartridgeType)
    {
        var rom = CreateRom(cartridgeType, 0x01, 0x02);

        var exception = Assert.Throws<NotSupportedException>(() => Cartridge.Detect(rom));
        Assert.Contains("MBC4 is not yet supported", exception.Message);
        Assert.Contains($"0x{cartridgeType:X2}", exception.Message);
    }

    [Fact]
    public void Detect_Mbc6_ThrowsNotSupportedException()
    {
        var rom = CreateRom(0x20, 0x01, 0x02); // MBC6

        var exception = Assert.Throws<NotSupportedException>(() => Cartridge.Detect(rom));
        Assert.Contains("MBC6 is not yet supported", exception.Message);
        Assert.Contains("0x20", exception.Message);
    }

    [Fact]
    public void Detect_Mbc7_ThrowsNotSupportedException()
    {
        var rom = CreateRom(0x22, 0x01, 0x02); // MBC7+SENSOR+RUMBLE+RAM+BATTERY

        var exception = Assert.Throws<NotSupportedException>(() => Cartridge.Detect(rom));
        Assert.Contains("MBC7 is not yet supported", exception.Message);
        Assert.Contains("0x22", exception.Message);
    }

    [Fact]
    public void Detect_HuC1_ThrowsNotSupportedException()
    {
        var rom = CreateRom(0xFF, 0x01, 0x02); // HuC1+RAM+BATTERY

        var exception = Assert.Throws<NotSupportedException>(() => Cartridge.Detect(rom));
        Assert.Contains("HuC1 is not yet supported", exception.Message);
        Assert.Contains("0xFF", exception.Message);
    }

    [Fact]
    public void Detect_HuC3_ThrowsNotSupportedException()
    {
        var rom = CreateRom(0xFE, 0x01, 0x02); // HuC3+RAM+BATTERY+RTC

        var exception = Assert.Throws<NotSupportedException>(() => Cartridge.Detect(rom));
        Assert.Contains("HuC3 is not yet supported", exception.Message);
        Assert.Contains("0xFE", exception.Message);
    }

    [Fact]
    public void Detect_Tama5_ThrowsNotSupportedException()
    {
        var rom = CreateRom(0xFD, 0x01, 0x02); // TAMA5

        var exception = Assert.Throws<NotSupportedException>(() => Cartridge.Detect(rom));
        Assert.Contains("TAMA5 is not yet supported", exception.Message);
        Assert.Contains("0xFD", exception.Message);
    }

    [Fact]
    public void Detect_NullRom_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => Cartridge.Detect(null!));
        Assert.Contains("ROM too small to contain valid header", exception.Message);
        Assert.Equal("rom", exception.ParamName);
    }

    [Theory]
    [InlineData(0xAA)]
    [InlineData(0xBB)]
    [InlineData(0xCC)]
    [InlineData(0x99)]
    public void Detect_UnknownCartridgeTypes_ThrowsNotSupportedException(byte cartridgeType)
    {
        var rom = CreateRom(cartridgeType, 0x00, 0x00);

        var exception = Assert.Throws<NotSupportedException>(() => Cartridge.Detect(rom));
        Assert.Contains("Unknown or unsupported cartridge type", exception.Message);
        Assert.Contains($"0x{cartridgeType:X2}", exception.Message);
    }

    /// <summary>
    /// Creates a test ROM with the specified cartridge type, ROM size, and RAM size.
    /// </summary>
    private static byte[] CreateRom(byte cartridgeType, byte romSizeCode, byte ramSizeCode)
    {
        // Calculate ROM size based on code
        int romSize = romSizeCode switch
        {
            0x00 => 32 * 1024,
            0x01 => 64 * 1024,
            0x02 => 128 * 1024,
            0x03 => 256 * 1024,
            0x04 => 512 * 1024,
            0x05 => 1024 * 1024,
            0x06 => 2048 * 1024,
            0x07 => 4096 * 1024,
            0x08 => 8192 * 1024,
            _ => 32 * 1024
        };

        var rom = new byte[romSize];

        // Set header values
        rom[0x0147] = cartridgeType;
        rom[0x0148] = romSizeCode;
        rom[0x0149] = ramSizeCode;

        return rom;
    }

    /// <summary>
    /// Creates a test ROM with bank markers for testing banking functionality.
    /// Each ROM bank has a distinctive marker at the start.
    /// </summary>
    protected static byte[] CreateRomWithBankMarkers(int numBanks)
    {
        var rom = new byte[numBanks * 0x4000]; // 16KB per bank

        for (int bank = 0; bank < numBanks; bank++)
        {
            int bankStart = bank * 0x4000;

            // Put bank number at start of each bank for easy identification
            rom[bankStart] = (byte)(0x10 + bank); // Bank markers: 0x10, 0x11, 0x12, etc.

            // Fill rest of bank with distinctive pattern
            for (int i = 1; i < 0x4000; i++)
            {
                rom[bankStart + i] = (byte)(bank & 0xFF);
            }
        }

        // Set header for MBC1 with appropriate ROM size
        byte romSizeCode = numBanks switch
        {
            2 => 0x00,   // 32KB
            4 => 0x01,   // 64KB
            8 => 0x02,   // 128KB
            16 => 0x03,  // 256KB
            32 => 0x04,  // 512KB
            64 => 0x05,  // 1MB
            128 => 0x06, // 2MB
            _ => 0x01    // Default to 64KB
        };

        rom[0x0147] = 0x01; // MBC1
        rom[0x0148] = romSizeCode;
        rom[0x0149] = 0x02; // 8KB RAM

        return rom;
    }
}