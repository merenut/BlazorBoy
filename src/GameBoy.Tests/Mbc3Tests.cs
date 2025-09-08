using System;
using GameBoy.Core;

namespace GameBoy.Tests;

/// <summary>
/// Tests for MBC3 (Memory Bank Controller 3) functionality.
/// </summary>
public class Mbc3Tests
{
    [Fact]
    public void ReadRom_Bank0_ReadsFromFixedBank()
    {
        var rom = CreateRomWithBankMarkers(8); // 8 banks (128KB)
        var header = CartridgeHeader.Parse(rom);
        var mbc3 = new Mbc3(rom, header);

        var value = mbc3.ReadRom(0x0000);
        Assert.Equal(0x10, value); // Bank 0 marker

        var value2 = mbc3.ReadRom(0x3FFF);
        Assert.Equal(0x00, value2); // Bank 0 fill value
    }

    [Fact]
    public void ReadRom_BankX_ReadsFromSwitchableBank()
    {
        var rom = CreateRomWithBankMarkers(8); // 8 banks (128KB)
        var header = CartridgeHeader.Parse(rom);
        var mbc3 = new Mbc3(rom, header);

        // Default should be bank 1
        var value = mbc3.ReadRom(0x4000);
        Assert.Equal(0x11, value); // Bank 1 marker

        // Switch to bank 2
        mbc3.WriteRom(0x2000, 0x02);
        value = mbc3.ReadRom(0x4000);
        Assert.Equal(0x12, value); // Bank 2 marker

        // Switch to bank 7
        mbc3.WriteRom(0x2000, 0x07);
        value = mbc3.ReadRom(0x4000);
        Assert.Equal(0x17, value); // Bank 7 marker
    }

    [Fact]
    public void WriteRom_Bank0Selection_MapsToBank1()
    {
        var rom = CreateRomWithBankMarkers(8);
        var header = CartridgeHeader.Parse(rom);
        var mbc3 = new Mbc3(rom, header);

        // Try to select bank 0 - should map to bank 1
        mbc3.WriteRom(0x2000, 0x00);
        var value = mbc3.ReadRom(0x4000);
        Assert.Equal(0x11, value); // Should read bank 1 marker
    }

    [Fact]
    public void WriteRom_RamAndTimerEnable_EnablesAccess()
    {
        var rom = CreateRom(0x13, 0x02, 0x03); // MBC3+RAM+BATTERY, 128KB ROM, 32KB RAM
        var header = CartridgeHeader.Parse(rom);
        var mbc3 = new Mbc3(rom, header);

        // RAM should be disabled by default
        mbc3.WriteExternalRam(0xA000, 0x42);
        Assert.Equal(0xFF, mbc3.ReadExternalRam(0xA000));

        // Enable RAM and Timer
        mbc3.WriteRom(0x0000, 0x0A);
        mbc3.WriteExternalRam(0xA000, 0x42);
        Assert.Equal(0x42, mbc3.ReadExternalRam(0xA000));

        // Disable RAM and Timer
        mbc3.WriteRom(0x0000, 0x00);
        Assert.Equal(0xFF, mbc3.ReadExternalRam(0xA000));
    }

