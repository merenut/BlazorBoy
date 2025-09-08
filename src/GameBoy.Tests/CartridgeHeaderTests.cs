using System;
using GameBoy.Core;
using Xunit;

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

        // Set title
        var titleBytes = System.Text.Encoding.ASCII.GetBytes("TEST GAME");
        Array.Copy(titleBytes, 0, rom, 0x0134, titleBytes.Length);

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

        Assert.Equal("TEST GAME", header.Title);
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

    [Fact]
    public void Title_ExtractsCorrectTitle()
    {
        var rom = new byte[0x8000];

        // Test normal title
        var titleBytes = System.Text.Encoding.ASCII.GetBytes("SUPER MARIO");
        Array.Copy(titleBytes, 0, rom, 0x0134, titleBytes.Length);

        var header = CartridgeHeader.Parse(rom);
        Assert.Equal("SUPER MARIO", header.Title);
    }

    [Fact]
    public void Title_HandlesNullTermination()
    {
        var rom = new byte[0x8000];

        // Test title with null termination
        var titleBytes = System.Text.Encoding.ASCII.GetBytes("TETRIS\0\0\0\0\0\0\0\0\0\0");
        Array.Copy(titleBytes, 0, rom, 0x0134, titleBytes.Length);

        var header = CartridgeHeader.Parse(rom);
        Assert.Equal("TETRIS", header.Title);
    }

    [Fact]
    public void Title_HandlesEmptyTitle()
    {
        var rom = new byte[0x8000];

        // Test empty title (all zeros)
        for (int i = 0; i < 16; i++)
        {
            rom[0x0134 + i] = 0;
        }

        var header = CartridgeHeader.Parse(rom);
        Assert.Equal("", header.Title);
    }

    [Fact]
    public void Title_HandlesNonPrintableCharacters()
    {
        var rom = new byte[0x8000];

        // Test title with non-printable characters
        rom[0x0134] = (byte)'T';
        rom[0x0135] = (byte)'E';
        rom[0x0136] = 0x01; // Control character
        rom[0x0137] = (byte)'S';
        rom[0x0138] = (byte)'T';

        var header = CartridgeHeader.Parse(rom);
        Assert.Equal("TE ST", header.Title);
    }

    [Fact]
    public void IsValidHeader_ValidHeader_ReturnsTrue()
    {
        var rom = new byte[0x8000];
        rom[0x0147] = 0x01; // Valid cartridge type
        rom[0x0148] = 0x02; // Valid ROM size
        rom[0x0149] = 0x02; // Valid RAM size
        rom[0x014A] = 0x01; // Valid destination code

        var header = CartridgeHeader.Parse(rom);
        Assert.True(header.IsValidHeader());
    }

    [Fact]
    public void IsValidHeader_InvalidRomSize_ReturnsFalse()
    {
        var rom = new byte[0x8000];
        rom[0x0148] = 0x99; // Invalid ROM size code
        rom[0x0149] = 0x02; // Valid RAM size
        rom[0x014A] = 0x01; // Valid destination code

        var header = CartridgeHeader.Parse(rom);
        Assert.False(header.IsValidHeader());
    }

    [Fact]
    public void IsValidHeader_InvalidRamSize_ReturnsFalse()
    {
        var rom = new byte[0x8000];
        rom[0x0148] = 0x02; // Valid ROM size
        rom[0x0149] = 0x99; // Invalid RAM size code
        rom[0x014A] = 0x01; // Valid destination code

        var header = CartridgeHeader.Parse(rom);
        Assert.False(header.IsValidHeader());
    }

    [Fact]
    public void IsValidHeader_InvalidDestinationCode_ReturnsFalse()
    {
        var rom = new byte[0x8000];
        rom[0x0148] = 0x02; // Valid ROM size
        rom[0x0149] = 0x02; // Valid RAM size
        rom[0x014A] = 0x99; // Invalid destination code

        var header = CartridgeHeader.Parse(rom);
        Assert.False(header.IsValidHeader());
    }

    [Fact]
    public void Parse_NullRom_ThrowsArgumentException()
    {
        byte[]? nullRom = null;
        Assert.Throws<ArgumentException>(() => CartridgeHeader.Parse(nullRom!));
    }

    [Theory]
    [InlineData(0x52, 1152 * 1024)]  // 1.1MB (72 banks)
    [InlineData(0x53, 1280 * 1024)]  // 1.2MB (80 banks)
    [InlineData(0x54, 1536 * 1024)]  // 1.5MB (96 banks)
    public void RomSize_CalculatesCorrectSizeForSpecialCodes(byte code, int expectedSize)
    {
        var rom = new byte[0x8000];
        rom[0x0148] = code;

        var header = CartridgeHeader.Parse(rom);

        Assert.Equal(expectedSize, header.RomSize);
        Assert.Equal(expectedSize / 0x4000, header.RomBankCount);
    }

    [Theory]
    [InlineData(0x09)]  // Invalid ROM size code
    [InlineData(0x51)]  // Invalid ROM size code
    [InlineData(0x55)]  // Invalid ROM size code
    [InlineData(0xFF)]  // Invalid ROM size code
    public void RomSize_InvalidCodes_DefaultsTo32KB(byte code)
    {
        var rom = new byte[0x8000];
        rom[0x0148] = code;

        var header = CartridgeHeader.Parse(rom);

        Assert.Equal(32 * 1024, header.RomSize);
        Assert.Equal(2, header.RomBankCount);
    }

    [Theory]
    [InlineData(0x06)]  // Invalid RAM size code
    [InlineData(0x99)]  // Invalid RAM size code
    [InlineData(0xFF)]  // Invalid RAM size code
    public void RamSize_InvalidCodes_DefaultsToZero(byte code)
    {
        var rom = new byte[0x8000];
        rom[0x0149] = code;

        var header = CartridgeHeader.Parse(rom);

        Assert.Equal(0, header.RamSize);
        Assert.Equal(0, header.RamBankCount);
    }

    [Fact]
    public void IsHeaderChecksumValid_ValidChecksum_ReturnsTrue()
    {
        var rom = new byte[0x8000];

        // Set up a header with known values
        var titleBytes = System.Text.Encoding.ASCII.GetBytes("TETRIS");
        Array.Copy(titleBytes, 0, rom, 0x0134, titleBytes.Length);
        rom[0x0147] = 0x01; // MBC1
        rom[0x0148] = 0x00; // 32KB ROM
        rom[0x0149] = 0x00; // No RAM
        rom[0x014A] = 0x01; // Non-Japan
        rom[0x014B] = 0x33; // Old licensee
        rom[0x014C] = 0x00; // Version

        // Calculate the correct checksum
        byte checksum = 0;
        for (int i = 0x0134; i <= 0x014C; i++)
        {
            checksum = (byte)(checksum - rom[i] - 1);
        }
        rom[0x014D] = checksum;

        var header = CartridgeHeader.Parse(rom);
        Assert.True(header.IsHeaderChecksumValid(rom));
    }

    [Fact]
    public void IsHeaderChecksumValid_InvalidChecksum_ReturnsFalse()
    {
        var rom = new byte[0x8000];

        // Set up a header with known values
        rom[0x0147] = 0x01; // MBC1
        rom[0x0148] = 0x00; // 32KB ROM
        rom[0x0149] = 0x00; // No RAM
        rom[0x014D] = 0x42; // Incorrect checksum

        var header = CartridgeHeader.Parse(rom);
        Assert.False(header.IsHeaderChecksumValid(rom));
    }

    [Fact]
    public void GlobalChecksum_ParsedCorrectly()
    {
        var rom = new byte[0x8000];
        rom[0x014E] = 0x34; // Low byte
        rom[0x014F] = 0x12; // High byte

        var header = CartridgeHeader.Parse(rom);
        Assert.Equal(0x1234, header.GlobalChecksum);
    }

    [Theory]
    [InlineData(0x02)]  // Invalid destination code
    [InlineData(0xFF)]  // Invalid destination code
    public void IsValidHeader_InvalidDestinationCodes_ReturnsFalse(byte destinationCode)
    {
        var rom = new byte[0x8000];
        rom[0x0148] = 0x02; // Valid ROM size
        rom[0x0149] = 0x02; // Valid RAM size
        rom[0x014A] = destinationCode;

        var header = CartridgeHeader.Parse(rom);
        Assert.False(header.IsValidHeader());
    }

    [Theory]
    [InlineData(0x09)]  // Invalid ROM size (beyond standard range)
    [InlineData(0x51)]  // Invalid ROM size
    [InlineData(0x55)]  // Invalid ROM size
    public void IsValidHeader_InvalidRomSizeCodes_ReturnsFalse(byte romSizeCode)
    {
        var rom = new byte[0x8000];
        rom[0x0148] = romSizeCode;
        rom[0x0149] = 0x02; // Valid RAM size
        rom[0x014A] = 0x01; // Valid destination code

        var header = CartridgeHeader.Parse(rom);
        Assert.False(header.IsValidHeader());
    }

    [Fact]
    public void Title_MaxLength16Characters_TruncatesCorrectly()
    {
        var rom = new byte[0x8000];

        // Set up title with exactly 16 characters (no null termination)
        var titleBytes = System.Text.Encoding.ASCII.GetBytes("ABCDEFGHIJKLMNOP");
        Array.Copy(titleBytes, 0, rom, 0x0134, 16);

        var header = CartridgeHeader.Parse(rom);
        Assert.Equal("ABCDEFGHIJKLMNOP", header.Title);
    }

    [Fact]
    public void Title_WithSpecialCharacters_HandlesCorrectly()
    {
        var rom = new byte[0x8000];

        // Test with various special characters
        rom[0x0134] = (byte)'M';
        rom[0x0135] = (byte)'A';
        rom[0x0136] = (byte)'R';
        rom[0x0137] = (byte)'I';
        rom[0x0138] = (byte)'O';
        rom[0x0139] = 0xFF; // Non-ASCII character (not control, so preserved)
        rom[0x013A] = (byte)'3';

        var header = CartridgeHeader.Parse(rom);
        Assert.Equal("MARIOÿ3", header.Title); // 0xFF is preserved as ÿ
    }
}