using GameBoy.Core;

namespace GameBoy.Tests;

public class CpuTests
{
    [Fact]
    public void Ld_r_r_CopiesRegister()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.B = 0x12;
        cpu.Regs.C = 0x34;
        // Place instruction in RAM and point PC there
        cpu.Regs.PC = 0xC000;
        // Opcode 0x41: LD B,C
        mmu.WriteByte(0xC000, 0x41);
        var cycles = cpu.Step();
        Assert.Equal(0x34, cpu.Regs.B);
        Assert.Equal(4, cycles);
    }
}
