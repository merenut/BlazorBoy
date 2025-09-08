using System;
using GameBoy.Core;

namespace GameBoy.Tests;

/// <summary>
/// Tests for MBC1 (Memory Bank Controller 1) functionality.
/// </summary>
public class Mbc1Tests
{
    [Fact]
    public void ReadRom_Bank0_ReadsFromFixedBank()
    {
        var rom = CreateRomWithBankMarkers(4); // 4 banks (64KB)
        var header = CartridgeHeader.Parse(rom);
        var mbc1 = new Mbc1(rom, header);

        // Read from bank 0 area
        var value = mbc1.ReadRom(0x0000);
        Assert.Equal(0x10, value); // Bank 0 marker

        var value2 = mbc1.ReadRom(0x3FFF);
        Assert.Equal(0x00, value2); // Bank 0 fill value
    }

    [Fact]
    public void ReadRom_BankX_ReadsFromSwitchableBank()
    {
        var rom = CreateRomWithBankMarkers(4); // 4 banks (64KB)
        var header = CartridgeHeader.Parse(rom);
        var mbc1 = new Mbc1(rom, header);

        // Default should be bank 1
        var value = mbc1.ReadRom(0x4000);
        Assert.Equal(0x11, value); // Bank 1 marker

        // Switch to bank 2
        mbc1.WriteRom(0x2000, 0x02);
        value = mbc1.ReadRom(0x4000);
        Assert.Equal(0x12, value); // Bank 2 marker

        // Switch to bank 3
        mbc1.WriteRom(0x2000, 0x03);
        value = mbc1.ReadRom(0x4000);
        Assert.Equal(0x13, value); // Bank 3 marker
    }

    [Fact]
    public void WriteRom_Bank0Selection_MapsToBank1()
    {
        var rom = CreateRomWithBankMarkers(4); // 4 banks (64KB)
        var header = CartridgeHeader.Parse(rom);
        var mbc1 = new Mbc1(rom, header);

        // Try to select bank 0 - should map to bank 1
        mbc1.WriteRom(0x2000, 0x00);
        var value = mbc1.ReadRom(0x4000);
        Assert.Equal(0x11, value); // Should read bank 1 marker
    }

    [Fact]
    public void WriteRom_RamEnable_EnablesRamAccess()
    {
        var rom = CreateRom(0x03, 0x01, 0x02); // MBC1+RAM+BATTERY, 64KB ROM, 8KB RAM
        var header = CartridgeHeader.Parse(rom);
        var mbc1 = new Mbc1(rom, header);

        // RAM should be disabled by default
        mbc1.WriteExternalRam(0xA000, 0x42);
        Assert.Equal(0xFF, mbc1.ReadExternalRam(0xA000));

        // Enable RAM
        mbc1.WriteRom(0x0000, 0x0A);
        mbc1.WriteExternalRam(0xA000, 0x42);
        Assert.Equal(0x42, mbc1.ReadExternalRam(0xA000));

        // Disable RAM
        mbc1.WriteRom(0x0000, 0x00);
        Assert.Equal(0xFF, mbc1.ReadExternalRam(0xA000));
    }

    [Fact]
    public void WriteRom_AdvancedBankingMode_SwitchesMode()
    {
        var rom = CreateRomWithBankMarkers(64); // 64 banks (1MB)
        var header = CartridgeHeader.Parse(rom);
        var mbc1 = new Mbc1(rom, header);

        // Set upper ROM bank bits
        mbc1.WriteRom(0x4000, 0x01); // Should affect bank selection

        // In simple mode (default), this should affect ROM banking
        mbc1.WriteRom(0x2000, 0x01); // Select bank 1 in lower 5 bits
        var value = mbc1.ReadRom(0x4000);
        // Bank should be (0x01 << 5) | 0x01 = 0x21 = 33
        Assert.Equal(0x31, value); // Bank 33 marker (0x10 + 33)

        // Switch to advanced banking mode
        mbc1.WriteRom(0x6000, 0x01);

        // Now the upper bits should affect Bank 0 area too
        var bank0Value = mbc1.ReadRom(0x0000);
        // Bank 0 area should now map to bank 32 (0x01 << 5)
        Assert.Equal(0x30, bank0Value); // Bank 32 marker (0x10 + 32)
    }

