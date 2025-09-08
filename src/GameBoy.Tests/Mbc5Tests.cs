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

    [Fact]
    public void ReadRom_BeyondAvailableSize_ReturnsFF()
    {
        var rom = CreateRomWithBankMarkers(16); // 16 banks (256KB)
        var header = CartridgeHeader.Parse(rom);
        var mbc5 = new Mbc5(rom, header);

        // Try to access beyond ROM size with 9-bit banking
        mbc5.WriteRom(0x2000, 0xFF); // Lower 8 bits = 255
        mbc5.WriteRom(0x3000, 0x01); // Upper bit = 1, so bank = 256 + 255 = 511
        var value = mbc5.ReadRom(0x4000);

        // Should return 0xFF for out-of-bounds access
        Assert.Equal(0xFF, value);
    }

    [Fact]
    public void WriteRom_MaxBankNumber_WorksAtLimits()
    {
        var rom = CreateRomWithBankMarkers(512); // 512 banks (8MB) - MBC5 maximum
        var header = CartridgeHeader.Parse(rom);
        var mbc5 = new Mbc5(rom, header);

        // Test maximum bank selection (511)
        mbc5.WriteRom(0x2000, 0xFF); // Lower 8 bits = 255
        mbc5.WriteRom(0x3000, 0x01); // Upper bit = 1
        // Total bank = (1 << 8) | 255 = 256 + 255 = 511
        var value = mbc5.ReadRom(0x4000);
        Assert.Equal(0x0F, value); // Bank 511 marker: 0x10 + (511 & 0xFF) = 0x10 + 0xFF = 0x0F (wrapped)
    }

    [Fact]
    public void WriteRom_9BitBanking_FullRangeTest()
    {
        var rom = CreateRomWithBankMarkers(512); // Full 512 banks
        var header = CartridgeHeader.Parse(rom);
        var mbc5 = new Mbc5(rom, header);

        // Test various 9-bit combinations
        var testCases = new[]
        {
            (lower: 0x00, upper: 0x00, expectedBank: 0),     // Bank 0
            (lower: 0x01, upper: 0x00, expectedBank: 1),     // Bank 1
            (lower: 0xFF, upper: 0x00, expectedBank: 255),   // Bank 255
            (lower: 0x00, upper: 0x01, expectedBank: 256),   // Bank 256
            (lower: 0x01, upper: 0x01, expectedBank: 257),   // Bank 257
            (lower: 0xFF, upper: 0x01, expectedBank: 511),   // Bank 511 (maximum)
        };

        foreach (var (lower, upper, expectedBank) in testCases)
        {
            mbc5.WriteRom(0x2000, (byte)lower);
            mbc5.WriteRom(0x3000, (byte)upper);
            var value = mbc5.ReadRom(0x4000);
            Assert.Equal((byte)(0x10 + (expectedBank & 0xFF)), value);
        }
    }

    [Fact]
    public void WriteRom_UpperBitMasking_OnlyBit0Used()
    {
        var rom = CreateRomWithBankMarkers(512);
        var header = CartridgeHeader.Parse(rom);
        var mbc5 = new Mbc5(rom, header);

        // Test that only bit 0 of upper register is used
        mbc5.WriteRom(0x2000, 0x10); // Bank 16
        mbc5.WriteRom(0x3000, 0xFF); // All bits set, but only bit 0 should matter

        var value = mbc5.ReadRom(0x4000);
        // Should be bank 256 + 16 = 272
        Assert.Equal((byte)(0x10 + 16), value); // Bank 272 marker (truncated)

        // Test with only bit 0 set
        mbc5.WriteRom(0x3000, 0x01);
        value = mbc5.ReadRom(0x4000);
        Assert.Equal((byte)(0x10 + 16), value); // Should be the same
    }

    [Fact]
    public void ExternalRam_Max16Banks_WorksCorrectly()
    {
        var rom = CreateRom(0x1B, 0x03, 0x04); // MBC5+RAM+BATTERY, 256KB ROM, 128KB RAM (16 banks)
        var header = CartridgeHeader.Parse(rom);
        var mbc5 = new Mbc5(rom, header);

        // Enable RAM
        mbc5.WriteRom(0x0000, 0x0A);

        // Test all 16 RAM banks (0-15)
        for (int bank = 0; bank < 16; bank++)
        {
            mbc5.WriteRom(0x4000, (byte)bank);
            mbc5.WriteExternalRam(0xA000, (byte)(0x60 + bank));
        }

        // Verify data in each bank
        for (int bank = 0; bank < 16; bank++)
        {
            mbc5.WriteRom(0x4000, (byte)bank);
            Assert.Equal((byte)(0x60 + bank), mbc5.ReadExternalRam(0xA000));
        }
    }

    [Fact]
    public void ExternalRam_4BitBankMasking_OnlyLower4BitsUsed()
    {
        var rom = CreateRom(0x1B, 0x03, 0x04); // MBC5+RAM+BATTERY, 128KB RAM
        var header = CartridgeHeader.Parse(rom);
        var mbc5 = new Mbc5(rom, header);

        // Enable RAM
        mbc5.WriteRom(0x0000, 0x0A);

        // Write with upper bits set (should be ignored)
        mbc5.WriteRom(0x4000, 0xFF); // All bits set
        mbc5.WriteExternalRam(0xA000, 0x42);

        // Should only use lower 4 bits: 0xFF & 0x0F = 0x0F = 15
        mbc5.WriteRom(0x4000, 0x0F); // Bank 15 explicitly
        Assert.Equal(0x42, mbc5.ReadExternalRam(0xA000));
    }

    [Fact]
    public void ExternalRam_BeyondAvailableBanks_HandledGracefully()
    {
        var rom = CreateRom(0x1A, 0x03, 0x02); // MBC5+RAM, 256KB ROM, 8KB RAM (1 bank)
        var header = CartridgeHeader.Parse(rom);
        var mbc5 = new Mbc5(rom, header);

        // Enable RAM
        mbc5.WriteRom(0x0000, 0x0A);

        // Try to access banks beyond available (1-15 don't exist)
        for (int bank = 1; bank < 16; bank++)
        {
            mbc5.WriteRom(0x4000, (byte)bank);
            Assert.Equal(0xFF, mbc5.ReadExternalRam(0xA000));

            // Writes should be ignored
            mbc5.WriteExternalRam(0xA000, 0x42);
            Assert.Equal(0xFF, mbc5.ReadExternalRam(0xA000));
        }
    }

    [Fact]
    public void WriteRom_BoundaryAddresses_HandledCorrectly()
    {
        var rom = CreateRomWithBankMarkers(16);
        var header = CartridgeHeader.Parse(rom);
        var mbc5 = new Mbc5(rom, header);

        // Test boundary addresses for control registers

        // RAM enable boundary (0x0000-0x1FFF)
        mbc5.WriteRom(0x1FFF, 0x0A); // Should enable RAM
        mbc5.WriteExternalRam(0xA000, 0x42);
        Assert.Equal(0x42, mbc5.ReadExternalRam(0xA000));

        // Lower ROM bank boundary (0x2000-0x2FFF)
        mbc5.WriteRom(0x2FFF, 0x05); // Should select bank 5
        var value = mbc5.ReadRom(0x4000);
        Assert.Equal(0x15, value);

        // Upper ROM bank boundary (0x3000-0x3FFF)
        mbc5.WriteRom(0x3000, 0x01); // Should set upper bit
        value = mbc5.ReadRom(0x4000);
        // Bank should now be 256 + 5 = 261
        Assert.Equal(0xFF, value); // Out of bounds for 16-bank ROM

        // RAM bank boundary (0x4000-0x5FFF)
        mbc5.WriteRom(0x5FFF, 0x02); // Should select RAM bank 2
        mbc5.WriteExternalRam(0xA000, 0x33);
        mbc5.WriteRom(0x4000, 0x02); // Verify it's bank 2
        Assert.Equal(0x33, mbc5.ReadExternalRam(0xA000));
    }

    [Fact]
    public void ReadRom_LargeRomEdgeCases_HandledCorrectly()
    {
        var rom = CreateRomWithBankMarkers(256); // 256 banks (4MB)
        var header = CartridgeHeader.Parse(rom);
        var mbc5 = new Mbc5(rom, header);

        // Test reading from the last available bank
        mbc5.WriteRom(0x2000, 0xFF); // Bank 255
        mbc5.WriteRom(0x3000, 0x00); // No upper bit
        var value = mbc5.ReadRom(0x4000);
        Assert.Equal(0x0F, value); // Bank 255 marker: 0x10 + 255 = 0x10F, wrapped to 0x0F

        // Test reading just beyond available banks
        mbc5.WriteRom(0x2000, 0x00); // Bank 256 (0x100)
        mbc5.WriteRom(0x3000, 0x01); // Upper bit set
        value = mbc5.ReadRom(0x4000);
        Assert.Equal(0xFF, value); // Out of bounds
    }

    [Fact]
    public void BatteryInterface_MaxRamSize_HandledCorrectly()
    {
        var rom = CreateRom(0x1B, 0x08, 0x04); // MBC5+RAM+BATTERY, 8MB ROM, 128KB RAM
        var header = CartridgeHeader.Parse(rom);
        var mbc5 = new Mbc5(rom, header);

        // Enable RAM and write data across all banks
        mbc5.WriteRom(0x0000, 0x0A);

        // Write unique data to each RAM bank
        for (int bank = 0; bank < 16; bank++)
        {
            mbc5.WriteRom(0x4000, (byte)bank);
            mbc5.WriteExternalRam(0xA000, (byte)(bank * 2 + 1));
        }

        // Get external RAM data
        var ramData = mbc5.GetExternalRam();
        Assert.NotNull(ramData);
        Assert.Equal(128 * 1024, ramData.Length);

        // Verify data in first bank
        Assert.Equal(1, ramData[0]); // Bank 0 data

        // Verify data in second bank (starts at offset 8192)
        Assert.Equal(3, ramData[8192]); // Bank 1 data
    }

    [Fact]
    public void WriteRom_StressTest_MultipleRapidSwitches()
    {
        var rom = CreateRomWithBankMarkers(128);
        var header = CartridgeHeader.Parse(rom);
        var mbc5 = new Mbc5(rom, header);

        // Rapidly switch between banks and verify consistency
        for (int i = 0; i < 100; i++)
        {
            byte bank = (byte)(i % 128);
            mbc5.WriteRom(0x2000, bank);

            var value = mbc5.ReadRom(0x4000);
            Assert.Equal((byte)(0x10 + bank), value);
        }
    }
}