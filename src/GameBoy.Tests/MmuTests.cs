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
    public void ReadWord_LittleEndianOrder()
    {
        var mmu = new Mmu();
        ushort addr = 0xC000;
        mmu.WriteByte(addr, 0x34);     // Low byte
        mmu.WriteByte((ushort)(addr + 1), 0x12); // High byte
        var word = mmu.ReadWord(addr);
        Assert.Equal(0x1234, word);
    }

    [Fact]
    public void WriteWord_LittleEndianOrder()
    {
        var mmu = new Mmu();
        ushort addr = 0xC000;
        mmu.WriteWord(addr, 0xABCD);
        var lowByte = mmu.ReadByte(addr);
        var highByte = mmu.ReadByte((ushort)(addr + 1));
        Assert.Equal(0xCD, lowByte);   // Low byte
        Assert.Equal(0xAB, highByte);  // High byte
    }

    [Fact]
    public void ReadWord_CrossesPageBoundary()
    {
        var mmu = new Mmu();
        ushort addr = 0xCFFF; // Cross page boundary
        mmu.WriteByte(addr, 0x56);
        mmu.WriteByte((ushort)(addr + 1), 0x78);
        var word = mmu.ReadWord(addr);
        Assert.Equal(0x7856, word);
    }

    [Fact]
    public void EchoRam_MirrorsWorkRam_Read()
    {
        var mmu = new Mmu();
        ushort workRamAddr = 0xC123;
        ushort echoRamAddr = 0xE123; // Same offset in Echo RAM
        mmu.WriteByte(workRamAddr, 0x42);
        var echoValue = mmu.ReadByte(echoRamAddr);
        Assert.Equal(0x42, echoValue);
    }

    [Fact]
    public void EchoRam_MirrorsWorkRam_Write()
    {
        var mmu = new Mmu();
        ushort workRamAddr = 0xC456;
        ushort echoRamAddr = 0xE456; // Same offset in Echo RAM
        mmu.WriteByte(echoRamAddr, 0x84);
        var workValue = mmu.ReadByte(workRamAddr);
        Assert.Equal(0x84, workValue);
    }

    [Fact]
    public void EchoRam_EntireRange_Read()
    {
        var mmu = new Mmu();
        // Test boundaries
        mmu.WriteByte(0xC000, 0x11); // Start of Work RAM
        mmu.WriteByte(0xDDFF, 0x22); // End of Work RAM

        Assert.Equal(0x11, mmu.ReadByte(0xE000)); // Start of Echo RAM
        Assert.Equal(0x22, mmu.ReadByte(0xFDFF)); // End of Echo RAM
    }

    [Fact]
    public void UnusableRegion_ReadsReturnFF()
    {
        var mmu = new Mmu();
        // Test the unusable range 0xFEA0-0xFEFF
        for (ushort addr = 0xFEA0; addr <= 0xFEFF; addr++)
        {
            var value = mmu.ReadByte(addr);
            Assert.Equal(0xFF, value);
        }
    }

    [Fact]
    public void UnusableRegion_WritesIgnored()
    {
        var mmu = new Mmu();
        ushort addr = 0xFEA0;
        mmu.WriteByte(addr, 0x42);
        var value = mmu.ReadByte(addr);
        Assert.Equal(0xFF, value); // Should still return 0xFF
    }

    [Fact]
    public void CartridgeRom_NoCartridge_ReturnsFF()
    {
        var mmu = new Mmu();
        for (ushort addr = 0x0000; addr < 0x8000; addr += 0x1000)
        {
            var value = mmu.ReadByte(addr);
            Assert.Equal(0xFF, value);
        }
    }

    [Fact]
    public void CartridgeRom_WithCartridge_ReturnsRomData()
    {
        var mmu = new Mmu();
        var rom = new byte[0x8000];
        rom[0x0000] = 0x12;
        rom[0x4000] = 0x34;
        rom[0x7FFF] = 0x56;
        
        mmu.LoadRom(rom);
        
        Assert.Equal(0x12, mmu.ReadByte(0x0000));
        Assert.Equal(0x34, mmu.ReadByte(0x4000));
        Assert.Equal(0x56, mmu.ReadByte(0x7FFF));
    }

    [Fact]
    public void InterruptEnable_ReadWrite()
    {
        var mmu = new Mmu();
        ushort ieAddr = 0xFFFF;
        mmu.WriteByte(ieAddr, 0xE5);
        var value = mmu.ReadByte(ieAddr);
        Assert.Equal(0xE5, value);
    }

    [Fact]
    public void IoRegion_UnmappedAddresses_ReturnFF()
    {
        var mmu = new Mmu();
        // Test some unmapped I/O addresses
        Assert.Equal(0xFF, mmu.ReadByte(0xFF00));
        Assert.Equal(0xFF, mmu.ReadByte(0xFF10));
        Assert.Equal(0xFF, mmu.ReadByte(0xFF40));
        Assert.Equal(0xFF, mmu.ReadByte(0xFF7F));
    }

    [Fact]
    public void Vram_ReadWrite()
    {
        var mmu = new Mmu();
        ushort addr = 0x8000;
        mmu.WriteByte(addr, 0x99);
        var value = mmu.ReadByte(addr);
        Assert.Equal(0x99, value);
    }

    [Fact]
    public void Hram_ReadWrite()
    {
        var mmu = new Mmu();
        ushort addr = 0xFF80;
        mmu.WriteByte(addr, 0x77);
        var value = mmu.ReadByte(addr);
        Assert.Equal(0x77, value);
    }

    [Fact]
    public void Oam_ReadWrite()
    {
        var mmu = new Mmu();
        ushort addr = 0xFE00;
        mmu.WriteByte(addr, 0x88);
        var value = mmu.ReadByte(addr);
        Assert.Equal(0x88, value);
    }

    [Fact]
    public void MemoryMap_Boundaries()
    {
        var mmu = new Mmu();
        
        // Test work RAM end and echo RAM start
        mmu.WriteByte(0xDDFF, 0x11); // Last byte of mirrored work RAM
        mmu.WriteByte(0xE000, 0x22); // First byte of echo RAM
        
        // Echo RAM should mirror work RAM
        Assert.Equal(0x22, mmu.ReadByte(0xC000)); // First byte of work RAM should be 0x22
        Assert.Equal(0x11, mmu.ReadByte(0xFDFF)); // Last byte of echo RAM should be 0x11
        
        // Test boundary between echo RAM and OAM
        Assert.Equal(0x11, mmu.ReadByte(0xFDFF)); // Last echo RAM address
        mmu.WriteByte(0xFE00, 0x33); // First OAM address
        Assert.Equal(0x33, mmu.ReadByte(0xFE00));
        
        // Test boundary between OAM and unusable
        mmu.WriteByte(0xFE9F, 0x44); // Last OAM address
        Assert.Equal(0x44, mmu.ReadByte(0xFE9F));
        Assert.Equal(0xFF, mmu.ReadByte(0xFEA0)); // First unusable address
    }
}
