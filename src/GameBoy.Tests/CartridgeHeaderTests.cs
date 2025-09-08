using System;
using GameBoy.Core;

namespace GameBoy.Tests;

/// <summary>
/// Tests for cartridge header parsing functionality.
/// </summary>
public class CartridgeHeaderTests
{
    [Fact]
    public void Parse_ValidRom_ParsesHeaderCorrectly()
    {
        // Create a minimal ROM with valid header
        var rom = new byte[0x8000]; // 32KB ROM

        // Set cartridge type to MBC1 (0x01)
        rom[0x0147] = 0x01;
        // Set ROM size to 32KB (0x00)
        rom[0x0148] = 0x00;
        // Set RAM size to 8KB (0x02)
        rom[0x0149] = 0x02;
        // Set destination code to Non-Japan (0x01)
        rom[0x014A] = 0x01;
        // Set old licensee code
        rom[0x014B] = 0x33;
        // Set mask ROM version
        rom[0x014C] = 0x00;
        // Set header checksum (calculated value)
        rom[0x014D] = 0x00;
        // Set global checksum
        rom[0x014E] = 0x00;
        rom[0x014F] = 0x00;

        var header = CartridgeHeader.Parse(rom);

        Assert.Equal(0x01, header.CartridgeType);
        Assert.Equal(0x00, header.RomSizeCode);
        Assert.Equal(0x02, header.RamSizeCode);
        Assert.Equal(0x01, header.DestinationCode);
        Assert.Equal(0x33, header.OldLicenseeCode);
        Assert.Equal(0x00, header.MaskRomVersion);
        Assert.Equal(32 * 1024, header.RomSize);
        Assert.Equal(8 * 1024, header.RamSize);
        Assert.Equal(2, header.RomBankCount);
        Assert.Equal(1, header.RamBankCount);
    }

    [Fact]
    public void Parse_TooSmallRom_ThrowsException()
    {
        var smallRom = new byte[0x100]; // Too small to contain header

        Assert.Throws<ArgumentException>(() => CartridgeHeader.Parse(smallRom));
    }

    [Fact]
    public void GetMbcType_ReturnsCorrectMbcForKnownTypes()
    {
        var rom = new byte[0x8000];

        // Test MBC0 (ROM only)
        rom[0x0147] = 0x00;
        var header = CartridgeHeader.Parse(rom);
        Assert.Equal(MbcType.None, header.GetMbcType());

        // Test MBC1
        rom[0x0147] = 0x01;
        header = CartridgeHeader.Parse(rom);
        Assert.Equal(MbcType.Mbc1, header.GetMbcType());

        // Test MBC1+RAM+BATTERY
        rom[0x0147] = 0x03;
        header = CartridgeHeader.Parse(rom);
        Assert.Equal(MbcType.Mbc1, header.GetMbcType());

        // Test MBC3
        rom[0x0147] = 0x11;
        header = CartridgeHeader.Parse(rom);
        Assert.Equal(MbcType.Mbc3, header.GetMbcType());

        // Test MBC3+TIMER+BATTERY
        rom[0x0147] = 0x0F;
        header = CartridgeHeader.Parse(rom);
        Assert.Equal(MbcType.Mbc3, header.GetMbcType());

        // Test MBC5
        rom[0x0147] = 0x19;
        header = CartridgeHeader.Parse(rom);
        Assert.Equal(MbcType.Mbc5, header.GetMbcType());

        // Test MBC5+RUMBLE+RAM+BATTERY
        rom[0x0147] = 0x1E;
        header = CartridgeHeader.Parse(rom);
        Assert.Equal(MbcType.Mbc5, header.GetMbcType());

        // Test MBC4
        rom[0x0147] = 0x15;
        header = CartridgeHeader.Parse(rom);
        Assert.Equal(MbcType.Mbc4, header.GetMbcType());

        // Test MBC6
        rom[0x0147] = 0x20;
        header = CartridgeHeader.Parse(rom);
        Assert.Equal(MbcType.Mbc6, header.GetMbcType());

        // Test MBC7
        rom[0x0147] = 0x22;
        header = CartridgeHeader.Parse(rom);
        Assert.Equal(MbcType.Mbc7, header.GetMbcType());

        // Test HuC1
        rom[0x0147] = 0xFF;
        header = CartridgeHeader.Parse(rom);
        Assert.Equal(MbcType.HuC1, header.GetMbcType());

        // Test HuC3
        rom[0x0147] = 0xFE;
        header = CartridgeHeader.Parse(rom);
        Assert.Equal(MbcType.HuC3, header.GetMbcType());

        // Test TAMA5
        rom[0x0147] = 0xFD;
        header = CartridgeHeader.Parse(rom);
        Assert.Equal(MbcType.Tama5, header.GetMbcType());

        // Test unknown type
        rom[0x0147] = 0x99;
        header = CartridgeHeader.Parse(rom);
        Assert.Equal(MbcType.Unknown, header.GetMbcType());
    }

