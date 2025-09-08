using GameBoy.Core;

namespace GameBoy.Tests;

/// <summary>
/// Tests to validate that I/O region wiring meets all acceptance criteria from issue #27
/// </summary>
public class IoRegionWiringTests
{
    [Fact]
    public void IoRegion_AllReads_CallReadIoRegister()
    {
        var mmu = new Mmu();

        // Test a few addresses in the I/O region to ensure ReadIoRegister is called
        // We can verify this by checking that stubbed registers return expected values
        // and unstubbed registers return 0xFF

        // Stubbed register - should return proper value with masking
        var joyp = mmu.ReadByte(0xFF00); // JOYP
        Assert.Equal(0xCF, joyp); // Default value with lower 4 bits forced to 1

        // Another stubbed register
        var ifReg = mmu.ReadByte(0xFF0F); // IF
        Assert.Equal(0xE1, ifReg); // Default 0x01 with upper 3 bits forced to 1

        // Unstubbed register - should return 0xFF
        var unstubbed = mmu.ReadByte(0xFF01); // Serial data register (not stubbed)
        Assert.Equal(0xFF, unstubbed);

        // Test edge cases
        var firstIo = mmu.ReadByte(0xFF00); // First I/O address
        var lastIo = mmu.ReadByte(0xFF7F);  // Last I/O address

        // First should be JOYP (stubbed), last should be unstubbed (0xFF)
        Assert.Equal(0xCF, firstIo);
        Assert.Equal(0xFF, lastIo);
    }

    [Fact]
    public void IoRegion_AllWrites_CallWriteIoRegister()
    {
        var mmu = new Mmu();

        // Test that writes to I/O region call WriteIoRegister by verifying behavior

        // Test DIV register behavior - writes should reset to 0
        mmu.WriteByte(0xFF04, 0x42); // Write any value to DIV
        Assert.Equal(0x00, mmu.ReadByte(0xFF04)); // Should be reset to 0

        mmu.WriteByte(0xFF04, 0xFF); // Write different value
        Assert.Equal(0x00, mmu.ReadByte(0xFF04)); // Should still be 0

        // Test JOYP masking behavior - only bits 4-5 should be writable
        mmu.WriteByte(0xFF00, 0x00); // Try to clear all bits
        var joyp = mmu.ReadByte(0xFF00);
        Assert.Equal(0xCF, joyp); // Lower 4 bits should still be 1s

        // Test IF masking behavior - only lower 5 bits writable
        mmu.WriteByte(0xFF0F, 0x00); // Clear all bits
        var ifReg = mmu.ReadByte(0xFF0F);
        Assert.Equal(0xE0, ifReg); // Upper 3 bits should still be 1s

        // Test unstubbed register - writes should be ignored
        mmu.WriteByte(0xFF01, 0x42); // Write to unstubbed register
        Assert.Equal(0xFF, mmu.ReadByte(0xFF01)); // Should still read 0xFF
    }

    [Fact]
    public void UnusedIoRegisters_ReturnOxFFOnRead()
    {
        var mmu = new Mmu();

        // Test various unstubbed I/O registers return 0xFF
        ushort[] unstubbed = {
            0xFF01, 0xFF02, 0xFF03, // Serial
            0xFF08, 0xFF09, 0xFF0A, 0xFF0B, 0xFF0C, 0xFF0D, 0xFF0E, // Gaps
            0xFF10, 0xFF11, 0xFF12, 0xFF13, 0xFF14, // Sound
            0xFF15, 0xFF16, 0xFF17, 0xFF18, 0xFF19, // Sound
            0xFF1A, 0xFF1B, 0xFF1C, 0xFF1D, 0xFF1E, // Sound  
            0xFF1F, 0xFF20, 0xFF21, 0xFF22, 0xFF23, // Sound
            0xFF24, 0xFF25, 0xFF26, // Sound
            0xFF27, 0xFF28, 0xFF29, 0xFF2A, 0xFF2B, 0xFF2C, 0xFF2D, 0xFF2E, 0xFF2F, // Gaps
            0xFF30, 0xFF31, 0xFF32, 0xFF33, 0xFF34, 0xFF35, 0xFF36, 0xFF37, // Wave
            0xFF38, 0xFF39, 0xFF3A, 0xFF3B, 0xFF3C, 0xFF3D, 0xFF3E, 0xFF3F, // Wave
            0xFF50, 0xFF51, 0xFF52, 0xFF53, 0xFF54, 0xFF55, 0xFF56, 0xFF57, // Gaps
            0xFF68, 0xFF69, 0xFF6A, 0xFF6B, 0xFF6C, 0xFF6D, 0xFF6E, 0xFF6F, // CGB
            0xFF70, 0xFF71, 0xFF72, 0xFF73, 0xFF74, 0xFF75, 0xFF76, 0xFF77, // CGB
            0xFF78, 0xFF79, 0xFF7A, 0xFF7B, 0xFF7C, 0xFF7D, 0xFF7E, 0xFF7F  // Gaps
        };

        foreach (var addr in unstubbed)
        {
            var value = mmu.ReadByte(addr);
            Assert.Equal(0xFF, value);
        }
    }