    [Fact]
    public void ExternalRam_RamBanking_WorksCorrectly()
    {
        var rom = CreateRom(0x13, 0x02, 0x03); // MBC3+RAM+BATTERY, 128KB ROM, 32KB RAM
        var header = CartridgeHeader.Parse(rom);
        var mbc3 = new Mbc3(rom, header);

        // Enable RAM
        mbc3.WriteRom(0x0000, 0x0A);

        // Write to different RAM banks
        mbc3.WriteRom(0x4000, 0x00); // RAM bank 0
        mbc3.WriteExternalRam(0xA000, 0x11);

        mbc3.WriteRom(0x4000, 0x01); // RAM bank 1
        mbc3.WriteExternalRam(0xA000, 0x22);

        mbc3.WriteRom(0x4000, 0x02); // RAM bank 2
        mbc3.WriteExternalRam(0xA000, 0x33);

        mbc3.WriteRom(0x4000, 0x03); // RAM bank 3
        mbc3.WriteExternalRam(0xA000, 0x44);

        // Read back from different banks
        mbc3.WriteRom(0x4000, 0x00);
        Assert.Equal(0x11, mbc3.ReadExternalRam(0xA000));

        mbc3.WriteRom(0x4000, 0x01);
        Assert.Equal(0x22, mbc3.ReadExternalRam(0xA000));

        mbc3.WriteRom(0x4000, 0x02);
        Assert.Equal(0x33, mbc3.ReadExternalRam(0xA000));

        mbc3.WriteRom(0x4000, 0x03);
        Assert.Equal(0x44, mbc3.ReadExternalRam(0xA000));
    }

    [Fact]
    public void ExternalRam_RtcRegisters_AccessibleWhenEnabled()
    {
        var rom = CreateRom(0x0F, 0x02, 0x00); // MBC3+TIMER+BATTERY, 128KB ROM, no RAM
        var header = CartridgeHeader.Parse(rom);
        var mbc3 = new Mbc3(rom, header);

        // Enable timer access
        mbc3.WriteRom(0x0000, 0x0A);

        // Test accessing RTC registers (0x08-0x0C)
        mbc3.WriteRom(0x4000, 0x08); // RTC Seconds register
        mbc3.WriteExternalRam(0xA000, 0x30); // Set seconds to 48
        Assert.Equal(0x30, mbc3.ReadExternalRam(0xA000));

        mbc3.WriteRom(0x4000, 0x09); // RTC Minutes register
        mbc3.WriteExternalRam(0xA000, 0x15); // Set minutes to 21
        Assert.Equal(0x15, mbc3.ReadExternalRam(0xA000));

        mbc3.WriteRom(0x4000, 0x0A); // RTC Hours register
        mbc3.WriteExternalRam(0xA000, 0x12); // Set hours to 18
        Assert.Equal(0x12, mbc3.ReadExternalRam(0xA000));

        mbc3.WriteRom(0x4000, 0x0B); // RTC Day Low register
        mbc3.WriteExternalRam(0xA000, 0xFF);
        Assert.Equal(0xFF, mbc3.ReadExternalRam(0xA000));

        mbc3.WriteRom(0x4000, 0x0C); // RTC Day High register + flags
        mbc3.WriteExternalRam(0xA000, 0x01);
        Assert.Equal(0x01, mbc3.ReadExternalRam(0xA000));
    }

    [Fact]
    public void ExternalRam_NonRtcCartridge_RtcRegistersInaccessible()
    {
        var rom = CreateRom(0x11, 0x02, 0x03); // MBC3 without timer, 128KB ROM, 32KB RAM
        var header = CartridgeHeader.Parse(rom);
        var mbc3 = new Mbc3(rom, header);

        // Enable RAM access
        mbc3.WriteRom(0x0000, 0x0A);

        // Try to access RTC registers (should return 0xFF)
        mbc3.WriteRom(0x4000, 0x08);
        Assert.Equal(0xFF, mbc3.ReadExternalRam(0xA000));

        mbc3.WriteRom(0x4000, 0x0C);
        Assert.Equal(0xFF, mbc3.ReadExternalRam(0xA000));
    }