    [Fact]
    public void HasBattery_ReturnsCorrectValueForBatteryTypes()
    {
        var rom = new byte[0x8000];

        // Test types without battery
        rom[0x0147] = 0x00; // ROM only
        var header = CartridgeHeader.Parse(rom);
        Assert.False(header.HasBattery());

        rom[0x0147] = 0x01; // MBC1
        header = CartridgeHeader.Parse(rom);
        Assert.False(header.HasBattery());

        // Test types with battery
        rom[0x0147] = 0x03; // MBC1+RAM+BATTERY
        header = CartridgeHeader.Parse(rom);
        Assert.True(header.HasBattery());

        rom[0x0147] = 0x06; // MBC2+BATTERY
        header = CartridgeHeader.Parse(rom);
        Assert.True(header.HasBattery());

        rom[0x0147] = 0x0F; // MBC3+TIMER+BATTERY
        header = CartridgeHeader.Parse(rom);
        Assert.True(header.HasBattery());

        rom[0x0147] = 0x1B; // MBC5+RAM+BATTERY
        header = CartridgeHeader.Parse(rom);
        Assert.True(header.HasBattery());
    }

    [Fact]
    public void HasRtc_ReturnsCorrectValueForRtcTypes()
    {
        var rom = new byte[0x8000];

        // Test types without RTC
        rom[0x0147] = 0x11; // MBC3
        var header = CartridgeHeader.Parse(rom);
        Assert.False(header.HasRtc());

        rom[0x0147] = 0x13; // MBC3+RAM+BATTERY
        header = CartridgeHeader.Parse(rom);
        Assert.False(header.HasRtc());

        // Test types with RTC
        rom[0x0147] = 0x0F; // MBC3+TIMER+BATTERY
        header = CartridgeHeader.Parse(rom);
        Assert.True(header.HasRtc());

        rom[0x0147] = 0x10; // MBC3+TIMER+RAM+BATTERY
        header = CartridgeHeader.Parse(rom);
        Assert.True(header.HasRtc());
    }

    [Theory]
    [InlineData(0x00, 32 * 1024)]    // 32KB
    [InlineData(0x01, 64 * 1024)]    // 64KB
    [InlineData(0x02, 128 * 1024)]   // 128KB
    [InlineData(0x03, 256 * 1024)]   // 256KB
    [InlineData(0x04, 512 * 1024)]   // 512KB
    [InlineData(0x05, 1024 * 1024)]  // 1MB
    [InlineData(0x06, 2048 * 1024)]  // 2MB
    [InlineData(0x07, 4096 * 1024)]  // 4MB
    [InlineData(0x08, 8192 * 1024)]  // 8MB
    public void RomSize_CalculatesCorrectSizeForStandardCodes(byte code, int expectedSize)
    {
        var rom = new byte[0x8000];
        rom[0x0148] = code;

        var header = CartridgeHeader.Parse(rom);

        Assert.Equal(expectedSize, header.RomSize);
        Assert.Equal(expectedSize / 0x4000, header.RomBankCount);
    }

    [Theory]
    [InlineData(0x00, 0)]            // No RAM
    [InlineData(0x01, 2 * 1024)]     // 2KB
    [InlineData(0x02, 8 * 1024)]     // 8KB
    [InlineData(0x03, 32 * 1024)]    // 32KB
    [InlineData(0x04, 128 * 1024)]   // 128KB
    [InlineData(0x05, 64 * 1024)]    // 64KB
    public void RamSize_CalculatesCorrectSizeForStandardCodes(byte code, int expectedSize)
    {
        var rom = new byte[0x8000];
        rom[0x0149] = code;

        var header = CartridgeHeader.Parse(rom);

        Assert.Equal(expectedSize, header.RamSize);
        if (expectedSize > 0)
        {
            Assert.Equal(Math.Max(1, expectedSize / 0x2000), header.RamBankCount);
        }
        else
        {
            Assert.Equal(0, header.RamBankCount);
        }
    }
}