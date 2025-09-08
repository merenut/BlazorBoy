using GameBoy.Core;

namespace GameBoy.Tests;

/// <summary>
/// Tests for core instruction groups to validate proper implementation.
/// </summary>
public class InstructionGroupTests
{
    #region ALU Tests

    [Fact]
    public void Add_A_Register_SetsCorrectFlags()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0x3C;
        cpu.Regs.B = 0x12;

        // Test ADD A,B (0x80)
        mmu.WriteByte(0xC000, 0x80);
        var cycles = cpu.Step();

        Assert.Equal(0x4E, cpu.Regs.A);
        Assert.Equal(4, cycles);
        Assert.False(cpu.GetZeroFlag());
        Assert.False(cpu.GetCarryFlag());
    }

    [Fact]
    public void Add_A_Immediate_SetsZeroAndCarryFlags()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0xFF;

        // Test ADD A,d8 (0xC6) with value 0x01
        mmu.WriteByte(0xC000, 0xC6);
        mmu.WriteByte(0xC001, 0x01);
        var cycles = cpu.Step();

        Assert.Equal(0x00, cpu.Regs.A);
        Assert.Equal(8, cycles);
        Assert.True(cpu.GetZeroFlag());
        Assert.True(cpu.GetCarryFlag());
    }

    [Fact]
    public void Xor_A_A_ClearsRegisterAndSetsZeroFlag()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0x55;

        // Test XOR A,A (0xAF)
        mmu.WriteByte(0xC000, 0xAF);
        var cycles = cpu.Step();

        Assert.Equal(0x00, cpu.Regs.A);
        Assert.Equal(4, cycles);
        Assert.True(cpu.GetZeroFlag());
        Assert.False(cpu.GetCarryFlag());
    }

    [Fact]
    public void Compare_A_Register_SetsCorrectFlags()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0x3C;
        cpu.Regs.B = 0x2F;

        // Test CP A,B (0xB8)
        mmu.WriteByte(0xC000, 0xB8);
        var cycles = cpu.Step();

        Assert.Equal(0x3C, cpu.Regs.A); // A unchanged by CP
        Assert.Equal(4, cycles);
        Assert.False(cpu.GetZeroFlag()); // A != B
        Assert.False(cpu.GetCarryFlag()); // A > B
    }

    #endregion

    #region Special Instruction Tests

    [Fact]
    public void DAA_AdjustsAccumulatorForBCD()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0x0F;
        cpu.Regs.B = 0x01;

        // First ADD A,B to get 0x10, then DAA should adjust to 0x10
        mmu.WriteByte(0xC000, 0x80); // ADD A,B
        mmu.WriteByte(0xC001, 0x27); // DAA
        
        cpu.Step(); // ADD A,B
        cpu.Step(); // DAA

        Assert.Equal(0x16, cpu.Regs.A); // BCD adjustment
    }

    [Fact]
    public void CPL_ComplementsA()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0x35;

        // Test CPL (0x2F)
        mmu.WriteByte(0xC000, 0x2F);
        var cycles = cpu.Step();

        Assert.Equal(0xCA, cpu.Regs.A); // ~0x35
        Assert.Equal(4, cycles);
    }

    [Fact]
    public void SCF_SetsCarryFlag()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.F = 0x00; // Clear all flags

        // Test SCF (0x37)
        mmu.WriteByte(0xC000, 0x37);
        var cycles = cpu.Step();

        Assert.Equal(4, cycles);
        Assert.True(cpu.GetCarryFlag());
    }

    [Fact]
    public void CCF_ComplementsCarryFlag()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.F = 0x10; // Set carry flag

        // Test CCF (0x3F)
        mmu.WriteByte(0xC000, 0x3F);
        var cycles = cpu.Step();

        Assert.Equal(4, cycles);
        Assert.False(cpu.GetCarryFlag());
    }

    #endregion

    #region CB Instruction Tests

    [Fact]
    public void CB_RLC_B_RotatesLeftCircular()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.B = 0x85; // 10000101

        // Test RLC B (0xCB 0x00)
        mmu.WriteByte(0xC000, 0xCB);
        mmu.WriteByte(0xC001, 0x00);
        var cycles = cpu.Step();

        Assert.Equal(0x0B, cpu.Regs.B); // 00001011 (rotated left, bit 7 -> bit 0)
        Assert.Equal(8, cycles);
        Assert.True(cpu.GetCarryFlag()); // Old bit 7 goes to carry
    }

    [Fact]
    public void CB_BIT_7_A_TestsBitCorrectly()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0x80; // Bit 7 set

        // Test BIT 7,A (0xCB 0x7F)
        mmu.WriteByte(0xC000, 0xCB);
        mmu.WriteByte(0xC001, 0x7F);
        var cycles = cpu.Step();

        Assert.Equal(0x80, cpu.Regs.A); // A unchanged
        Assert.Equal(8, cycles);
        Assert.False(cpu.GetZeroFlag()); // Bit is set, so Z=0
    }

    [Fact]
    public void CB_RES_3_B_ClearsBit()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.B = 0xFF; // All bits set

        // Test RES 3,B (0xCB 0x98)
        mmu.WriteByte(0xC000, 0xCB);
        mmu.WriteByte(0xC001, 0x98);
        var cycles = cpu.Step();

        Assert.Equal(0xF7, cpu.Regs.B); // Bit 3 cleared (0xFF & ~0x08)
        Assert.Equal(8, cycles);
    }

    [Fact]
    public void CB_SET_5_C_SetsBit()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.C = 0x00; // All bits clear

        // Test SET 5,C (0xCB 0xE9)
        mmu.WriteByte(0xC000, 0xCB);
        mmu.WriteByte(0xC001, 0xE9);
        var cycles = cpu.Step();

        Assert.Equal(0x20, cpu.Regs.C); // Bit 5 set (0x00 | 0x20)
        Assert.Equal(8, cycles);
    }

    [Fact]
    public void CB_SWAP_A_SwapsNibbles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0xF0;

        // Test SWAP A (0xCB 0x37)
        mmu.WriteByte(0xC000, 0xCB);
        mmu.WriteByte(0xC001, 0x37);
        var cycles = cpu.Step();

        Assert.Equal(0x0F, cpu.Regs.A); // Nibbles swapped
        Assert.Equal(8, cycles);
        Assert.False(cpu.GetZeroFlag());
    }

    [Fact]
    public void CB_SRL_D_ShiftsRightLogical()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.D = 0x81; // 10000001

        // Test SRL D (0xCB 0x3A)
        mmu.WriteByte(0xC000, 0xCB);
        mmu.WriteByte(0xC001, 0x3A);
        var cycles = cpu.Step();

        Assert.Equal(0x40, cpu.Regs.D); // 01000000 (shifted right, bit 0 -> carry)
        Assert.Equal(8, cycles);
        Assert.True(cpu.GetCarryFlag()); // Old bit 0 goes to carry
    }

    #endregion

    #region INC/DEC Tests

    [Fact]
    public void INC_B_IncrementsRegister()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.B = 0x4F;

        // Test INC B (0x04)
        mmu.WriteByte(0xC000, 0x04);
        var cycles = cpu.Step();

        Assert.Equal(0x50, cpu.Regs.B);
        Assert.Equal(4, cycles);
        Assert.False(cpu.GetZeroFlag());
        // Carry flag should be unchanged by INC
    }

    [Fact]
    public void DEC_C_DecrementsRegister()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.C = 0x01;

        // Test DEC C (0x0D)
        mmu.WriteByte(0xC000, 0x0D);
        var cycles = cpu.Step();

        Assert.Equal(0x00, cpu.Regs.C);
        Assert.Equal(4, cycles);
        Assert.True(cpu.GetZeroFlag());
    }

    #endregion

    #region Load Tests

    [Fact]
    public void LD_B_d8_LoadsImmediate()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;

        // Test LD B,d8 (0x06)
        mmu.WriteByte(0xC000, 0x06);
        mmu.WriteByte(0xC001, 0x42);
        var cycles = cpu.Step();

        Assert.Equal(0x42, cpu.Regs.B);
        Assert.Equal(8, cycles);
        Assert.Equal(0xC002, cpu.Regs.PC);
    }

    [Fact]
    public void LD_A_HL_LoadsFromMemory()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.HL = 0xD000;
        mmu.WriteByte(0xD000, 0x77);

        // Test LD A,(HL) (0x7E)
        mmu.WriteByte(0xC000, 0x7E);
        var cycles = cpu.Step();

        Assert.Equal(0x77, cpu.Regs.A);
        Assert.Equal(8, cycles);
    }

    #endregion
}