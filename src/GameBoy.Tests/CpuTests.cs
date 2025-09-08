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

    [Fact]
    public void Ld_r_d8_LoadsImmediateByte()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;

        // Test LD A,d8 (0x3E)
        mmu.WriteByte(0xC000, 0x3E);
        mmu.WriteByte(0xC001, 0x42);

        var cycles = cpu.Step();

        Assert.Equal(0x42, cpu.Regs.A);
        Assert.Equal(8, cycles);
        Assert.Equal(0xC002, cpu.Regs.PC); // PC should advance by 2
    }

    [Fact]
    public void Add_A_r_AddsToRegisterA()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0x10;
        cpu.Regs.B = 0x20;

        // Test ADD A,B (0x80)
        mmu.WriteByte(0xC000, 0x80);

        var cycles = cpu.Step();

        Assert.Equal(0x30, cpu.Regs.A);
        Assert.Equal(4, cycles);
        Assert.Equal(0xC001, cpu.Regs.PC); // PC should advance by 1
    }

    [Fact]
    public void Add_A_d8_AddsImmediateByte()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0x10;

        // Test ADD A,d8 (0xC6)
        mmu.WriteByte(0xC000, 0xC6);
        mmu.WriteByte(0xC001, 0x25);

        var cycles = cpu.Step();

        Assert.Equal(0x35, cpu.Regs.A);
        Assert.Equal(8, cycles);
        Assert.Equal(0xC002, cpu.Regs.PC); // PC should advance by 2
    }

    [Fact]
    public void Add_SetsFlags_Correctly()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;

        // Test zero flag
        cpu.Regs.A = 0x00;
        cpu.Regs.B = 0x00;
        mmu.WriteByte(0xC000, 0x80); // ADD A,B
        cpu.Step();
        Assert.Equal(0x80, cpu.Regs.F); // Zero flag set

        // Test carry flag (0xFF + 0x01 = 0x100, result 0x00, zero, half-carry and carry set)
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0xFF;
        cpu.Regs.B = 0x01;
        cpu.Step();
        Assert.Equal(0xB0, cpu.Regs.F); // Zero, half-carry and carry flags set (0x80 | 0x20 | 0x10)

        // Test half-carry flag (0x0F + 0x01 = 0x10, only half-carry)
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0x0F;
        cpu.Regs.B = 0x01;
        cpu.Step();
        Assert.Equal(0x20, cpu.Regs.F); // Half-carry flag set

        // Test normal addition without flags (0x10 + 0x20 = 0x30, no flags)
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0x10;
        cpu.Regs.B = 0x20;
        cpu.Step();
        Assert.Equal(0x00, cpu.Regs.F); // No flags set
    }

    [Fact]
    public void Nop_DoesNothing()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        var originalRegs = cpu.Regs;

        // Test NOP (0x00)
        mmu.WriteByte(0xC000, 0x00);

        var cycles = cpu.Step();

        Assert.Equal(4, cycles);
        Assert.Equal(0xC001, cpu.Regs.PC); // PC should advance by 1
        // All other registers should be unchanged
        Assert.Equal(originalRegs.A, cpu.Regs.A);
        Assert.Equal(originalRegs.F, cpu.Regs.F);
        Assert.Equal(originalRegs.BC, cpu.Regs.BC);
        Assert.Equal(originalRegs.DE, cpu.Regs.DE);
        Assert.Equal(originalRegs.HL, cpu.Regs.HL);
        Assert.Equal(originalRegs.SP, cpu.Regs.SP);
    }
}
