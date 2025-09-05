using GameBoy.Core;

namespace GameBoy.Tests;

public class MmuTests
{
    [Fact]
    public void ReadWrite_WorkInRamRegion()
    {
        var mmu = new Mmu();
        ushort addr = 0xC000; // Work RAM
        mmu.WriteByte(addr, 0xAB);
        var val = mmu.ReadByte(addr);
        Assert.Equal(0xAB, val);
    }

    [Fact]
    public void PostBiosDefaults_IoRegistersInitializedCorrectly()
    {
        var mmu = new Mmu();
        
        // Test key I/O register defaults
        Assert.Equal(0xCF, mmu.ReadByte(0xFF00)); // JOYP
        Assert.Equal(0x00, mmu.ReadByte(0xFF01)); // SB
        Assert.Equal(0x7E, mmu.ReadByte(0xFF02)); // SC
        Assert.Equal(0x00, mmu.ReadByte(0xFF04)); // DIV
        Assert.Equal(0x00, mmu.ReadByte(0xFF05)); // TIMA
        Assert.Equal(0x00, mmu.ReadByte(0xFF06)); // TMA
        Assert.Equal(0xF8, mmu.ReadByte(0xFF07)); // TAC
        Assert.Equal(0xE1, mmu.ReadByte(0xFF0F)); // IF
        Assert.Equal(0x91, mmu.ReadByte(0xFF40)); // LCDC
        Assert.Equal(0x85, mmu.ReadByte(0xFF41)); // STAT
        Assert.Equal(0x00, mmu.ReadByte(0xFF42)); // SCY
        Assert.Equal(0x00, mmu.ReadByte(0xFF43)); // SCX
        Assert.Equal(0x00, mmu.ReadByte(0xFF44)); // LY
        Assert.Equal(0x00, mmu.ReadByte(0xFF45)); // LYC
        Assert.Equal(0xFF, mmu.ReadByte(0xFF46)); // DMA
        Assert.Equal(0xFC, mmu.ReadByte(0xFF47)); // BGP
        Assert.Equal(0x00, mmu.ReadByte(0xFF48)); // OBP0
        Assert.Equal(0x00, mmu.ReadByte(0xFF49)); // OBP1
        Assert.Equal(0x00, mmu.ReadByte(0xFF4A)); // WY
        Assert.Equal(0x00, mmu.ReadByte(0xFF4B)); // WX
        Assert.Equal(0x00, mmu.ReadByte(0xFFFF)); // IE
    }

    [Fact]
    public void Reset_RestoresPostBiosDefaults()
    {
        var mmu = new Mmu();
        
        // Modify some I/O registers
        mmu.WriteByte(0xFF00, 0x12);
        mmu.WriteByte(0xFF40, 0x34);
        mmu.WriteByte(0xFF47, 0x56);
        
        // Reset should restore defaults
        mmu.Reset();
        
        Assert.Equal(0xCF, mmu.ReadByte(0xFF00)); // JOYP
        Assert.Equal(0x91, mmu.ReadByte(0xFF40)); // LCDC
        Assert.Equal(0xFC, mmu.ReadByte(0xFF47)); // BGP
    }
}