    [Fact]
    public void UnusedIoRegisters_IgnoreWrites()
    {
        var mmu = new Mmu();

        // Test that writes to unstubbed I/O registers are ignored
        ushort[] unstubbed = { 0xFF01, 0xFF02, 0xFF10, 0xFF30, 0xFF50, 0xFF7F };

        foreach (var addr in unstubbed)
        {
            // Write a value
            mmu.WriteByte(addr, 0x42);

            // Should still read as 0xFF (write ignored)
            var value = mmu.ReadByte(addr);
            Assert.Equal(0xFF, value);

            // Try a different value
            mmu.WriteByte(addr, 0x84);
            value = mmu.ReadByte(addr);
            Assert.Equal(0xFF, value);
        }
    }

    [Fact]
    public void StubbedIoRegisters_PreserveBehavior()
    {
        var mmu = new Mmu();
        var timer = new GameBoy.Core.Timer(mmu.InterruptController);
        mmu.Timer = timer; // Connect timer to MMU

        // Test that all stubbed registers maintain their expected behavior

        // JOYP - lower 4 bits always read as 1s, only bits 4-5 writable
        mmu.WriteByte(0xFF00, 0x00);
        Assert.Equal(0xCF, mmu.ReadByte(0xFF00));

        mmu.WriteByte(0xFF00, 0x10); // Set bit 4
        Assert.Equal(0xDF, mmu.ReadByte(0xFF00));

        // DIV - writes reset to 0
        mmu.WriteByte(0xFF04, 0xFF);
        Assert.Equal(0x00, mmu.ReadByte(0xFF04));

        // TIMA/TMA/TAC - simple read/write
        mmu.WriteByte(0xFF05, 0x42);
        Assert.Equal(0x42, mmu.ReadByte(0xFF05));

        mmu.WriteByte(0xFF06, 0x84);
        Assert.Equal(0x84, mmu.ReadByte(0xFF06));

        mmu.WriteByte(0xFF07, 0x07);
        Assert.Equal(0x07, mmu.ReadByte(0xFF07));

        // IF - upper 3 bits always read as 1s, only lower 5 bits writable
        mmu.WriteByte(0xFF0F, 0x00);
        Assert.Equal(0xE0, mmu.ReadByte(0xFF0F));

        mmu.WriteByte(0xFF0F, 0x1F);
        Assert.Equal(0xFF, mmu.ReadByte(0xFF0F));

        // PPU registers - simple read/write for most
        mmu.WriteByte(0xFF40, 0x80); // LCDC
        Assert.Equal(0x80, mmu.ReadByte(0xFF40));

        mmu.WriteByte(0xFF42, 0x10); // SCY
        Assert.Equal(0x10, mmu.ReadByte(0xFF42));

        // LY - read-only, writes ignored
        var lyBefore = mmu.ReadByte(0xFF44);
        mmu.WriteByte(0xFF44, 0xFF);
        var lyAfter = mmu.ReadByte(0xFF44);
        Assert.Equal(lyBefore, lyAfter);
    }
}