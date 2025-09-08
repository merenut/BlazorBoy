using GameBoy.Core;

namespace GameBoy.Tests;

/// <summary>
/// Integration tests for emulator-level MBC functionality including battery RAM support.
/// </summary>
public class EmulatorMbcIntegrationTests
{
    [Fact]
    public void Emulator_LoadRom_Mbc0_NoBatteryRam()
    {
        var emulator = new Emulator();
        var rom = CreateRom(0x00, 0x01, 0x00); // ROM ONLY, 64KB ROM, No RAM

        emulator.LoadRom(rom);

        Assert.NotNull(emulator.Mmu.Cartridge);
        Assert.IsType<Mbc0>(emulator.Mmu.Cartridge);
        Assert.False(emulator.HasBatteryRam);
        Assert.Null(emulator.GetBatteryRam());
    }

    [Fact]
    public void Emulator_LoadRom_Mbc1WithBattery_HasBatteryRam()
    {
        var emulator = new Emulator();
        var rom = CreateRom(0x03, 0x01, 0x02); // MBC1+RAM+BATTERY, 64KB ROM, 8KB RAM

        emulator.LoadRom(rom);

        Assert.NotNull(emulator.Mmu.Cartridge);
        Assert.IsType<Mbc1>(emulator.Mmu.Cartridge);
        Assert.True(emulator.HasBatteryRam);

        var batteryCartridge = (IBatteryBacked)emulator.Mmu.Cartridge;
        Assert.Equal(8 * 1024, batteryCartridge.ExternalRamSize);
    }

    [Fact]
    public void Emulator_LoadRom_Mbc3WithBattery_HasBatteryRam()
    {
        var emulator = new Emulator();
        var rom = CreateRom(0x13, 0x02, 0x03); // MBC3+RAM+BATTERY, 128KB ROM, 32KB RAM

        emulator.LoadRom(rom);

        Assert.NotNull(emulator.Mmu.Cartridge);
        Assert.IsType<Mbc3>(emulator.Mmu.Cartridge);
        Assert.True(emulator.HasBatteryRam);

        var batteryCartridge = (IBatteryBacked)emulator.Mmu.Cartridge;
        Assert.Equal(32 * 1024, batteryCartridge.ExternalRamSize);
    }

    [Fact]
    public void Emulator_LoadRom_Mbc5WithBattery_HasBatteryRam()
    {
        var emulator = new Emulator();
        var rom = CreateRom(0x1B, 0x03, 0x03); // MBC5+RAM+BATTERY, 256KB ROM, 32KB RAM

        emulator.LoadRom(rom);

        Assert.NotNull(emulator.Mmu.Cartridge);
        Assert.IsType<Mbc5>(emulator.Mmu.Cartridge);
        Assert.True(emulator.HasBatteryRam);

        var batteryCartridge = (IBatteryBacked)emulator.Mmu.Cartridge;
        Assert.Equal(32 * 1024, batteryCartridge.ExternalRamSize);
    }

    [Fact]
    public void Emulator_BatteryRam_SaveLoad_Mbc1()
    {
        var emulator = new Emulator();
        var rom = CreateRom(0x03, 0x01, 0x02); // MBC1+RAM+BATTERY

        emulator.LoadRom(rom);

        // Enable RAM and write test data
        emulator.Mmu.WriteByte(0x0000, 0x0A); // Enable RAM
        emulator.Mmu.WriteByte(0xA000, 0x42);
        emulator.Mmu.WriteByte(0xA001, 0x84);
        emulator.Mmu.WriteByte(0xA100, 0x12);

        // Get battery RAM data
        var batteryData = emulator.GetBatteryRam();
        Assert.NotNull(batteryData);
        Assert.Equal(8 * 1024, batteryData.Length);
        Assert.Equal(0x42, batteryData[0]);
        Assert.Equal(0x84, batteryData[1]);
        Assert.Equal(0x12, batteryData[0x100]);

        // Create new emulator and load same ROM
        var emulator2 = new Emulator();
        emulator2.LoadRom(rom);

        // Load the battery data
        emulator2.LoadBatteryRam(batteryData);

        // Enable RAM and verify data is restored
        emulator2.Mmu.WriteByte(0x0000, 0x0A); // Enable RAM
        Assert.Equal(0x42, emulator2.Mmu.ReadByte(0xA000));
        Assert.Equal(0x84, emulator2.Mmu.ReadByte(0xA001));
        Assert.Equal(0x12, emulator2.Mmu.ReadByte(0xA100));
    }

