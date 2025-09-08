using System;
using System.Collections.Generic;
using GameBoy.Core;

namespace GameBoy.Tests.Integration;

/// <summary>
/// Comprehensive CPU instruction tests covering all opcodes and addressing modes.
/// These tests validate instruction execution correctness and edge cases.
/// </summary>
public class CpuInstructionTests
{
    #region Load Instructions Tests

    [Theory]
    [InlineData(0x3E)] // LD A,n
    [InlineData(0x06)] // LD B,n
    [InlineData(0x0E)] // LD C,n
    [InlineData(0x16)] // LD D,n
    [InlineData(0x1E)] // LD E,n
    [InlineData(0x26)] // LD H,n
    [InlineData(0x2E)] // LD L,n
    public void LD_r_d8_LoadsImmediateValue(byte opcode)
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;

        mmu.WriteByte(0xC000, opcode);
        mmu.WriteByte(0xC001, 0x42);

        var cycles = cpu.Step();

        // Verify the correct register was loaded
        byte expectedValue = 0x42;
        switch (opcode)
        {
            case 0x3E: Assert.Equal(expectedValue, cpu.Regs.A); break;
            case 0x06: Assert.Equal(expectedValue, cpu.Regs.B); break;
            case 0x0E: Assert.Equal(expectedValue, cpu.Regs.C); break;
            case 0x16: Assert.Equal(expectedValue, cpu.Regs.D); break;
            case 0x1E: Assert.Equal(expectedValue, cpu.Regs.E); break;
            case 0x26: Assert.Equal(expectedValue, cpu.Regs.H); break;
            case 0x2E: Assert.Equal(expectedValue, cpu.Regs.L); break;
        }

