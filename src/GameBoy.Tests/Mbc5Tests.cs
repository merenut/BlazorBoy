using System;
using GameBoy.Core;

namespace GameBoy.Tests;

/// <summary>
/// Tests for MBC5 (Memory Bank Controller 5) functionality.
/// </summary>
public class Mbc5Tests
{
    [Fact]
    public void ReadRom_Bank0_ReadsFromFixedBank()
    {
        var rom = CreateRomWithBankMarkers(16); // 16 banks (256KB)
        var header = CartridgeHeader.Parse(rom);
        var mbc5 = new Mbc5(rom, header);

        var value = mbc5.ReadRom(0x0000);
        Assert.Equal(0x10, value); // Bank 0 marker

        var value2 = mbc5.ReadRom(0x3FFF);
        Assert.Equal(0x00, value2); // Bank 0 fill value
    }

    [Fact]
    public void ReadRom_BankX_ReadsFromSwitchableBank()
    {
        var rom = CreateRomWithBankMarkers(16); // 16 banks (256KB)
        var header = CartridgeHeader.Parse(rom);
        var mbc5 = new Mbc5(rom, header);

        // Default should be bank 0 (unlike MBC1/MBC3)
        var value = mbc5.ReadRom(0x4000);
        Assert.Equal(0x10, value); // Bank 0 marker

        // Switch to bank 1
        mbc5.WriteRom(0x2000, 0x01);
        value = mbc5.ReadRom(0x4000);
        Assert.Equal(0x11, value); // Bank 1 marker

        // Switch to bank 15
        mbc5.WriteRom(0x2000, 0x0F);
        value = mbc5.ReadRom(0x4000);
        Assert.Equal(0x1F, value); // Bank 15 marker
    }

    [Fact]
    public void WriteRom_Bank0Selection_AllowsBank0()
    {
        var rom = CreateRomWithBankMarkers(8);
        var header = CartridgeHeader.Parse(rom);
        var mbc5 = new Mbc5(rom, header);

        // MBC5 allows bank 0 to be selected (unlike MBC1/MBC3)
        mbc5.WriteRom(0x2000, 0x00);
        var value = mbc5.ReadRom(0x4000);
        Assert.Equal(0x10, value); // Should read bank 0 marker
    }

    [Fact]
    public void WriteRom_9BitRomBanking_WorksCorrectly()
    {
        var rom = CreateRomWithBankMarkers(512); // 512 banks (8MB) - maximum for MBC5
        var header = CartridgeHeader.Parse(rom);
        var mbc5 = new Mbc5(rom, header);

        // Test lower 8 bits
        mbc5.WriteRom(0x2000, 0x55); // Set lower 8 bits to 0x55
        var value = mbc5.ReadRom(0x4000);
        Assert.Equal((byte)(0x10 + 0x55), value); // Bank 0x55 marker

        // Test upper bit (9th bit)
        mbc5.WriteRom(0x3000, 0x01); // Set bit 8 (9th bit)
        value = mbc5.ReadRom(0x4000);
        var expectedBank = 0x100 | 0x55; // 256 + 85 = 341
        Assert.Equal((byte)(0x10 + (expectedBank & 0xFF)), value);

        // Test clearing upper bit
        mbc5.WriteRom(0x3000, 0x00); // Clear bit 8
        value = mbc5.ReadRom(0x4000);
        Assert.Equal((byte)(0x10 + 0x55), value); // Back to bank 0x55

        // Test high bank number (bank 300)
        mbc5.WriteRom(0x2000, 0x2C); // Lower 8 bits = 44
        mbc5.WriteRom(0x3000, 0x01); // Upper bit = 1
        // Bank = 256 + 44 = 300
        value = mbc5.ReadRom(0x4000);
        Assert.Equal((byte)(0x10 + 44), value); // Bank 300 marker (truncated to fit byte)
    }

