using GameBoy.Core;

namespace GameBoy.Tests;

public class TestResetBehavior
{
    [Fact]
    public void Reset_RestoresAllIORegisterDefaults()
    {
        var mmu = new Mmu();
        
        // Modify all I/O registers to non-default values
        mmu.WriteByte(Mmu.JOYP, 0x00);
        mmu.WriteByte(Mmu.DIV, 0x42);
        mmu.WriteByte(Mmu.TIMA, 0x11);
        mmu.WriteByte(Mmu.TMA, 0x22);
        mmu.WriteByte(Mmu.TAC, 0x33);
        mmu.WriteByte(Mmu.IF, 0x1F);
        mmu.WriteByte(Mmu.LCDC, 0x44);
        mmu.WriteByte(Mmu.STAT, 0x78);
        mmu.WriteByte(Mmu.SCY, 0x55);
        mmu.WriteByte(Mmu.SCX, 0x66);
        mmu.WriteByte(Mmu.LYC, 0x77);
        mmu.WriteByte(Mmu.DMA, 0x88);
        mmu.WriteByte(Mmu.BGP, 0x99);
        mmu.WriteByte(Mmu.OBP0, 0xAA);
        mmu.WriteByte(Mmu.OBP1, 0xBB);
        mmu.WriteByte(Mmu.WY, 0xCC);
        mmu.WriteByte(Mmu.WX, 0xDD);
        mmu.WriteByte(0xFFFF, 0xEE); // IE register
        
        // Reset MMU
        mmu.Reset();
        
        // Verify all registers are restored to post-BIOS defaults
        Assert.Equal(0xCF, mmu.ReadByte(Mmu.JOYP));
        Assert.Equal(0x00, mmu.ReadByte(Mmu.DIV));
        Assert.Equal(0x00, mmu.ReadByte(Mmu.TIMA));
        Assert.Equal(0x00, mmu.ReadByte(Mmu.TMA));
        Assert.Equal(0xF8, mmu.ReadByte(Mmu.TAC));
        Assert.Equal(0xE1, mmu.ReadByte(Mmu.IF));
        Assert.Equal(0x91, mmu.ReadByte(Mmu.LCDC));
        Assert.Equal(0x85, mmu.ReadByte(Mmu.STAT));
        Assert.Equal(0x00, mmu.ReadByte(Mmu.SCY));
        Assert.Equal(0x00, mmu.ReadByte(Mmu.SCX));
        Assert.Equal(0x00, mmu.ReadByte(Mmu.LY));
        Assert.Equal(0x00, mmu.ReadByte(Mmu.LYC));
        Assert.Equal(0xFF, mmu.ReadByte(Mmu.DMA));
        Assert.Equal(0xFC, mmu.ReadByte(Mmu.BGP));
        Assert.Equal(0x00, mmu.ReadByte(Mmu.OBP0));
        Assert.Equal(0x00, mmu.ReadByte(Mmu.OBP1));
        Assert.Equal(0x00, mmu.ReadByte(Mmu.WY));
        Assert.Equal(0x00, mmu.ReadByte(Mmu.WX));
        Assert.Equal(0x00, mmu.ReadByte(0xFFFF)); // IE register
    }
}