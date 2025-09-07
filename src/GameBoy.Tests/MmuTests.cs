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
    public void JOYP_ReadsDefaultValue()
    {
        var mmu = new Mmu();
        var val = mmu.ReadByte(Mmu.JOYP);
        Assert.Equal(0xCF, val);
    }

    [Fact]
    public void JOYP_WritesOnlyAffectBits4And5()
    {
        var mmu = new Mmu();
        mmu.WriteByte(Mmu.JOYP, 0x00); // Try to clear all bits
        var val = mmu.ReadByte(Mmu.JOYP);
        Assert.Equal(0xCF, val); // Lower 4 bits should still be 1s

        mmu.WriteByte(Mmu.JOYP, 0x10); // Set bit 4
        val = mmu.ReadByte(Mmu.JOYP);
        Assert.Equal(0xDF, val); // Should be 0xCF with bit 4 set

        mmu.WriteByte(Mmu.JOYP, 0x20); // Set bit 5 
        val = mmu.ReadByte(Mmu.JOYP);
        Assert.Equal(0xEF, val); // Should be 0xCF with bit 5 set
    }

    [Fact]
    public void DIV_ReadsDefaultValue()
    {
        var mmu = new Mmu();
        var val = mmu.ReadByte(Mmu.DIV);
        Assert.Equal(0x00, val);
    }

    [Fact]
    public void DIV_WriteResetsToZero()
    {
        var mmu = new Mmu();
        // Simulate DIV having some value
        mmu.WriteByte(Mmu.DIV, 0x42); // Any write should reset to 0
        var val = mmu.ReadByte(Mmu.DIV);
        Assert.Equal(0x00, val);

        mmu.WriteByte(Mmu.DIV, 0xFF); // Another write should also reset to 0
        val = mmu.ReadByte(Mmu.DIV);
        Assert.Equal(0x00, val);
    }

    [Fact]
    public void TimerRegisters_ReadDefaultValues()
    {
        var mmu = new Mmu();
        Assert.Equal(0x00, mmu.ReadByte(Mmu.TIMA));
        Assert.Equal(0x00, mmu.ReadByte(Mmu.TMA));
        Assert.Equal(0xF8, mmu.ReadByte(Mmu.TAC));
    }

    [Fact]
    public void TimerRegisters_WriteAndRead()
    {
        var mmu = new Mmu();

        mmu.WriteByte(Mmu.TIMA, 0x42);
        Assert.Equal(0x42, mmu.ReadByte(Mmu.TIMA));

        mmu.WriteByte(Mmu.TMA, 0x84);
        Assert.Equal(0x84, mmu.ReadByte(Mmu.TMA));

        mmu.WriteByte(Mmu.TAC, 0x07);
        Assert.Equal(0x07, mmu.ReadByte(Mmu.TAC));
    }

    [Fact]
    public void IF_ReadsDefaultValue()
    {
        var mmu = new Mmu();
        var val = mmu.ReadByte(Mmu.IF);
        Assert.Equal(0xE1, val);
    }

    [Fact]
    public void IF_WriteMasksToLower5Bits()
    {
        var mmu = new Mmu();
        mmu.WriteByte(Mmu.IF, 0x00); // Clear all bits
        var val = mmu.ReadByte(Mmu.IF);
        Assert.Equal(0xE0, val); // Upper 3 bits should still be 1s

        mmu.WriteByte(Mmu.IF, 0x1F); // Set lower 5 bits
        val = mmu.ReadByte(Mmu.IF);
        Assert.Equal(0xFF, val); // Should be 0xE0 | 0x1F

        mmu.WriteByte(Mmu.IF, 0xFF); // Try to set all bits
        val = mmu.ReadByte(Mmu.IF);
        Assert.Equal(0xFF, val); // Upper bits forced to 1, lower 5 written
    }

    [Fact]
    public void PPU_Registers_ReadDefaultValues()
    {
        var mmu = new Mmu();
        Assert.Equal(0x91, mmu.ReadByte(Mmu.LCDC));
        Assert.Equal(0x85, mmu.ReadByte(Mmu.STAT));
        Assert.Equal(0x00, mmu.ReadByte(Mmu.SCY));
        Assert.Equal(0x00, mmu.ReadByte(Mmu.SCX));
        Assert.Equal(0x00, mmu.ReadByte(Mmu.LY));
        Assert.Equal(0x00, mmu.ReadByte(Mmu.LYC));
        Assert.Equal(0xFC, mmu.ReadByte(Mmu.BGP));
        Assert.Equal(0x00, mmu.ReadByte(Mmu.OBP0));
        Assert.Equal(0x00, mmu.ReadByte(Mmu.OBP1));
        Assert.Equal(0x00, mmu.ReadByte(Mmu.WY));
        Assert.Equal(0x00, mmu.ReadByte(Mmu.WX));
    }

    [Fact]
    public void PPU_Registers_WriteAndRead()
    {
        var mmu = new Mmu();

        mmu.WriteByte(Mmu.LCDC, 0x80);
        Assert.Equal(0x80, mmu.ReadByte(Mmu.LCDC));

        mmu.WriteByte(Mmu.SCY, 0x10);
        Assert.Equal(0x10, mmu.ReadByte(Mmu.SCY));

        mmu.WriteByte(Mmu.SCX, 0x20);
        Assert.Equal(0x20, mmu.ReadByte(Mmu.SCX));

        mmu.WriteByte(Mmu.LYC, 0x90);
        Assert.Equal(0x90, mmu.ReadByte(Mmu.LYC));

        mmu.WriteByte(Mmu.BGP, 0xE4);
        Assert.Equal(0xE4, mmu.ReadByte(Mmu.BGP));
    }

    [Fact]
    public void LY_WriteIsIgnored()
    {
        var mmu = new Mmu();
        var originalValue = mmu.ReadByte(Mmu.LY);

        mmu.WriteByte(Mmu.LY, 0xFF); // Try to write to LY
        var newValue = mmu.ReadByte(Mmu.LY);

        Assert.Equal(originalValue, newValue); // Should be unchanged
    }

    [Fact]
    public void STAT_WritableBitsOnly()
    {
        var mmu = new Mmu();

        // STAT default is 0x85, writable bits are 6,5,4,3 (mask 0x78)
        mmu.WriteByte(Mmu.STAT, 0xFF); // Try to set all bits
        var val = mmu.ReadByte(Mmu.STAT);

        // Should be (0x85 & 0x87) | (0xFF & 0x78) = 0x85 | 0x78 = 0xFD
        Assert.Equal(0xFD, val);

        mmu.WriteByte(Mmu.STAT, 0x00); // Try to clear all bits
        val = mmu.ReadByte(Mmu.STAT);

        // Should be (0x85 & 0x87) | (0x00 & 0x78) = 0x85 | 0x00 = 0x85  
        Assert.Equal(0x85, val);
    }

    [Fact]
    public void DMA_WriteAndRead()
    {
        var mmu = new Mmu();

        mmu.WriteByte(Mmu.DMA, 0x42);
        Assert.Equal(0x42, mmu.ReadByte(Mmu.DMA));

        mmu.WriteByte(Mmu.DMA, 0xC0);
        Assert.Equal(0xC0, mmu.ReadByte(Mmu.DMA));
    }

    [Fact]
    public void Unstubbed_IORegisters_ReadAs0xFF()
    {
        var mmu = new Mmu();

        // Test some unstubbed I/O addresses
        Assert.Equal(0xFF, mmu.ReadByte(0xFF01)); // Serial transfer data
        Assert.Equal(0xFF, mmu.ReadByte(0xFF02)); // Serial transfer control
        Assert.Equal(0xFF, mmu.ReadByte(0xFF10)); // Sound channel 1 sweep
        Assert.Equal(0xFF, mmu.ReadByte(0xFF30)); // Wave pattern
        Assert.Equal(0xFF, mmu.ReadByte(0xFF7F)); // Last I/O address
    }

    [Fact]
    public void Unstubbed_IORegisters_WritesIgnored()
    {
        var mmu = new Mmu();

        // Write to unstubbed I/O registers should be ignored
        mmu.WriteByte(0xFF01, 0x42);
        Assert.Equal(0xFF, mmu.ReadByte(0xFF01)); // Should still read as 0xFF

        mmu.WriteByte(0xFF10, 0x84);
        Assert.Equal(0xFF, mmu.ReadByte(0xFF10)); // Should still read as 0xFF
    }

    [Fact]
    public void EchoRam_MirrorsWorkRam()
    {
        var mmu = new Mmu();

        // Write to Work RAM, read from Echo RAM
        mmu.WriteByte(0xC000, 0x42);
        Assert.Equal(0x42, mmu.ReadByte(0xE000)); // Echo RAM should mirror Work RAM

        mmu.WriteByte(0xC100, 0x84);
        Assert.Equal(0x84, mmu.ReadByte(0xE100)); // Test another address in the range

        // Write to Echo RAM, read from Work RAM
        mmu.WriteByte(0xE200, 0xAA);
        Assert.Equal(0xAA, mmu.ReadByte(0xC200)); // Work RAM should mirror Echo RAM writes

        mmu.WriteByte(0xFDFF, 0xBB); // Last address in Echo RAM range
        Assert.Equal(0xBB, mmu.ReadByte(0xDDFF)); // Should mirror to last address in Work RAM range
    }

    [Fact]
    public void UnusableRegion_ReadsReturn0xFF()
    {
        var mmu = new Mmu();

        // Test various addresses in the unusable region (0xFEA0-0xFEFF)
        Assert.Equal(0xFF, mmu.ReadByte(0xFEA0)); // First address in unusable region
        Assert.Equal(0xFF, mmu.ReadByte(0xFEB0)); // Middle address
        Assert.Equal(0xFF, mmu.ReadByte(0xFEFF)); // Last address in unusable region
    }

    [Fact]
    public void UnusableRegion_WritesIgnored()
    {
        var mmu = new Mmu();

        // Writes to unusable region should be ignored
        mmu.WriteByte(0xFEA0, 0x42);
        Assert.Equal(0xFF, mmu.ReadByte(0xFEA0)); // Should still read as 0xFF

        mmu.WriteByte(0xFEB0, 0x84);
        Assert.Equal(0xFF, mmu.ReadByte(0xFEB0)); // Should still read as 0xFF

        mmu.WriteByte(0xFEFF, 0xAA);
        Assert.Equal(0xFF, mmu.ReadByte(0xFEFF)); // Should still read as 0xFF
    }

    [Fact]
    public void IE_Register_ReadWriteRoundTrip()
    {
        var mmu = new Mmu();

        // IE register (0xFFFF) should allow full read/write without masking
        mmu.WriteByte(0xFFFF, 0x00);
        Assert.Equal(0x00, mmu.ReadByte(0xFFFF));

        mmu.WriteByte(0xFFFF, 0xFF);
        Assert.Equal(0xFF, mmu.ReadByte(0xFFFF));

        mmu.WriteByte(0xFFFF, 0x1F); // Only lower 5 bits set
        Assert.Equal(0x1F, mmu.ReadByte(0xFFFF));

        mmu.WriteByte(0xFFFF, 0xE0); // Only upper 3 bits set
        Assert.Equal(0xE0, mmu.ReadByte(0xFFFF));

        mmu.WriteByte(0xFFFF, 0x42); // Arbitrary value
        Assert.Equal(0x42, mmu.ReadByte(0xFFFF));
    }

    [Fact]
    public void ReadWord_LittleEndian()
    {
        var mmu = new Mmu();

        // Write two bytes and read as 16-bit word
        mmu.WriteByte(0xC000, 0x34); // Low byte
        mmu.WriteByte(0xC001, 0x12); // High byte

        ushort word = mmu.ReadWord(0xC000);
        Assert.Equal(0x1234, word); // Should be little-endian: 0x12 << 8 | 0x34
    }

    [Fact]
    public void WriteWord_LittleEndian()
    {
        var mmu = new Mmu();

        // Write 16-bit word and read individual bytes
        mmu.WriteWord(0xC000, 0x5678);

        Assert.Equal(0x78, mmu.ReadByte(0xC000)); // Low byte should be 0x78
        Assert.Equal(0x56, mmu.ReadByte(0xC001)); // High byte should be 0x56
    }

    [Fact]
    public void ReadWriteWord_CrossesRegionBoundaries()
    {
        var mmu = new Mmu();

        // Test word operations across normal memory boundaries
        mmu.WriteWord(0xC100, 0xABCD); // Simple case in Work RAM

        Assert.Equal(0xCD, mmu.ReadByte(0xC100)); // Low byte
        Assert.Equal(0xAB, mmu.ReadByte(0xC101)); // High byte

        // Read the word back
        ushort word = mmu.ReadWord(0xC100);
        Assert.Equal(0xABCD, word);

        // Test that the Echo RAM correctly mirrors the same location
        Assert.Equal(0xCD, mmu.ReadByte(0xE100)); // Echo RAM mirror (0xC100 + 0x2000)
        Assert.Equal(0xAB, mmu.ReadByte(0xE101)); // Echo RAM mirror (0xC101 + 0x2000)

        // Read word from Echo RAM should match
        ushort echoWord = mmu.ReadWord(0xE100);
        Assert.Equal(0xABCD, echoWord);
    }
}