    [Fact]
    public void WriteRom_RamEnable_EnablesRamAccess()
    {
        var rom = CreateRom(0x1B, 0x03, 0x04); // MBC5+RAM+BATTERY, 256KB ROM, 128KB RAM
        var header = CartridgeHeader.Parse(rom);
        var mbc5 = new Mbc5(rom, header);

        // RAM should be disabled by default
        mbc5.WriteExternalRam(0xA000, 0x42);
        Assert.Equal(0xFF, mbc5.ReadExternalRam(0xA000));

        // Enable RAM
        mbc5.WriteRom(0x0000, 0x0A);
        mbc5.WriteExternalRam(0xA000, 0x42);
        Assert.Equal(0x42, mbc5.ReadExternalRam(0xA000));

        // Disable RAM
        mbc5.WriteRom(0x0000, 0x00);
        Assert.Equal(0xFF, mbc5.ReadExternalRam(0xA000));
    }

    [Fact]
    public void ExternalRam_4BitRamBanking_WorksCorrectly()
    {
        var rom = CreateRom(0x1B, 0x03, 0x04); // MBC5+RAM+BATTERY, 256KB ROM, 128KB RAM
        var header = CartridgeHeader.Parse(rom);
        var mbc5 = new Mbc5(rom, header);

        // Enable RAM
        mbc5.WriteRom(0x0000, 0x0A);

        // Write to different RAM banks (0-15)
        for (int bank = 0; bank < 16; bank++)
        {
            mbc5.WriteRom(0x4000, (byte)bank);
            mbc5.WriteExternalRam(0xA000, (byte)(0x20 + bank));
        }

        // Read back from different banks
        for (int bank = 0; bank < 16; bank++)
        {
            mbc5.WriteRom(0x4000, (byte)bank);
            Assert.Equal((byte)(0x20 + bank), mbc5.ReadExternalRam(0xA000));
        }
    }

    [Fact]
    public void ExternalRam_BasicReadWrite()
    {
        var rom = CreateRom(0x1B, 0x03, 0x03); // MBC5+RAM+BATTERY, 256KB ROM, 32KB RAM
        var header = CartridgeHeader.Parse(rom);
        var mbc5 = new Mbc5(rom, header);

        // Enable RAM
        mbc5.WriteRom(0x0000, 0x0A);

        // Test basic read/write
        mbc5.WriteExternalRam(0xA000, 0x12);
        mbc5.WriteExternalRam(0xA001, 0x34);
        mbc5.WriteExternalRam(0xBFFF, 0x56);

        Assert.Equal(0x12, mbc5.ReadExternalRam(0xA000));
        Assert.Equal(0x34, mbc5.ReadExternalRam(0xA001));
        Assert.Equal(0x56, mbc5.ReadExternalRam(0xBFFF));
    }

    [Fact]
    public void ExternalRam_OutOfBounds_ReturnsFF()
    {
        var rom = CreateRom(0x1A, 0x03, 0x02); // MBC5+RAM, 256KB ROM, 8KB RAM
        var header = CartridgeHeader.Parse(rom);
        var mbc5 = new Mbc5(rom, header);

        // Enable RAM
        mbc5.WriteRom(0x0000, 0x0A);

        // Try to access RAM bank beyond what's available
        mbc5.WriteRom(0x4000, 0x05); // Bank 5, but we only have 8KB (1 bank)
        Assert.Equal(0xFF, mbc5.ReadExternalRam(0xA000));

        // Writes to out-of-bounds should be ignored
        mbc5.WriteExternalRam(0xA000, 0x42);
        Assert.Equal(0xFF, mbc5.ReadExternalRam(0xA000));
    }

    [Fact]
    public void BatteryInterface_GetExternalRam_ReturnsDataIfModified()
    {
        var rom = CreateRom(0x1B, 0x03, 0x03); // MBC5+RAM+BATTERY
        var header = CartridgeHeader.Parse(rom);
        var mbc5 = new Mbc5(rom, header);

        // Initially should return null (no data to save)
        Assert.Null(mbc5.GetExternalRam());

        // Enable RAM and write some data
        mbc5.WriteRom(0x0000, 0x0A);
        mbc5.WriteExternalRam(0xA000, 0x42);

        // Should now return the RAM data
        var ramData = mbc5.GetExternalRam();
        Assert.NotNull(ramData);
        Assert.Equal(32 * 1024, ramData.Length);
        Assert.Equal(0x42, ramData[0]);
    }

