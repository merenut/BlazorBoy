using GameBoy.Core;

namespace GameBoy.Tests;

/// <summary>
/// Tests to validate precise cycle timing for all CPU instructions.
/// Each test verifies that instructions execute in the correct number of cycles
/// according to Pan Docs and hardware behavior.
/// </summary>
public class InstructionTimingTests
{
    #region Load Instruction Timing Tests

    [Fact]
    public void LoadRegisterToRegister_Takes4Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;

        // Test LD B,C (0x41)
        mmu.WriteByte(0xC000, 0x41);
        var cycles = cpu.Step();

        Assert.Equal(4, cycles);
    }

    [Fact]
    public void LoadImmediate8_Takes8Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;

        // Test LD A,d8 (0x3E)
        mmu.WriteByte(0xC000, 0x3E);
        mmu.WriteByte(0xC001, 0x42);
        var cycles = cpu.Step();

        Assert.Equal(8, cycles);
    }

    [Fact]
    public void LoadImmediate16_Takes12Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;

        // Test LD BC,d16 (0x01)
        mmu.WriteByte(0xC000, 0x01);
        mmu.WriteByte(0xC001, 0x34);
        mmu.WriteByte(0xC002, 0x12);
        var cycles = cpu.Step();

        Assert.Equal(12, cycles);
    }

    [Fact]
    public void LoadFromHL_Takes8Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.HL = 0xD000;

        // Test LD B,(HL) (0x46)
        mmu.WriteByte(0xC000, 0x46);
        mmu.WriteByte(0xD000, 0x42);
        var cycles = cpu.Step();

        Assert.Equal(8, cycles);
    }

    [Fact]
    public void LoadToHL_Takes8Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.HL = 0xD000;
        cpu.Regs.A = 0x42;

        // Test LD (HL),A (0x77)
        mmu.WriteByte(0xC000, 0x77);
        var cycles = cpu.Step();

        Assert.Equal(8, cycles);
    }

    [Fact]
    public void LoadImmediateToHL_Takes12Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.HL = 0xD000;

        // Test LD (HL),d8 (0x36)
        mmu.WriteByte(0xC000, 0x36);
        mmu.WriteByte(0xC001, 0x42);
        var cycles = cpu.Step();

        Assert.Equal(12, cycles);
    }

    #endregion

    #region Arithmetic Instruction Timing Tests

    [Fact]
    public void AddRegister_Takes4Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0x10;
        cpu.Regs.B = 0x20;

        // Test ADD A,B (0x80)
        mmu.WriteByte(0xC000, 0x80);
        var cycles = cpu.Step();

        Assert.Equal(4, cycles);
    }

    [Fact]
    public void AddImmediate_Takes8Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0x10;

        // Test ADD A,d8 (0xC6)
        mmu.WriteByte(0xC000, 0xC6);
        mmu.WriteByte(0xC001, 0x20);
        var cycles = cpu.Step();

        Assert.Equal(8, cycles);
    }

    [Fact]
    public void AddFromHL_Takes8Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0x10;
        cpu.Regs.HL = 0xD000;

        // Test ADD A,(HL) (0x86)
        mmu.WriteByte(0xC000, 0x86);
        mmu.WriteByte(0xD000, 0x20);
        var cycles = cpu.Step();

        Assert.Equal(8, cycles);
    }

    [Fact]
    public void IncRegister_Takes4Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;

        // Test INC A (0x3C)
        mmu.WriteByte(0xC000, 0x3C);
        var cycles = cpu.Step();

        Assert.Equal(4, cycles);
    }

    [Fact]
    public void IncHL_Takes12Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.HL = 0xD000;

        // Test INC (HL) (0x34)
        mmu.WriteByte(0xC000, 0x34);
        mmu.WriteByte(0xD000, 0x42);
        var cycles = cpu.Step();

        Assert.Equal(12, cycles);
    }

    [Fact]
    public void Inc16BitRegister_Takes8Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;

        // Test INC BC (0x03)
        mmu.WriteByte(0xC000, 0x03);
        var cycles = cpu.Step();

        Assert.Equal(8, cycles);
    }

    [Fact]
    public void Add16BitToHL_Takes8Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.HL = 0x1000;
        cpu.Regs.BC = 0x0500;

        // Test ADD HL,BC (0x09)
        mmu.WriteByte(0xC000, 0x09);
        var cycles = cpu.Step();

        Assert.Equal(8, cycles);
    }

    #endregion

    #region Jump Instruction Timing Tests

    [Fact]
    public void UnconditionalJump_Takes16Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;

        // Test JP a16 (0xC3)
        mmu.WriteByte(0xC000, 0xC3);
        mmu.WriteByte(0xC001, 0x00);
        mmu.WriteByte(0xC002, 0xD0);
        var cycles = cpu.Step();

        Assert.Equal(16, cycles);
    }

    [Fact]
    public void ConditionalJumpTaken_Takes16Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.F = 0x80; // Set Zero flag

        // Test JP Z,a16 (0xCA)
        mmu.WriteByte(0xC000, 0xCA);
        mmu.WriteByte(0xC001, 0x00);
        mmu.WriteByte(0xC002, 0xD0);
        var cycles = cpu.Step();

        Assert.Equal(16, cycles);
    }

    [Fact]
    public void ConditionalJumpNotTaken_Takes12Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.F = 0x00; // Clear Zero flag

        // Test JP Z,a16 (0xCA)
        mmu.WriteByte(0xC000, 0xCA);
        mmu.WriteByte(0xC001, 0x00);
        mmu.WriteByte(0xC002, 0xD0);
        var cycles = cpu.Step();

        Assert.Equal(12, cycles);
    }

    [Fact]
    public void RelativeJumpTaken_Takes12Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.F = 0x00; // Clear Zero flag (condition true for JR NZ)

        // Test JR NZ,r8 (0x20)
        mmu.WriteByte(0xC000, 0x20);
        mmu.WriteByte(0xC001, 0x10); // Jump forward 16 bytes
        var cycles = cpu.Step();

        Assert.Equal(12, cycles);
    }

    [Fact]
    public void RelativeJumpNotTaken_Takes8Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.F = 0x80; // Set Zero flag (condition false for JR NZ)

        // Test JR NZ,r8 (0x20)
        mmu.WriteByte(0xC000, 0x20);
        mmu.WriteByte(0xC001, 0x10);
        var cycles = cpu.Step();

        Assert.Equal(8, cycles);
    }

    [Fact]
    public void JumpHL_Takes4Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.HL = 0xD000;

        // Test JP (HL) (0xE9)
        mmu.WriteByte(0xC000, 0xE9);
        var cycles = cpu.Step();

        Assert.Equal(4, cycles);
    }

    #endregion

    #region Call and Return Timing Tests

    [Fact]
    public void UnconditionalCall_Takes24Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.SP = 0xFFFE;

        // Test CALL a16 (0xCD)
        mmu.WriteByte(0xC000, 0xCD);
        mmu.WriteByte(0xC001, 0x00);
        mmu.WriteByte(0xC002, 0xD0);
        var cycles = cpu.Step();

        Assert.Equal(24, cycles);
    }

    [Fact]
    public void ConditionalCallTaken_Takes24Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.SP = 0xFFFE;
        cpu.Regs.F = 0x80; // Set Zero flag

        // Test CALL Z,a16 (0xCC)
        mmu.WriteByte(0xC000, 0xCC);
        mmu.WriteByte(0xC001, 0x00);
        mmu.WriteByte(0xC002, 0xD0);
        var cycles = cpu.Step();

        Assert.Equal(24, cycles);
    }

    [Fact]
    public void ConditionalCallNotTaken_Takes12Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.F = 0x00; // Clear Zero flag

        // Test CALL Z,a16 (0xCC)
        mmu.WriteByte(0xC000, 0xCC);
        mmu.WriteByte(0xC001, 0x00);
        mmu.WriteByte(0xC002, 0xD0);
        var cycles = cpu.Step();

        Assert.Equal(12, cycles);
    }

    [Fact]
    public void UnconditionalReturn_Takes16Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.SP = 0xFFFC;

        // Set up return address on stack
        mmu.WriteWord(0xFFFC, 0xD000);

        // Test RET (0xC9)
        mmu.WriteByte(0xC000, 0xC9);
        var cycles = cpu.Step();

        Assert.Equal(16, cycles);
    }

    [Fact]
    public void ConditionalReturnTaken_Takes20Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.SP = 0xFFFC;
        cpu.Regs.F = 0x80; // Set Zero flag

        // Set up return address on stack
        mmu.WriteWord(0xFFFC, 0xD000);

        // Test RET Z (0xC8)
        mmu.WriteByte(0xC000, 0xC8);
        var cycles = cpu.Step();

        Assert.Equal(20, cycles);
    }

    [Fact]
    public void ConditionalReturnNotTaken_Takes8Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.F = 0x00; // Clear Zero flag

        // Test RET Z (0xC8)
        mmu.WriteByte(0xC000, 0xC8);
        var cycles = cpu.Step();

        Assert.Equal(8, cycles);
    }

    [Fact]
    public void ReturnFromInterrupt_Takes16Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.SP = 0xFFFC;

        // Set up return address on stack
        mmu.WriteWord(0xFFFC, 0xD000);

        // Test RETI (0xD9)
        mmu.WriteByte(0xC000, 0xD9);
        var cycles = cpu.Step();

        Assert.Equal(16, cycles);
    }

    #endregion

    #region Stack Operation Timing Tests

    [Fact]
    public void PushRegister_Takes16Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.SP = 0xFFFE;
        cpu.Regs.BC = 0x1234;

        // Test PUSH BC (0xC5)
        mmu.WriteByte(0xC000, 0xC5);
        var cycles = cpu.Step();

        Assert.Equal(16, cycles);
    }

    [Fact]
    public void PopRegister_Takes12Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.SP = 0xFFFC;

        // Set up data on stack
        mmu.WriteWord(0xFFFC, 0x1234);

        // Test POP BC (0xC1)
        mmu.WriteByte(0xC000, 0xC1);
        var cycles = cpu.Step();

        Assert.Equal(12, cycles);
    }

    #endregion

    #region Special Instruction Timing Tests

    [Fact]
    public void Nop_Takes4Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;

        // Test NOP (0x00)
        mmu.WriteByte(0xC000, 0x00);
        var cycles = cpu.Step();

        Assert.Equal(4, cycles);
    }

    [Fact]
    public void Halt_Takes4Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;

        // Test HALT (0x76)
        mmu.WriteByte(0xC000, 0x76);
        var cycles = cpu.Step();

        Assert.Equal(4, cycles);
    }

    [Fact]
    public void EnableInterrupts_Takes4Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;

        // Test EI (0xFB)
        mmu.WriteByte(0xC000, 0xFB);
        var cycles = cpu.Step();

        Assert.Equal(4, cycles);
    }

    [Fact]
    public void DisableInterrupts_Takes4Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;

        // Test DI (0xF3)
        mmu.WriteByte(0xC000, 0xF3);
        var cycles = cpu.Step();

        Assert.Equal(4, cycles);
    }

    [Fact]
    public void Rst_Takes16Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.SP = 0xFFFE;

        // Test RST 00H (0xC7)
        mmu.WriteByte(0xC000, 0xC7);
        var cycles = cpu.Step();

        Assert.Equal(16, cycles);
    }

    #endregion

    #region CB Instruction Timing Tests

    [Fact]
    public void CBRegisterOperation_Takes8Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0x80;

        // Test RLC A (0xCB 0x07)
        mmu.WriteByte(0xC000, 0xCB);
        mmu.WriteByte(0xC001, 0x07);
        var cycles = cpu.Step();

        Assert.Equal(8, cycles);
    }

    [Fact]
    public void CBMemoryOperation_Takes16Cycles()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.HL = 0xD000;

        // Test RLC (HL) (0xCB 0x06)
        mmu.WriteByte(0xC000, 0xCB);
        mmu.WriteByte(0xC001, 0x06);
        mmu.WriteByte(0xD000, 0x80);
        var cycles = cpu.Step();

        Assert.Equal(16, cycles);
    }

    [Fact]
    public void CBBitTest_Takes12CyclesForMemory()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.HL = 0xD000;

        // Test BIT 0,(HL) (0xCB 0x46)
        mmu.WriteByte(0xC000, 0xCB);
        mmu.WriteByte(0xC001, 0x46);
        mmu.WriteByte(0xD000, 0x01);
        var cycles = cpu.Step();

        Assert.Equal(12, cycles);
    }

    [Fact]
    public void CBBitTest_Takes8CyclesForRegister()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.Regs.PC = 0xC000;
        cpu.Regs.A = 0x01;

        // Test BIT 0,A (0xCB 0x47)
        mmu.WriteByte(0xC000, 0xCB);
        mmu.WriteByte(0xC001, 0x47);
        var cycles = cpu.Step();

        Assert.Equal(8, cycles);
    }

    #endregion
}