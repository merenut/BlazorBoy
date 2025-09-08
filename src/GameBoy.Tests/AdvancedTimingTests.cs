using GameBoy.Core;

namespace GameBoy.Tests;

/// <summary>
/// Advanced timing tests for edge cases and special instructions.
/// </summary>
public class AdvancedTimingTests
{
    #region Special Memory Instructions

    [Fact]
    public void LoadAbsoluteA_Takes16Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;

        // Test LD A,(a16) (0xFA)
        mmu.WriteByte(0xC000, 0xFA);
        mmu.WriteByte(0xC001, 0x00);
        mmu.WriteByte(0xC002, 0xD0);
        mmu.WriteByte(0xD000, 0x42);
        var cycles = cpu.Step();

        Assert.Equal(16, cycles);
    }

    [Fact]
    public void StoreAbsoluteA_Takes16Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0x42;

        // Test LD (a16),A (0xEA)
        mmu.WriteByte(0xC000, 0xEA);
        mmu.WriteByte(0xC001, 0x00);
        mmu.WriteByte(0xC002, 0xD0);
        var cycles = cpu.Step();

        Assert.Equal(16, cycles);
    }

    [Fact]
    public void StoreSPAbsolute_Takes20Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.SP = 0xFFFE;

        // Test LD (a16),SP (0x08)
        mmu.WriteByte(0xC000, 0x08);
        mmu.WriteByte(0xC001, 0x00);
        mmu.WriteByte(0xC002, 0xD0);
        var cycles = cpu.Step();

        Assert.Equal(20, cycles);
    }

    [Fact]
    public void LoadHLFromSPPlusOffset_Takes12Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.SP = 0xFFF0;

        // Test LD HL,SP+r8 (0xF8)
        mmu.WriteByte(0xC000, 0xF8);
        mmu.WriteByte(0xC001, 0x10); // Offset +16
        var cycles = cpu.Step();

        Assert.Equal(12, cycles);
    }

    [Fact]
    public void LoadSPFromHL_Takes8Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.HL = 0xFFFE;

        // Test LD SP,HL (0xF9)
        mmu.WriteByte(0xC000, 0xF9);
        var cycles = cpu.Step();

        Assert.Equal(8, cycles);
    }

    #endregion

    #region High Memory Instructions

    [Fact]
    public void LoadHighMemoryImmediate_Takes12Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;

        // Test LDH A,(a8) (0xF0)
        mmu.WriteByte(0xC000, 0xF0);
        mmu.WriteByte(0xC001, 0x80);
        mmu.WriteByte(0xFF80, 0x42);
        var cycles = cpu.Step();

        Assert.Equal(12, cycles);
    }

    [Fact]
    public void StoreHighMemoryImmediate_Takes12Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0x42;

        // Test LDH (a8),A (0xE0)
        mmu.WriteByte(0xC000, 0xE0);
        mmu.WriteByte(0xC001, 0x80);
        var cycles = cpu.Step();

        Assert.Equal(12, cycles);
    }

    [Fact]
    public void LoadHighMemoryC_Takes8Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.C = 0x80;

        // Test LDH A,(C) (0xF2)
        mmu.WriteByte(0xC000, 0xF2);
        mmu.WriteByte(0xFF80, 0x42);
        var cycles = cpu.Step();

        Assert.Equal(8, cycles);
    }

    [Fact]
    public void StoreHighMemoryC_Takes8Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0x42;
        cpu.Regs.C = 0x80;

        // Test LDH (C),A (0xE2)
        mmu.WriteByte(0xC000, 0xE2);
        var cycles = cpu.Step();

        Assert.Equal(8, cycles);
    }

    #endregion

    #region Load and Increment/Decrement Instructions

    [Fact]
    public void LoadHLI_Takes8Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.HL = 0xD000;

        // Test LD A,(HL+) (0x2A)
        mmu.WriteByte(0xC000, 0x2A);
        mmu.WriteByte(0xD000, 0x42);
        var cycles = cpu.Step();

        Assert.Equal(8, cycles);
        Assert.Equal(0xD001, cpu.Regs.HL); // Should increment HL
    }

    [Fact]
    public void LoadHLD_Takes8Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.HL = 0xD000;

        // Test LD A,(HL-) (0x3A)
        mmu.WriteByte(0xC000, 0x3A);
        mmu.WriteByte(0xD000, 0x42);
        var cycles = cpu.Step();

        Assert.Equal(8, cycles);
        Assert.Equal(0xCFFF, cpu.Regs.HL); // Should decrement HL
    }

    [Fact]
    public void StoreHLI_Takes8Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.HL = 0xD000;
        cpu.Regs.A = 0x42;

        // Test LD (HL+),A (0x22)
        mmu.WriteByte(0xC000, 0x22);
        var cycles = cpu.Step();

        Assert.Equal(8, cycles);
        Assert.Equal(0xD001, cpu.Regs.HL); // Should increment HL
    }

    [Fact]
    public void StoreHLD_Takes8Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.HL = 0xD000;
        cpu.Regs.A = 0x42;

        // Test LD (HL-),A (0x32)
        mmu.WriteByte(0xC000, 0x32);
        var cycles = cpu.Step();

        Assert.Equal(8, cycles);
        Assert.Equal(0xCFFF, cpu.Regs.HL); // Should decrement HL
    }

    #endregion

    #region Additional Conditional Branches

    [Fact]
    public void ConditionalRelativeJumpCarryTaken_Takes12Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.F = 0x10; // Set Carry flag

        // Test JR C,r8 (0x38)
        mmu.WriteByte(0xC000, 0x38);
        mmu.WriteByte(0xC001, 0x10);
        var cycles = cpu.Step();

        Assert.Equal(12, cycles);
    }

    [Fact]
    public void ConditionalRelativeJumpCarryNotTaken_Takes8Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.F = 0x00; // Clear Carry flag

        // Test JR C,r8 (0x38)
        mmu.WriteByte(0xC000, 0x38);
        mmu.WriteByte(0xC001, 0x10);
        var cycles = cpu.Step();

        Assert.Equal(8, cycles);
    }

    [Fact]
    public void ConditionalRelativeJumpNoCarryTaken_Takes12Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.F = 0x00; // Clear Carry flag

        // Test JR NC,r8 (0x30)
        mmu.WriteByte(0xC000, 0x30);
        mmu.WriteByte(0xC001, 0x10);
        var cycles = cpu.Step();

        Assert.Equal(12, cycles);
    }

    [Fact]
    public void ConditionalRelativeJumpNoCarryNotTaken_Takes8Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.F = 0x10; // Set Carry flag

        // Test JR NC,r8 (0x30)
        mmu.WriteByte(0xC000, 0x30);
        mmu.WriteByte(0xC001, 0x10);
        var cycles = cpu.Step();

        Assert.Equal(8, cycles);
    }

    #endregion

    #region CB Instruction Edge Cases

    [Fact]
    public void CBShiftMemory_Takes16Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.HL = 0xD000;

        // Test SLA (HL) (0xCB 0x26)
        mmu.WriteByte(0xC000, 0xCB);
        mmu.WriteByte(0xC001, 0x26);
        mmu.WriteByte(0xD000, 0x80);
        var cycles = cpu.Step();

        Assert.Equal(16, cycles);
    }

    [Fact]
    public void CBSwapMemory_Takes16Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.HL = 0xD000;

        // Test SWAP (HL) (0xCB 0x36)
        mmu.WriteByte(0xC000, 0xCB);
        mmu.WriteByte(0xC001, 0x36);
        mmu.WriteByte(0xD000, 0x12);
        var cycles = cpu.Step();

        Assert.Equal(16, cycles);
    }

    [Fact]
    public void CBSwapRegister_Takes8Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0x12;

        // Test SWAP A (0xCB 0x37)
        mmu.WriteByte(0xC000, 0xCB);
        mmu.WriteByte(0xC001, 0x37);
        var cycles = cpu.Step();

        Assert.Equal(8, cycles);
    }

    #endregion
}