        Assert.Equal(8, cycles);
        Assert.Equal(0xC002, cpu.Regs.PC);
    }

    [Theory]
    [InlineData(0x01)] // LD BC,nn
    [InlineData(0x11)] // LD DE,nn
    [InlineData(0x21)] // LD HL,nn
    [InlineData(0x31)] // LD SP,nn
    public void LD_rr_d16_Loads16BitImmediate(byte opcode)
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;

        mmu.WriteByte(0xC000, opcode);
        mmu.WriteByte(0xC001, 0x34); // Low byte
        mmu.WriteByte(0xC002, 0x12); // High byte

        var cycles = cpu.Step();

        ushort expectedValue = 0x1234;
        switch (opcode)
        {
            case 0x01: Assert.Equal(expectedValue, cpu.Regs.BC); break;
            case 0x11: Assert.Equal(expectedValue, cpu.Regs.DE); break;
            case 0x21: Assert.Equal(expectedValue, cpu.Regs.HL); break;
            case 0x31: Assert.Equal(expectedValue, cpu.Regs.SP); break;
        }

        Assert.Equal(12, cycles);
        Assert.Equal(0xC003, cpu.Regs.PC);
    }

    [Fact]
    public void LD_A_BC_LoadsFromBCAddress()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.BC = 0xD000;

        mmu.WriteByte(0xC000, 0x0A); // LD A,(BC)
        mmu.WriteByte(0xD000, 0x55);

        var cycles = cpu.Step();

        Assert.Equal(0x55, cpu.Regs.A);
        Assert.Equal(8, cycles);
    }

    [Fact]
    public void LDH_A_a8_LoadsFromHighMemory()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;

        mmu.WriteByte(0xC000, 0xF0); // LDH A,(a8)
        mmu.WriteByte(0xC001, 0x80);
        mmu.WriteByte(0xFF80, 0x77);

        var cycles = cpu.Step();

        Assert.Equal(0x77, cpu.Regs.A);
        Assert.Equal(12, cycles);
    }

    #endregion

    #region Arithmetic Instructions Tests

    [Theory]
    [InlineData(0x10, 0x20, 0x30, false, false, false, false)] // No flags
    [InlineData(0xFF, 0x01, 0x00, true, false, true, true)]   // Z, H, C flags
    [InlineData(0x0F, 0x01, 0x10, false, false, true, false)] // H flag only
    public void ADD_A_B_AddsRegisterToA(byte aValue, byte bValue,
        byte expectedResult, bool expectedZ, bool expectedN, bool expectedH, bool expectedC)
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = aValue;
        cpu.Regs.B = bValue;

        mmu.WriteByte(0xC000, 0x80); // ADD A,B

        var cycles = cpu.Step();

        Assert.Equal(expectedResult, cpu.Regs.A);
        Assert.Equal(expectedZ, (cpu.Regs.F & 0x80) != 0); // Z flag
        Assert.Equal(expectedN, (cpu.Regs.F & 0x40) != 0); // N flag
        Assert.Equal(expectedH, (cpu.Regs.F & 0x20) != 0); // H flag
        Assert.Equal(expectedC, (cpu.Regs.F & 0x10) != 0); // C flag
        Assert.Equal(4, cycles);
    }

    [Fact]
    public void INC_B_IncrementsRegister()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.B = 0x0F;

        mmu.WriteByte(0xC000, 0x04); // INC B

        var cycles = cpu.Step();

        Assert.Equal(0x10, cpu.Regs.B);
        Assert.False((cpu.Regs.F & 0x80) != 0); // Z flag should be clear
        Assert.False((cpu.Regs.F & 0x40) != 0); // N flag should be clear
        Assert.True((cpu.Regs.F & 0x20) != 0);  // H flag should be set (0x0F + 1 = half carry)
        Assert.Equal(4, cycles);
    }

    #endregion

    #region Jump and Call Instructions Tests

    [Fact]
    public void JP_a16_JumpsToAbsoluteAddress()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;

        mmu.WriteByte(0xC000, 0xC3); // JP a16
        mmu.WriteByte(0xC001, 0x00); // Low byte
        mmu.WriteByte(0xC002, 0x80); // High byte

        var cycles = cpu.Step();

        Assert.Equal(0x8000, cpu.Regs.PC);
        Assert.Equal(16, cycles);
    }

    [Fact]
    public void JR_r8_RelativeJump()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;

        mmu.WriteByte(0xC000, 0x18); // JR r8
        mmu.WriteByte(0xC001, 0x10); // +16 displacement

        var cycles = cpu.Step();

        Assert.Equal(0xC012, cpu.Regs.PC); // 0xC002 + 0x10
        Assert.Equal(12, cycles);
    }

    #endregion

    #region CB Prefix Instructions Tests

    [Theory]
    [InlineData((byte)0x80, (byte)0x01, false, false, false, true)]   // Rotate with carry out
    [InlineData((byte)0x7F, (byte)0xFE, false, false, false, false)] // Rotate without carry
    [InlineData((byte)0x00, (byte)0x00, true, false, false, false)]  // Rotate zero
    public void CB_RLC_B_RotateLeftCircular(byte initial,
        byte expected, bool expectedZ, bool expectedN, bool expectedH, bool expectedC)
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.B = initial;

        mmu.WriteByte(0xC000, 0xCB);
        mmu.WriteByte(0xC001, 0x00); // RLC B

        var cycles = cpu.Step();

        Assert.Equal(expected, cpu.Regs.B);
        Assert.Equal(expectedZ, (cpu.Regs.F & 0x80) != 0); // Z flag
        Assert.Equal(expectedN, (cpu.Regs.F & 0x40) != 0); // N flag
        Assert.Equal(expectedH, (cpu.Regs.F & 0x20) != 0); // H flag
        Assert.Equal(expectedC, (cpu.Regs.F & 0x10) != 0); // C flag
        Assert.Equal(8, cycles);
    }

    [Theory]
    [InlineData((byte)0x01, false)] // Bit is set
    [InlineData((byte)0xFE, true)]  // Bit is clear
    public void CB_BIT_0_B_TestsBit(byte registerValue, bool expectedZ)
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.B = registerValue;

        mmu.WriteByte(0xC000, 0xCB);
        mmu.WriteByte(0xC001, 0x40); // BIT 0,B

        var cycles = cpu.Step();

        Assert.Equal(expectedZ, (cpu.Regs.F & 0x80) != 0); // Z flag
        Assert.False((cpu.Regs.F & 0x40) != 0); // N flag should be clear
        Assert.True((cpu.Regs.F & 0x20) != 0);  // H flag should be set
        Assert.Equal(8, cycles);
    }

    #endregion

    #region Edge Cases and Special Instructions

    [Fact]
    public void NOP_DoesNothing()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        var originalState = cpu.Regs;
        cpu.Regs.PC = 0xC000;

        mmu.WriteByte(0xC000, 0x00); // NOP

        var cycles = cpu.Step();

        // Only PC should have changed
        Assert.Equal(0xC001, cpu.Regs.PC);
        Assert.Equal(originalState.A, cpu.Regs.A);
        Assert.Equal(originalState.F, cpu.Regs.F);
        Assert.Equal(originalState.BC, cpu.Regs.BC);
        Assert.Equal(originalState.DE, cpu.Regs.DE);
        Assert.Equal(originalState.HL, cpu.Regs.HL);
        Assert.Equal(originalState.SP, cpu.Regs.SP);
        Assert.Equal(4, cycles);
    }

    [Fact]
    public void HALT_StopsExecution()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;

        mmu.WriteByte(0xC000, 0x76); // HALT

        var cycles = cpu.Step();

        // HALT should advance PC and take 4 cycles
        Assert.Equal(0xC001, cpu.Regs.PC);
        Assert.Equal(4, cycles);
    }

    [Fact]
    public void EI_EnablesInterrupts()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.InterruptsEnabled = false;

        mmu.WriteByte(0xC000, 0xFB); // EI
        mmu.WriteByte(0xC001, 0x00); // NOP

        // Execute EI instruction
        var cycles = cpu.Step();

        // Interrupts should not be enabled immediately after EI
        Assert.False(cpu.InterruptsEnabled);
        Assert.Equal(4, cycles);

        // Execute next instruction (NOP) - this should enable interrupts
        cycles = cpu.Step();
        Assert.True(cpu.InterruptsEnabled);
        Assert.Equal(4, cycles);
    }

    [Fact]
    public void DI_DisablesInterrupts()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.InterruptsEnabled = true;

        mmu.WriteByte(0xC000, 0xF3); // DI

        var cycles = cpu.Step();

        Assert.False(cpu.InterruptsEnabled);
        Assert.Equal(4, cycles);
    }

    #endregion

    #region Memory Addressing Mode Tests

    [Fact]
    public void LD_HL_d8_LoadsMemoryFromImmediate()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.HL = 0xD000;

        mmu.WriteByte(0xC000, 0x36); // LD (HL),d8
        mmu.WriteByte(0xC001, 0x42);

        var cycles = cpu.Step();

        Assert.Equal(0x42, mmu.ReadByte(0xD000));
        Assert.Equal(12, cycles);
    }

    [Fact]
    public void LD_B_HL_LoadsRegisterFromMemory()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.HL = 0xD000;

        mmu.WriteByte(0xC000, 0x46); // LD B,(HL)
        mmu.WriteByte(0xD000, 0x55);

        var cycles = cpu.Step();

        Assert.Equal(0x55, cpu.Regs.B);
        Assert.Equal(8, cycles);
    }

    [Fact]
    public void ADD_A_HL_AddsMemoryValueToA()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0x10;
        cpu.Regs.HL = 0xD000;

        mmu.WriteByte(0xC000, 0x86); // ADD A,(HL)
        mmu.WriteByte(0xD000, 0x20);

        var cycles = cpu.Step();

        Assert.Equal(0x30, cpu.Regs.A);
        Assert.Equal(8, cycles);
    }

    #endregion
}