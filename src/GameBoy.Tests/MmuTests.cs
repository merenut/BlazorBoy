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
}
