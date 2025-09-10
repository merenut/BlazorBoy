using Xunit;
using GameBoy.Core.Persistence;
using System.Text;

namespace GameBoy.Tests;

/// <summary>
/// Tests for battery-backed RAM persistence functionality.
/// </summary>
public class BatteryStoreTests
{
    [Fact]
    public void GenerateRomKey_ValidRom_ReturnsCorrectKey()
    {
        // Create a test ROM with a title
        byte[] testRom = new byte[0x8000]; // 32KB ROM

        // Set title in ROM header (0x134-0x143)
        byte[] title = Encoding.ASCII.GetBytes("TESTGAME");
        Array.Copy(title, 0, testRom, 0x134, title.Length);

        // Set some ROM data for hashing
        testRom[0x100] = 0x00; // NOP
        testRom[0x101] = 0xC3; // JP
        testRom[0x102] = 0x50;
        testRom[0x103] = 0x01;

        string key = BatteryStore.GenerateRomKey(testRom);

        Assert.StartsWith("battery_TESTGAME_", key);
        Assert.True(key.Length > 20); // Should include hash
    }

    [Fact]
    public void GenerateRomKey_TitleWithSpaces_ReplacesWithUnderscores()
    {
        byte[] testRom = new byte[0x8000];

        // Title with spaces and special characters
        byte[] title = Encoding.ASCII.GetBytes("TEST GAME/1");
        Array.Copy(title, 0, testRom, 0x134, title.Length);

        string key = BatteryStore.GenerateRomKey(testRom);

        Assert.Contains("TEST_GAME_1", key);
        Assert.DoesNotContain(" ", key);
        Assert.DoesNotContain("/", key);
    }

    [Fact]
    public void GenerateRomKey_EmptyRom_ReturnsUnknown()
    {
        byte[] testRom = new byte[0x100]; // Too small for header

        string key = BatteryStore.GenerateRomKey(testRom);

        Assert.Equal("unknown_rom", key);
    }

    [Fact]
    public void GenerateRomKey_NullRom_ReturnsUnknown()
    {
        string key = BatteryStore.GenerateRomKey(null);

        Assert.Equal("unknown_rom", key);
    }

    [Fact]
    public void GenerateRomKey_SameRom_ReturnsSameKey()
    {
        byte[] testRom = new byte[0x8000];
        byte[] title = Encoding.ASCII.GetBytes("TESTGAME");
        Array.Copy(title, 0, testRom, 0x134, title.Length);

        // Add some consistent ROM data
        testRom[0x100] = 0x00;
        testRom[0x101] = 0xC3;

        string key1 = BatteryStore.GenerateRomKey(testRom);
        string key2 = BatteryStore.GenerateRomKey(testRom);

        Assert.Equal(key1, key2);
    }

    [Fact]
    public void GenerateRomKey_DifferentRoms_ReturnsDifferentKeys()
    {
        byte[] testRom1 = new byte[0x8000];
        byte[] testRom2 = new byte[0x8000];

        byte[] title1 = Encoding.ASCII.GetBytes("GAME1");
        byte[] title2 = Encoding.ASCII.GetBytes("GAME2");

        Array.Copy(title1, 0, testRom1, 0x134, title1.Length);
        Array.Copy(title2, 0, testRom2, 0x134, title2.Length);

        string key1 = BatteryStore.GenerateRomKey(testRom1);
        string key2 = BatteryStore.GenerateRomKey(testRom2);

        Assert.NotEqual(key1, key2);
    }

    [Theory]
    [InlineData(-1, true)]   // null case
    [InlineData(0, true)]    // empty
    [InlineData(8192, true)] // 8KB - typical size
    [InlineData(32768, true)] // 32KB - max common size
    [InlineData(131072, true)] // 128KB - max allowed
    [InlineData(200000, false)] // Too large
    public void ValidateBatteryData_VariousSizes_ReturnsExpected(int size, bool expected)
    {
        byte[]? data = size < 0 ? null : new byte[size];

        bool result = BatteryStore.ValidateBatteryData(data);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetBatteryRamKey_ValidRomKey_ReturnsCorrectKey()
    {
        string romKey = "battery_TESTGAME_12345678";

        string ramKey = BatteryStore.GetBatteryRamKey(romKey);

        Assert.Equal("battery_TESTGAME_12345678_ram", ramKey);
    }
}