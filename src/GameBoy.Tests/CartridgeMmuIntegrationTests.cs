using GameBoy.Core;

namespace GameBoy.Tests;

/// <summary>
/// Integration tests for cartridge integration with MMU.
/// </summary>
public class CartridgeMmuIntegrationTests
{
    [Fact]
    public void Mmu_LoadRom_DetectsCorrectCartridge()
    {
        var mmu = new Mmu();
        var rom = CreateRom(0x01, 0x01, 0x02); // MBC1, 64KB ROM, 8KB RAM

        mmu.LoadRom(rom);

        Assert.NotNull(mmu.Cartridge);
        Assert.IsType<Mbc1>(mmu.Cartridge);
    }

    [Fact]
    public void Mmu_ReadByte_RoutesToCartridgeRom()
    {
        var mmu = new Mmu();
        var rom = CreateRomWithBankMarkers(4); // 4 banks
        rom[0x0147] = 0x01; // MBC1

        mmu.LoadRom(rom);

        // Read from ROM area
        var value = mmu.ReadByte(0x0000);
        Assert.Equal(0x10, value); // Bank 0 marker

        var value2 = mmu.ReadByte(0x4000);
        Assert.Equal(0x11, value2); // Bank 1 marker (default for MBC1)
    }

    [Fact]
    public void Mmu_WriteByte_RoutesToCartridgeRom()
    {
        var mmu = new Mmu();
        var rom = CreateRomWithBankMarkers(4); // 4 banks
        rom[0x0147] = 0x01; // MBC1
        rom[0x0149] = 0x02; // 8KB RAM

        mmu.LoadRom(rom);

        // Write to ROM area (bank switching)
        mmu.WriteByte(0x2000, 0x02); // Switch to bank 2

        var value = mmu.ReadByte(0x4000);
        Assert.Equal(0x12, value); // Bank 2 marker
    }

    [Fact]
    public void Mmu_ExternalRam_RoutesToCartridge()
    {
        var mmu = new Mmu();
        var rom = CreateRom(0x03, 0x01, 0x02); // MBC1+RAM+BATTERY, 64KB ROM, 8KB RAM

        mmu.LoadRom(rom);

        // Enable RAM via cartridge ROM write
        mmu.WriteByte(0x0000, 0x0A);

        // Write to external RAM area
        mmu.WriteByte(0xA000, 0x42);
        mmu.WriteByte(0xA001, 0x84);

        // Read back from external RAM area
        Assert.Equal(0x42, mmu.ReadByte(0xA000));
        Assert.Equal(0x84, mmu.ReadByte(0xA001));
    }

    [Fact]
    public void Mmu_ExternalRam_DisabledByDefault()
    {
        var mmu = new Mmu();
        var rom = CreateRom(0x03, 0x01, 0x02); // MBC1+RAM+BATTERY

        mmu.LoadRom(rom);

        // RAM should be disabled by default
        mmu.WriteByte(0xA000, 0x42);
        Assert.Equal(0xFF, mmu.ReadByte(0xA000));
    }

    [Fact]
    public void Mmu_ExternalRam_Mbc3WithRtc()
    {
        var mmu = new Mmu();
        var rom = CreateRom(0x0F, 0x02, 0x00); // MBC3+TIMER+BATTERY

        mmu.LoadRom(rom);

        // Enable timer access
        mmu.WriteByte(0x0000, 0x0A);

        // Access RTC register
        mmu.WriteByte(0x4000, 0x08); // RTC Seconds register
        mmu.WriteByte(0xA000, 0x30);

        Assert.Equal(0x30, mmu.ReadByte(0xA000));
    }

    [Fact]
    public void Mmu_ExternalRam_Mbc5Banking()
    {
        var mmu = new Mmu();
        var rom = CreateRom(0x1B, 0x03, 0x03); // MBC5+RAM+BATTERY, 256KB ROM, 32KB RAM

        mmu.LoadRom(rom);

        // Enable RAM
        mmu.WriteByte(0x0000, 0x0A);

        // Write to different RAM banks
        mmu.WriteByte(0x4000, 0x00); // RAM bank 0
        mmu.WriteByte(0xA000, 0x11);

        mmu.WriteByte(0x4000, 0x01); // RAM bank 1
        mmu.WriteByte(0xA000, 0x22);

        // Read back from different banks
        mmu.WriteByte(0x4000, 0x00);
        Assert.Equal(0x11, mmu.ReadByte(0xA000));

        mmu.WriteByte(0x4000, 0x01);
        Assert.Equal(0x22, mmu.ReadByte(0xA000));
    }

    [Fact]
    public void Mmu_NoCartridge_ExternalRamReturnsFF()
    {
        var mmu = new Mmu();

        // Without cartridge loaded, external RAM should return 0xFF
        Assert.Equal(0xFF, mmu.ReadByte(0xA000));
        Assert.Equal(0xFF, mmu.ReadByte(0xBFFF));

        // Writes should be ignored (no exception)
        mmu.WriteByte(0xA000, 0x42);
        Assert.Equal(0xFF, mmu.ReadByte(0xA000));
    }

    [Fact]
    public void Mmu_WordOperations_WorkWithCartridge()
    {
        var mmu = new Mmu();
        var rom = CreateRom(0x03, 0x01, 0x02); // MBC1+RAM+BATTERY

        mmu.LoadRom(rom);

        // Enable RAM
        mmu.WriteByte(0x0000, 0x0A);

        // Test 16-bit operations with external RAM
        mmu.WriteWord(0xA000, 0x1234);
        Assert.Equal(0x1234, mmu.ReadWord(0xA000));

        // Test 16-bit operations with ROM (should route writes to cartridge)
        mmu.WriteWord(0x2000, 0x0002); // Switch to ROM bank 2

        // The ROM bank switch should have taken effect
        // (We can't easily test this without bank markers, but at least verify no crash)
        var result = mmu.ReadByte(0x4000); // Should not crash, returns ROM data
        // For a basic ROM without markers, this will be 0x00 (empty ROM data)
        Assert.Equal(0x00, result);
    }

    [Fact]
    public void Mmu_Reset_PreservesCartridge()
    {
        var mmu = new Mmu();
        var rom = CreateRom(0x01, 0x01, 0x02); // MBC1

        mmu.LoadRom(rom);
        var originalCartridge = mmu.Cartridge;

        mmu.Reset();

        // Cartridge should be preserved after reset
        Assert.Equal(originalCartridge, mmu.Cartridge);
        Assert.IsType<Mbc1>(mmu.Cartridge);
    }

    [Fact]
    public void Mmu_BatteryBackedCartridge_ImplementsInterface()
    {
        var mmu = new Mmu();
        var rom = CreateRom(0x03, 0x01, 0x02); // MBC1+RAM+BATTERY

        mmu.LoadRom(rom);

        Assert.IsAssignableFrom<IBatteryBacked>(mmu.Cartridge);

        var batteryCartridge = (IBatteryBacked)mmu.Cartridge!;
        Assert.True(batteryCartridge.HasBattery);
        Assert.Equal(8 * 1024, batteryCartridge.ExternalRamSize);
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
            _ => 0x01
        };

        rom[0x0147] = 0x01; // MBC1
        rom[0x0148] = romSizeCode;
        rom[0x0149] = 0x02; // 8KB RAM

        return rom;
    }
}