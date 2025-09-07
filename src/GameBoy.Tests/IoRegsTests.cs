using GameBoy.Core;

namespace GameBoy.Tests;

public class IoRegsTests
{
    [Fact]
    public void IoRegs_ShouldHaveCorrectAddresses()
    {
        // Test some key I/O register addresses
        Assert.Equal(0xFF00, IoRegs.P1_JOYP);
        Assert.Equal(0xFF01, IoRegs.SB);
        Assert.Equal(0xFF04, IoRegs.DIV);
        Assert.Equal(0xFF0F, IoRegs.IF);
        Assert.Equal(0xFF40, IoRegs.LCDC);
        Assert.Equal(0xFF44, IoRegs.LY);
        Assert.Equal(0xFFFF, IoRegs.IE);
    }

    [Fact]
    public void IoRegs_ShouldBeInCorrectRange()
    {
        // Test that I/O registers are within I/O range (except IE)
        Assert.True(IoRegs.P1_JOYP >= 0xFF00 && IoRegs.P1_JOYP <= 0xFF7F);
        Assert.True(IoRegs.LCDC >= 0xFF00 && IoRegs.LCDC <= 0xFF7F);
        Assert.True(IoRegs.TAC >= 0xFF00 && IoRegs.TAC <= 0xFF7F);

        // IE is a special case outside the I/O range
        Assert.Equal(0xFFFF, IoRegs.IE);
    }

    [Fact]
    public void Mmu_ShouldUseIoRegsConstants()
    {
        // Test that Mmu uses IoRegs constants
        Assert.Equal(IoRegs.P1_JOYP, Mmu.IoStart);
        Assert.Equal(IoRegs.IE, Mmu.InterruptEnable);
    }
}