    [Fact]
    public void Emulator_BatteryRam_SaveLoad_Mbc3WithRtc()
    {
        var emulator = new Emulator();
        var rom = CreateRom(0x10, 0x02, 0x03); // MBC3+TIMER+RAM+BATTERY, 128KB ROM, 32KB RAM

        emulator.LoadRom(rom);

        // Enable timer access and write test data
        emulator.Mmu.WriteByte(0x0000, 0x0A); // Enable access
        emulator.Mmu.WriteByte(0x4000, 0x08); // RTC Seconds register
        emulator.Mmu.WriteByte(0xA000, 0x30);

        emulator.Mmu.WriteByte(0x4000, 0x09); // RTC Minutes register
        emulator.Mmu.WriteByte(0xA000, 0x15);

        // Get battery RAM data (should include RTC data)
        var batteryData = emulator.GetBatteryRam();
        Assert.NotNull(batteryData);
        Assert.True(batteryData.Length >= 5); // Should include RTC registers

        // Create new emulator and restore data
        var emulator2 = new Emulator();
        emulator2.LoadRom(rom);
        emulator2.LoadBatteryRam(batteryData);

        // Verify RTC data is restored
        emulator2.Mmu.WriteByte(0x0000, 0x0A);
        emulator2.Mmu.WriteByte(0x4000, 0x08); // RTC Seconds
        Assert.Equal(0x30, emulator2.Mmu.ReadByte(0xA000));

        emulator2.Mmu.WriteByte(0x4000, 0x09); // RTC Minutes
        Assert.Equal(0x15, emulator2.Mmu.ReadByte(0xA000));
    }

    [Fact]
    public void Emulator_BatteryRam_SaveLoad_Mbc5RamBanking()
    {
        var emulator = new Emulator();
        var rom = CreateRom(0x1B, 0x03, 0x03); // MBC5+RAM+BATTERY, 256KB ROM, 32KB RAM

        emulator.LoadRom(rom);

        // Enable RAM and write to different RAM banks
        emulator.Mmu.WriteByte(0x0000, 0x0A); // Enable RAM

        emulator.Mmu.WriteByte(0x4000, 0x00); // RAM bank 0
        emulator.Mmu.WriteByte(0xA000, 0x11);

        emulator.Mmu.WriteByte(0x4000, 0x01); // RAM bank 1
        emulator.Mmu.WriteByte(0xA000, 0x22);

        emulator.Mmu.WriteByte(0x4000, 0x02); // RAM bank 2
        emulator.Mmu.WriteByte(0xA000, 0x33);

        // Get battery data
        var batteryData = emulator.GetBatteryRam();
        Assert.NotNull(batteryData);
        Assert.Equal(32 * 1024, batteryData.Length);

        // Create new emulator and restore
        var emulator2 = new Emulator();
        emulator2.LoadRom(rom);
        emulator2.LoadBatteryRam(batteryData);

        // Verify all bank data is restored
        emulator2.Mmu.WriteByte(0x0000, 0x0A); // Enable RAM

        emulator2.Mmu.WriteByte(0x4000, 0x00);
        Assert.Equal(0x11, emulator2.Mmu.ReadByte(0xA000));

        emulator2.Mmu.WriteByte(0x4000, 0x01);
        Assert.Equal(0x22, emulator2.Mmu.ReadByte(0xA000));

        emulator2.Mmu.WriteByte(0x4000, 0x02);
        Assert.Equal(0x33, emulator2.Mmu.ReadByte(0xA000));
    }

    [Fact]
    public void Emulator_LoadRom_InvalidRom_ThrowsException()
    {
        var emulator = new Emulator();

        // Test null ROM
        Assert.Throws<ArgumentNullException>(() => emulator.LoadRom(null!));

        // Test ROM too small
        var tinyRom = new byte[0x0100]; // Too small for header
        Assert.Throws<ArgumentException>(() => emulator.LoadRom(tinyRom));
    }

    [Fact]
    public void Emulator_LoadRom_UnsupportedMbc_ThrowsNotSupportedException()
    {
        var emulator = new Emulator();
        var rom = CreateRom(0x05, 0x01, 0x02); // MBC2 - not yet supported

        var ex = Assert.Throws<NotSupportedException>(() => emulator.LoadRom(rom));
        Assert.Contains("MBC2 is not yet supported", ex.Message);
    }