    [Fact]
    public void BatteryInterface_LoadExternalRam_LoadsSavedData()
    {
        var rom = CreateRom(0x1B, 0x03, 0x03); // MBC5+RAM+BATTERY
        var header = CartridgeHeader.Parse(rom);
        var mbc5 = new Mbc5(rom, header);

        // Create some saved RAM data
        var savedData = new byte[32 * 1024];
        savedData[0] = 0x84;
        savedData[100] = 0x21;

        // Load the saved data
        mbc5.LoadExternalRam(savedData);

        // Enable RAM and verify the data was loaded
        mbc5.WriteRom(0x0000, 0x0A);
        Assert.Equal(0x84, mbc5.ReadExternalRam(0xA000));
        Assert.Equal(0x21, mbc5.ReadExternalRam(0xA064)); // 0xA000 + 100
    }

    [Fact]
    public void BatteryInterface_NoBatteryCartridge_ReturnsNull()
    {
        var rom = CreateRom(0x19, 0x03, 0x03); // MBC5 without battery
        var header = CartridgeHeader.Parse(rom);
        var mbc5 = new Mbc5(rom, header);

        // Should always return null for non-battery cartridges
        mbc5.WriteRom(0x0000, 0x0A);
        mbc5.WriteExternalRam(0xA000, 0x42);
        Assert.Null(mbc5.GetExternalRam());
    }

    [Fact]
    public void WriteRom_UnusedRanges_IgnoredCorrectly()
    {
        var rom = CreateRomWithBankMarkers(16);
        var header = CartridgeHeader.Parse(rom);
        var mbc5 = new Mbc5(rom, header);

        // MBC5 doesn't use 0x6000-0x7FFF range (unlike MBC1/MBC3)
        // These writes should be ignored and not affect banking

        // Set a known bank
        mbc5.WriteRom(0x2000, 0x05);
        var initialValue = mbc5.ReadRom(0x4000);
        Assert.Equal(0x15, initialValue); // Bank 5 marker

        // Write to unused range
        mbc5.WriteRom(0x6000, 0xFF);
        mbc5.WriteRom(0x7000, 0x42);

        // Bank should remain unchanged
        var unchangedValue = mbc5.ReadRom(0x4000);
        Assert.Equal(0x15, unchangedValue); // Still bank 5
    }

    /// <summary>
    /// Creates a test ROM with the specified cartridge type, ROM size, and RAM size.
    /// </summary>
    private static byte[] CreateRom(byte cartridgeType, byte romSizeCode, byte ramSizeCode)
    {
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
        rom[0x0147] = cartridgeType;
        rom[0x0148] = romSizeCode;
        rom[0x0149] = ramSizeCode;

        return rom;
    }

    /// <summary>
    /// Creates a test ROM with bank markers for testing banking functionality.
    /// </summary>
    private static byte[] CreateRomWithBankMarkers(int numBanks)
    {
        var rom = new byte[numBanks * 0x4000];

        for (int bank = 0; bank < numBanks; bank++)
        {
            int bankStart = bank * 0x4000;
            rom[bankStart] = (byte)(0x10 + bank);

            for (int i = 1; i < 0x4000; i++)
            {
                rom[bankStart + i] = (byte)(bank & 0xFF);
            }
        }

        byte romSizeCode = numBanks switch
        {
            2 => 0x00,
            4 => 0x01,
            8 => 0x02,
            16 => 0x03,
            32 => 0x04,
            64 => 0x05,
            128 => 0x06,
            256 => 0x07,
            512 => 0x08,
            _ => 0x01
        };

        rom[0x0147] = 0x19; // MBC5
        rom[0x0148] = romSizeCode;
        rom[0x0149] = 0x03; // 32KB RAM

        return rom;
    }
}