    [Fact]
    public void RtcLatch_LatchingBehavior_UpdatesRegisters()
    {
        var rom = CreateRom(0x0F, 0x02, 0x00); // MBC3+TIMER+BATTERY
        var header = CartridgeHeader.Parse(rom);
        var mbc3 = new Mbc3(rom, header);

        // Enable timer access
        mbc3.WriteRom(0x0000, 0x0A);

        // Read initial RTC values
        mbc3.WriteRom(0x4000, 0x08); // Seconds register
        var initialSeconds = mbc3.ReadExternalRam(0xA000);

        // Latch the clock (write 0x00 then 0x01)
        mbc3.WriteRom(0x6000, 0x00);
        mbc3.WriteRom(0x6000, 0x01);

        // RTC values should be latched and readable
        mbc3.WriteRom(0x4000, 0x08);
        var latchedSeconds = mbc3.ReadExternalRam(0xA000);

        // Values should be valid seconds (0-59)
        Assert.InRange(latchedSeconds, 0, 59);
    }

    [Fact]
    public void BatteryInterface_GetExternalRam_IncludesRamAndRtc()
    {
        var rom = CreateRom(0x10, 0x02, 0x03); // MBC3+TIMER+RAM+BATTERY
        var header = CartridgeHeader.Parse(rom);
        var mbc3 = new Mbc3(rom, header);

        // Enable RAM access and write some data
        mbc3.WriteRom(0x0000, 0x0A);
        mbc3.WriteRom(0x4000, 0x00); // RAM bank 0
        mbc3.WriteExternalRam(0xA000, 0x42);

        // Write to RTC register
        mbc3.WriteRom(0x4000, 0x08); // RTC Seconds
        mbc3.WriteExternalRam(0xA000, 0x30);

        // Get external RAM data (should include both RAM and RTC)
        var ramData = mbc3.GetExternalRam();
        Assert.NotNull(ramData);

        // Should be RAM size + 5 bytes for RTC
        Assert.Equal(32 * 1024 + 5, ramData.Length);

        // Check RAM data
        Assert.Equal(0x42, ramData[0]);

        // Check RTC data (last 5 bytes)
        Assert.Equal(0x30, ramData[32 * 1024]); // Seconds register
    }

    [Fact]
    public void BatteryInterface_LoadExternalRam_LoadsRamAndRtc()
    {
        var rom = CreateRom(0x10, 0x02, 0x03); // MBC3+TIMER+RAM+BATTERY
        var header = CartridgeHeader.Parse(rom);
        var mbc3 = new Mbc3(rom, header);

        // Create saved data with RAM and RTC
        var savedData = new byte[32 * 1024 + 5];
        savedData[0] = 0x84; // RAM data
        savedData[32 * 1024] = 0x45; // RTC seconds
        savedData[32 * 1024 + 1] = 0x30; // RTC minutes

        // Load the saved data
        mbc3.LoadExternalRam(savedData);

        // Enable access and verify the data was loaded
        mbc3.WriteRom(0x0000, 0x0A);

        // Check RAM data
        mbc3.WriteRom(0x4000, 0x00); // RAM bank 0
        Assert.Equal(0x84, mbc3.ReadExternalRam(0xA000));

        // Check RTC data
        mbc3.WriteRom(0x4000, 0x08); // RTC seconds
        Assert.Equal(0x45, mbc3.ReadExternalRam(0xA000));

        mbc3.WriteRom(0x4000, 0x09); // RTC minutes
        Assert.Equal(0x30, mbc3.ReadExternalRam(0xA000));
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
            _ => 0x01
        };

        rom[0x0147] = 0x11; // MBC3
        rom[0x0148] = romSizeCode;
        rom[0x0149] = 0x03; // 32KB RAM