    [Fact]
    public void Emulator_BatteryRam_NoCartridge_ReturnsNull()
    {
        var emulator = new Emulator();

        Assert.False(emulator.HasBatteryRam);
        Assert.Null(emulator.GetBatteryRam());

        // Loading null battery data should not crash
        emulator.LoadBatteryRam(new byte[] { 0x42, 0x84 });
    }

    [Fact]
    public void Emulator_BatteryRam_NonBatteryCartridge_ReturnsNull()
    {
        var emulator = new Emulator();
        var rom = CreateRom(0x01, 0x01, 0x02); // MBC1 without battery

        emulator.LoadRom(rom);

        Assert.False(emulator.HasBatteryRam);
        Assert.Null(emulator.GetBatteryRam());
    }

    [Fact]
    public void Emulator_ExternalRamAccessibility_AllMbcTypes()
    {
        var emulator = new Emulator();

        // Test MBC0 (no external RAM support)
        var rom0 = CreateRom(0x00, 0x01, 0x00);
        emulator.LoadRom(rom0);
        Assert.True(emulator.Mmu.IsExternalRamAccessible()); // Basic availability

        // Test MBC1 with RAM
        var rom1 = CreateRom(0x02, 0x01, 0x02);
        emulator.LoadRom(rom1);
        Assert.True(emulator.Mmu.IsExternalRamAccessible());

        // Test MBC3 with RAM and RTC
        var rom3 = CreateRom(0x10, 0x02, 0x03);
        emulator.LoadRom(rom3);
        Assert.True(emulator.Mmu.IsExternalRamAccessible());

        // Test MBC5 with RAM
        var rom5 = CreateRom(0x1A, 0x03, 0x03);
        emulator.LoadRom(rom5);
        Assert.True(emulator.Mmu.IsExternalRamAccessible());
    }

    [Fact]
    public void Mmu_ErrorHandling_InvalidRomAccess()
    {
        var emulator = new Emulator();

        // Test no cartridge loaded - should return 0xFF for ROM reads
        Assert.Equal(0xFF, emulator.Mmu.ReadByte(0x0000));
        Assert.Equal(0xFF, emulator.Mmu.ReadByte(0x4000));
        Assert.Equal(0xFF, emulator.Mmu.ReadByte(0x7FFF));

        // Writes should be ignored (no exception)
        emulator.Mmu.WriteByte(0x0000, 0x42);
        emulator.Mmu.WriteByte(0x4000, 0x84);
    }

    [Fact]
    public void Mmu_ErrorHandling_ExternalRamAccessibility()
    {
        var emulator = new Emulator();

        // Test no cartridge
        Assert.False(emulator.Mmu.IsExternalRamAccessible());

        // Test cartridge with no RAM
        var romNoRam = CreateRom(0x00, 0x01, 0x00); // ROM ONLY, no RAM
        emulator.LoadRom(romNoRam);
        Assert.True(emulator.Mmu.IsExternalRamAccessible()); // Basic check passes

        // Test cartridge with RAM
        var romWithRam = CreateRom(0x03, 0x01, 0x02); // MBC1+RAM+BATTERY
        emulator.LoadRom(romWithRam);
        Assert.True(emulator.Mmu.IsExternalRamAccessible());

        var batteryCartridge = (IBatteryBacked)emulator.Mmu.Cartridge!;
        Assert.Equal(8 * 1024, batteryCartridge.ExternalRamSize);
    }

    [Fact]
    public void Emulator_BatteryRam_ErrorHandling_EdgeCases()
    {
        var emulator = new Emulator();

        // Test with no cartridge
        Assert.Null(emulator.GetBatteryRam());
        emulator.LoadBatteryRam(new byte[] { 0x42 }); // Should not crash

        // Test with non-battery cartridge
        var romNoBattery = CreateRom(0x01, 0x01, 0x02); // MBC1 without battery
        emulator.LoadRom(romNoBattery);
        Assert.Null(emulator.GetBatteryRam());
        emulator.LoadBatteryRam(new byte[] { 0x42 }); // Should not crash

        // Test with battery cartridge but empty RAM
        var romWithBattery = CreateRom(0x03, 0x01, 0x02); // MBC1+RAM+BATTERY
        emulator.LoadRom(romWithBattery);

        // Without writing any data, should return null (no data to save)
        Assert.Null(emulator.GetBatteryRam());

        // Load null battery data
        emulator.LoadBatteryRam(null); // Should not crash

        // Load empty array
        emulator.LoadBatteryRam(new byte[0]); // Should not crash
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
}