    [Fact]
    public void ExternalRam_BasicReadWrite()
    {
        var rom = CreateRom(0x03, 0x01, 0x02); // MBC1+RAM+BATTERY, 64KB ROM, 8KB RAM
        var header = CartridgeHeader.Parse(rom);
        var mbc1 = new Mbc1(rom, header);

        // Enable RAM
        mbc1.WriteRom(0x0000, 0x0A);

        // Test basic read/write
        mbc1.WriteExternalRam(0xA000, 0x12);
        mbc1.WriteExternalRam(0xA001, 0x34);
        mbc1.WriteExternalRam(0xBFFF, 0x56);

        Assert.Equal(0x12, mbc1.ReadExternalRam(0xA000));
        Assert.Equal(0x34, mbc1.ReadExternalRam(0xA001));
        Assert.Equal(0x56, mbc1.ReadExternalRam(0xBFFF));
    }

    [Fact]
    public void ExternalRam_AdvancedMode_EnablesBanking()
    {
        var rom = CreateRom(0x03, 0x01, 0x03); // MBC1+RAM+BATTERY, 64KB ROM, 32KB RAM
        var header = CartridgeHeader.Parse(rom);
        var mbc1 = new Mbc1(rom, header);

        // Enable RAM
        mbc1.WriteRom(0x0000, 0x0A);

        // Switch to advanced banking mode
        mbc1.WriteRom(0x6000, 0x01);

        // Write to different RAM banks
        mbc1.WriteRom(0x4000, 0x00); // RAM bank 0
        mbc1.WriteExternalRam(0xA000, 0x11);

        mbc1.WriteRom(0x4000, 0x01); // RAM bank 1
        mbc1.WriteExternalRam(0xA000, 0x22);

        mbc1.WriteRom(0x4000, 0x02); // RAM bank 2
        mbc1.WriteExternalRam(0xA000, 0x33);

        mbc1.WriteRom(0x4000, 0x03); // RAM bank 3
        mbc1.WriteExternalRam(0xA000, 0x44);

        // Read back from different banks
        mbc1.WriteRom(0x4000, 0x00);
        Assert.Equal(0x11, mbc1.ReadExternalRam(0xA000));

        mbc1.WriteRom(0x4000, 0x01);
        Assert.Equal(0x22, mbc1.ReadExternalRam(0xA000));

        mbc1.WriteRom(0x4000, 0x02);
        Assert.Equal(0x33, mbc1.ReadExternalRam(0xA000));

        mbc1.WriteRom(0x4000, 0x03);
        Assert.Equal(0x44, mbc1.ReadExternalRam(0xA000));
    }

    [Fact]
    public void BatteryInterface_GetExternalRam_ReturnsDataIfModified()
    {
        var rom = CreateRom(0x03, 0x01, 0x02); // MBC1+RAM+BATTERY
        var header = CartridgeHeader.Parse(rom);
        var mbc1 = new Mbc1(rom, header);

        // Initially should return null (no data to save)
        Assert.Null(mbc1.GetExternalRam());

        // Enable RAM and write some data
        mbc1.WriteRom(0x0000, 0x0A);
        mbc1.WriteExternalRam(0xA000, 0x42);

        // Should now return the RAM data
        var ramData = mbc1.GetExternalRam();
        Assert.NotNull(ramData);
        Assert.Equal(8 * 1024, ramData.Length);
        Assert.Equal(0x42, ramData[0]);
    }

    [Fact]
    public void BatteryInterface_LoadExternalRam_LoadsSavedData()
    {
        var rom = CreateRom(0x03, 0x01, 0x02); // MBC1+RAM+BATTERY
        var header = CartridgeHeader.Parse(rom);
        var mbc1 = new Mbc1(rom, header);

        // Create some saved RAM data
        var savedData = new byte[8 * 1024];
        savedData[0] = 0x84;
        savedData[100] = 0x21;

        // Load the saved data
        mbc1.LoadExternalRam(savedData);

        // Enable RAM and verify the data was loaded
        mbc1.WriteRom(0x0000, 0x0A);
        Assert.Equal(0x84, mbc1.ReadExternalRam(0xA000));
        Assert.Equal(0x21, mbc1.ReadExternalRam(0xA064)); // 0xA000 + 100
    }

    [Fact]
    public void BatteryInterface_NoBatteryCartridge_ReturnsNull()
    {
        var rom = CreateRom(0x01, 0x01, 0x02); // MBC1 without battery
        var header = CartridgeHeader.Parse(rom);
        var mbc1 = new Mbc1(rom, header);

        // Should always return null for non-battery cartridges
        mbc1.WriteRom(0x0000, 0x0A);
        mbc1.WriteExternalRam(0xA000, 0x42);
        Assert.Null(mbc1.GetExternalRam());
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
            _ => 0x01
        };

        rom[0x0147] = 0x01; // MBC1
        rom[0x0148] = romSizeCode;
        rom[0x0149] = 0x02; // 8KB RAM

        return rom;
    }
}