        return rom;
    }

    [Fact]
    public void ReadRom_BeyondAvailableSize_ReturnsFF()
    {
        var rom = CreateRomWithBankMarkers(8); // 8 banks (128KB)
        var header = CartridgeHeader.Parse(rom);
        var mbc3 = new Mbc3(rom, header);

        // Try to access beyond ROM size
        mbc3.WriteRom(0x2000, 0x7F); // Try to select bank 127 (doesn't exist)
        var value = mbc3.ReadRom(0x4000);

        // Should return 0xFF for out-of-bounds access
        Assert.Equal(0xFF, value);
    }

    [Fact]
    public void WriteRom_MaxBankNumber_WorksWithinLimits()
    {
        var rom = CreateRomWithBankMarkers(64); // 64 banks (1MB) - reasonable MBC3 size
        var header = CartridgeHeader.Parse(rom);
        var mbc3 = new Mbc3(rom, header);

        // Test high bank selection
        mbc3.WriteRom(0x2000, 0x3F); // Bank 63
        var value = mbc3.ReadRom(0x4000);
        Assert.Equal((byte)(0x10 + 63), value); // Bank 63 marker
    }

    [Fact]
    public void WriteRom_Bank7BitLimit_OnlyLower7BitsUsed()
    {
        var rom = CreateRomWithBankMarkers(64); // 64 banks
        var header = CartridgeHeader.Parse(rom);
        var mbc3 = new Mbc3(rom, header);

        // Write with upper bit set (should be ignored)
        mbc3.WriteRom(0x2000, 0xFF); // All bits set
        var value = mbc3.ReadRom(0x4000);
        // Should only use lower 7 bits: 0x7F = 127, but limited by available banks
        // With 64 banks, this should wrap or return FF
        Assert.Equal(0xFF, value);
    }

    [Fact]
    public void ExternalRam_MaxRamBanks_WorksCorrectly()
    {
        var rom = CreateRom(0x13, 0x02, 0x03); // MBC3+RAM+BATTERY, 128KB ROM, 32KB RAM (4 banks)
        var header = CartridgeHeader.Parse(rom);
        var mbc3 = new Mbc3(rom, header);

        // Enable RAM
        mbc3.WriteRom(0x0000, 0x0A);

        // Test all 4 RAM banks (0-3)
        for (int bank = 0; bank < 4; bank++)
        {
            mbc3.WriteRom(0x4000, (byte)bank);
            mbc3.WriteExternalRam(0xA000, (byte)(0x50 + bank));
        }

        // Verify data in each bank
        for (int bank = 0; bank < 4; bank++)
        {
            mbc3.WriteRom(0x4000, (byte)bank);
            Assert.Equal((byte)(0x50 + bank), mbc3.ReadExternalRam(0xA000));
        }
    }

    [Fact]
    public void ExternalRam_BeyondAvailableBanks_ReturnsFF()
    {
        var rom = CreateRom(0x11, 0x02, 0x02); // MBC3, 128KB ROM, 8KB RAM (1 bank)
        var header = CartridgeHeader.Parse(rom);
        var mbc3 = new Mbc3(rom, header);

        // Enable RAM
        mbc3.WriteRom(0x0000, 0x0A);

        // Try to access RAM bank 2 (doesn't exist with 8KB RAM)
        mbc3.WriteRom(0x4000, 0x02);
        Assert.Equal(0xFF, mbc3.ReadExternalRam(0xA000));

        // Write should be ignored
        mbc3.WriteExternalRam(0xA000, 0x42);
        Assert.Equal(0xFF, mbc3.ReadExternalRam(0xA000));
    }

    [Fact]
    public void RtcRegisters_BoundaryValues_HandledCorrectly()
    {
        var rom = CreateRom(0x0F, 0x02, 0x00); // MBC3+TIMER+BATTERY
        var header = CartridgeHeader.Parse(rom);
        var mbc3 = new Mbc3(rom, header);

        // Enable timer access
        mbc3.WriteRom(0x0000, 0x0A);

        // Test that RTC registers accept and store boundary values (no clamping)
        mbc3.WriteRom(0x4000, 0x08); // RTC Seconds
        mbc3.WriteExternalRam(0xA000, 59); // Max valid seconds
        Assert.Equal(59, mbc3.ReadExternalRam(0xA000));

        mbc3.WriteExternalRam(0xA000, 60); // Invalid seconds - should be stored as-is
        Assert.Equal(60, mbc3.ReadExternalRam(0xA000)); // MBC3 stores raw value

        mbc3.WriteRom(0x4000, 0x09); // RTC Minutes  
        mbc3.WriteExternalRam(0xA000, 59); // Max valid minutes
        Assert.Equal(59, mbc3.ReadExternalRam(0xA000));

        mbc3.WriteRom(0x4000, 0x0A); // RTC Hours
        mbc3.WriteExternalRam(0xA000, 23); // Max valid hours
        Assert.Equal(23, mbc3.ReadExternalRam(0xA000));
    }

    [Fact]
    public void RtcRegisters_InvalidRegisterAccess_ReturnsFF()
    {
        var rom = CreateRom(0x0F, 0x02, 0x00); // MBC3+TIMER+BATTERY
        var header = CartridgeHeader.Parse(rom);
        var mbc3 = new Mbc3(rom, header);

        // Enable timer access
        mbc3.WriteRom(0x0000, 0x0A);

        // Try to access invalid RTC register
        mbc3.WriteRom(0x4000, 0x0D); // Beyond RTC register range
        Assert.Equal(0xFF, mbc3.ReadExternalRam(0xA000));

        mbc3.WriteRom(0x4000, 0xFF); // Way beyond
        Assert.Equal(0xFF, mbc3.ReadExternalRam(0xA000));
    }

    [Fact]
    public void WriteRom_InvalidAddressRanges_Ignored()
    {
        var rom = CreateRomWithBankMarkers(8);
        var header = CartridgeHeader.Parse(rom);
        var mbc3 = new Mbc3(rom, header);

        // Set initial bank
        mbc3.WriteRom(0x2000, 0x05);
        var initialValue = mbc3.ReadRom(0x4000);
        Assert.Equal(0x15, initialValue); // Bank 5

        // Writes to invalid ranges should be ignored
        mbc3.WriteRom(0x8000, 0x07); // Beyond MBC3 control range

        // Bank should remain unchanged
        var unchangedValue = mbc3.ReadRom(0x4000);
        Assert.Equal(0x15, unchangedValue); // Still bank 5
    }

    [Fact]
    public void RtcLatch_ExtremeValues_HandledGracefully()
    {
        var rom = CreateRom(0x0F, 0x02, 0x00); // MBC3+TIMER+BATTERY
        var header = CartridgeHeader.Parse(rom);
        var mbc3 = new Mbc3(rom, header);

        // Enable timer access
        mbc3.WriteRom(0x0000, 0x0A);

        // Multiple latch attempts should be handled gracefully
        for (int i = 0; i < 10; i++)
        {
            mbc3.WriteRom(0x6000, 0x00);
            mbc3.WriteRom(0x6000, 0x01);
        }

        // Should still be able to read RTC values
        mbc3.WriteRom(0x4000, 0x08); // Seconds
        var seconds = mbc3.ReadExternalRam(0xA000);
        Assert.InRange(seconds, 0, 59);
    }

    [Fact]
    public void BatteryInterface_LargeRamSize_HandledCorrectly()
    {
        var rom = CreateRom(0x10, 0x04, 0x04); // MBC3+TIMER+RAM+BATTERY (with RTC), 512KB ROM, 128KB RAM
        var header = CartridgeHeader.Parse(rom);
        var mbc3 = new Mbc3(rom, header);

        // Enable RAM and write data across multiple banks
        mbc3.WriteRom(0x0000, 0x0A);

        // Write to different banks
        for (int bank = 0; bank < 16; bank++) // 128KB = 16 banks
        {
            mbc3.WriteRom(0x4000, (byte)bank);
            mbc3.WriteExternalRam(0xA000, (byte)(bank + 1));
        }

        // Get external RAM data
        var ramData = mbc3.GetExternalRam();
        Assert.NotNull(ramData);
        Assert.Equal(128 * 1024 + 5, ramData.Length); // RAM + RTC (cartridge type 0x10 has RTC)

        // Verify data in first bank
        Assert.Equal(1, ramData[0]);
    }
}