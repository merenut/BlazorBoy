using GameBoy.Core;

namespace GameBoy.Tests;

public class CpuAddressingTests
{
    #region Register Addressing Tests

    [Fact]
    public void GetR8_ReturnsCorrectRegisterValues()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Set test values in registers
        cpu.Regs.A = 0xAA;
        cpu.Regs.B = 0xBB;
        cpu.Regs.C = 0xCC;
        cpu.Regs.D = 0xDD;
        cpu.Regs.E = 0xEE;
        cpu.Regs.H = 0x11;
        cpu.Regs.L = 0x22;

        // Test each register index
        Assert.Equal(0xBB, cpu.GetR8(0)); // B
        Assert.Equal(0xCC, cpu.GetR8(1)); // C
        Assert.Equal(0xDD, cpu.GetR8(2)); // D
        Assert.Equal(0xEE, cpu.GetR8(3)); // E
        Assert.Equal(0x11, cpu.GetR8(4)); // H
        Assert.Equal(0x22, cpu.GetR8(5)); // L
        Assert.Equal(0xAA, cpu.GetR8(7)); // A
    }

    [Fact]
    public void GetR8_ThrowsForReservedIndex()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Index 6 is reserved for (HL) addressing
        Assert.Throws<InvalidOperationException>(() => cpu.GetR8(6));
    }

    [Fact]
    public void GetR8_ThrowsForInvalidIndex()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        Assert.Throws<ArgumentOutOfRangeException>(() => cpu.GetR8(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => cpu.GetR8(8));
    }

    [Fact]
    public void SetR8_SetsCorrectRegisterValues()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test setting each register
        cpu.SetR8(0, 0x10); // B
        cpu.SetR8(1, 0x20); // C
        cpu.SetR8(2, 0x30); // D
        cpu.SetR8(3, 0x40); // E
        cpu.SetR8(4, 0x50); // H
        cpu.SetR8(5, 0x60); // L
        cpu.SetR8(7, 0x70); // A

        Assert.Equal(0x10, cpu.Regs.B);
        Assert.Equal(0x20, cpu.Regs.C);
        Assert.Equal(0x30, cpu.Regs.D);
        Assert.Equal(0x40, cpu.Regs.E);
        Assert.Equal(0x50, cpu.Regs.H);
        Assert.Equal(0x60, cpu.Regs.L);
        Assert.Equal(0x70, cpu.Regs.A);
    }

    [Fact]
    public void SetR8_ThrowsForReservedIndex()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Index 6 is reserved for (HL) addressing
        Assert.Throws<InvalidOperationException>(() => cpu.SetR8(6, 0x42));
    }

    [Fact]
    public void GetR16_ReturnsCorrectRegisterPairValues()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Set test values
        cpu.Regs.BC = 0x1234;
        cpu.Regs.DE = 0x5678;
        cpu.Regs.HL = 0x9ABC;
        cpu.Regs.SP = 0xDEF0;

        // Test each register pair
        Assert.Equal(0x1234, cpu.GetR16(0)); // BC
        Assert.Equal(0x5678, cpu.GetR16(1)); // DE
        Assert.Equal(0x9ABC, cpu.GetR16(2)); // HL
        Assert.Equal(0xDEF0, cpu.GetR16(3)); // SP
    }

    [Fact]
    public void SetR16_SetsCorrectRegisterPairValues()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test setting each register pair
        cpu.SetR16(0, 0x1111); // BC
        cpu.SetR16(1, 0x2222); // DE
        cpu.SetR16(2, 0x3333); // HL
        cpu.SetR16(3, 0x4444); // SP

        Assert.Equal(0x1111, cpu.Regs.BC);
        Assert.Equal(0x2222, cpu.Regs.DE);
        Assert.Equal(0x3333, cpu.Regs.HL);
        Assert.Equal(0x4444, cpu.Regs.SP);
    }

    #endregion

    #region Memory Addressing Tests

    [Fact]
    public void ReadHL_ReadsFromHLAddress()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.HL = 0xC000;
        mmu.WriteByte(0xC000, 0x42);

        var result = cpu.ReadHL();

        Assert.Equal(0x42, result);
        Assert.Equal(0xC000, cpu.Regs.HL); // HL should not change
    }

    [Fact]
    public void WriteHL_WritesToHLAddress()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.HL = 0xC000;
        cpu.WriteHL(0x55);

        Assert.Equal(0x55, mmu.ReadByte(0xC000));
        Assert.Equal(0xC000, cpu.Regs.HL); // HL should not change
    }

    [Fact]
    public void ReadHLI_ReadsFromHLAddressThenIncrementsHL()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.HL = 0xC000;
        mmu.WriteByte(0xC000, 0x42);

        var result = cpu.ReadHLI();

        Assert.Equal(0x42, result);
        Assert.Equal(0xC001, cpu.Regs.HL); // HL should be incremented
    }

    [Fact]
    public void WriteHLI_WritesToHLAddressThenIncrementsHL()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.HL = 0xC000;
        cpu.WriteHLI(0x55);

        Assert.Equal(0x55, mmu.ReadByte(0xC000));
        Assert.Equal(0xC001, cpu.Regs.HL); // HL should be incremented
    }

    [Fact]
    public void ReadHLD_ReadsFromHLAddressThenDecrementsHL()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.HL = 0xC001;
        mmu.WriteByte(0xC001, 0x42);

        var result = cpu.ReadHLD();

        Assert.Equal(0x42, result);
        Assert.Equal(0xC000, cpu.Regs.HL); // HL should be decremented
    }

    [Fact]
    public void WriteHLD_WritesToHLAddressThenDecrementsHL()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.HL = 0xC001;
        cpu.WriteHLD(0x55);

        Assert.Equal(0x55, mmu.ReadByte(0xC001));
        Assert.Equal(0xC000, cpu.Regs.HL); // HL should be decremented
    }

    [Fact]
    public void ReadBC_ReadsFromBCAddress()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.BC = 0xC000;
        mmu.WriteByte(0xC000, 0x42);

        var result = cpu.ReadBC();

        Assert.Equal(0x42, result);
    }

    [Fact]
    public void WriteBC_WritesToBCAddress()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.BC = 0xC000;
        cpu.WriteBC(0x55);

        Assert.Equal(0x55, mmu.ReadByte(0xC000));
    }

    [Fact]
    public void ReadDE_ReadsFromDEAddress()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.DE = 0xC000;
        mmu.WriteByte(0xC000, 0x42);

        var result = cpu.ReadDE();

        Assert.Equal(0x42, result);
    }

    [Fact]
    public void WriteDE_WritesToDEAddress()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.DE = 0xC000;
        cpu.WriteDE(0x55);

        Assert.Equal(0x55, mmu.ReadByte(0xC000));
    }

    [Fact]
    public void ReadHighC_ReadsFromHighRAMPlusCRegister()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.C = 0x80;
        mmu.WriteByte(0xFF80, 0x42);

        var result = cpu.ReadHighC();

        Assert.Equal(0x42, result);
    }

    [Fact]
    public void WriteHighC_WritesToHighRAMPlusCRegister()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.C = 0x80;
        cpu.WriteHighC(0x55);

        Assert.Equal(0x55, mmu.ReadByte(0xFF80));
    }

    [Fact]
    public void ReadHighImm8_ReadsFromHighRAMPlusImmediateValue()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.PC = 0xC000;
        mmu.WriteByte(0xC000, 0x80); // Immediate value
        mmu.WriteByte(0xFF80, 0x42); // Target data

        var result = cpu.ReadHighImm8();

        Assert.Equal(0x42, result);
        Assert.Equal(0xC001, cpu.Regs.PC); // PC should advance by 1
    }

    [Fact]
    public void WriteHighImm8_WritesToHighRAMPlusImmediateValue()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.PC = 0xC000;
        mmu.WriteByte(0xC000, 0x80); // Immediate value

        cpu.WriteHighImm8(0x55);

        Assert.Equal(0x55, mmu.ReadByte(0xFF80));
        Assert.Equal(0xC001, cpu.Regs.PC); // PC should advance by 1
    }

    [Fact]
    public void ReadImm16Addr_ReadsFromImmediateAddress()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.PC = 0xC000;
        mmu.WriteByte(0xC000, 0x00); // Low byte of address
        mmu.WriteByte(0xC001, 0xD0); // High byte of address (little-endian) -> 0xD000
        mmu.WriteByte(0xD000, 0x42); // Target data

        var result = cpu.ReadImm16Addr();

        Assert.Equal(0x42, result);
        Assert.Equal(0xC002, cpu.Regs.PC); // PC should advance by 2
    }

    [Fact]
    public void WriteImm16Addr_WritesToImmediateAddress()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.PC = 0xC000;
        mmu.WriteByte(0xC000, 0x00); // Low byte of address
        mmu.WriteByte(0xC001, 0xD0); // High byte of address (little-endian) -> 0xD000

        cpu.WriteImm16Addr(0x55);

        Assert.Equal(0x55, mmu.ReadByte(0xD000));
        Assert.Equal(0xC002, cpu.Regs.PC); // PC should advance by 2
    }

    [Fact]
    public void HL_IncrementDecrement_HandlesOverflowCorrectly()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test HL increment overflow
        cpu.Regs.HL = 0xFFFF;
        cpu.ReadHLI();
        Assert.Equal(0x0000, cpu.Regs.HL);

        // Test HL decrement underflow
        cpu.Regs.HL = 0x0000;
        cpu.ReadHLD();
        Assert.Equal(0xFFFF, cpu.Regs.HL);
    }

    #endregion

    #region Integration Tests with Opcode Execution

    [Fact]
    public void AddressingHelpers_WorkWithOpcodeExecution()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test LD B,(HL) using ReadHL helper
        cpu.Regs.PC = 0xC000;
        cpu.Regs.HL = 0xD000;
        mmu.WriteByte(0xC000, 0x46); // LD B,(HL) opcode
        mmu.WriteByte(0xD000, 0x42); // Data to load

        var cycles = cpu.Step();

        Assert.Equal(0x42, cpu.Regs.B);
        Assert.Equal(8, cycles);
        Assert.Equal(0xC001, cpu.Regs.PC);
    }

    [Fact]
    public void AddressingHelpers_HL_IncrementDecrement_WorksWithOpcodes()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test LD A,(HL+) using ReadHLI helper
        cpu.Regs.PC = 0xC000;
        cpu.Regs.HL = 0xD000;
        mmu.WriteByte(0xC000, 0x2A); // LD A,(HL+) opcode
        mmu.WriteByte(0xD000, 0x55); // Data to load

        var cycles = cpu.Step();

        Assert.Equal(0x55, cpu.Regs.A);
        Assert.Equal(0xD001, cpu.Regs.HL); // HL should be incremented
        Assert.Equal(8, cycles);

        // Test LD A,(HL-) using ReadHLD helper
        cpu.Regs.PC = 0xC001;
        mmu.WriteByte(0xC001, 0x3A); // LD A,(HL-) opcode
        mmu.WriteByte(0xD001, 0x66); // Data to load

        cycles = cpu.Step();

        Assert.Equal(0x66, cpu.Regs.A);
        Assert.Equal(0xD000, cpu.Regs.HL); // HL should be decremented
        Assert.Equal(8, cycles);
    }

    [Fact]
    public void AddressingHelpers_BC_DE_WorksWithOpcodes()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test LD A,(BC) using ReadBC helper
        cpu.Regs.PC = 0xC000;
        cpu.Regs.BC = 0xD000;
        cpu.Regs.A = 0x00;
        mmu.WriteByte(0xC000, 0x0A); // LD A,(BC) opcode
        mmu.WriteByte(0xD000, 0x77); // Data to load

        var cycles = cpu.Step();

        Assert.Equal(0x77, cpu.Regs.A);
        Assert.Equal(8, cycles);

        // Test LD (DE),A using WriteDE helper
        cpu.Regs.PC = 0xC001;
        cpu.Regs.DE = 0xD001;
        cpu.Regs.A = 0x88;
        mmu.WriteByte(0xC001, 0x12); // LD (DE),A opcode

        cycles = cpu.Step();

        Assert.Equal(0x88, mmu.ReadByte(0xD001));
        Assert.Equal(8, cycles);
    }

    [Fact]
    public void AddressingHelpers_HighRAM_WorksWithOpcodes()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test LDH A,(a8) using ReadHighImm8 helper
        cpu.Regs.PC = 0xC000;
        mmu.WriteByte(0xC000, 0xF0); // LDH A,(a8) opcode
        mmu.WriteByte(0xC001, 0x80); // Offset (0xFF80)
        mmu.WriteByte(0xFF80, 0x99); // Data to load

        var cycles = cpu.Step();

        Assert.Equal(0x99, cpu.Regs.A);
        Assert.Equal(12, cycles);
        Assert.Equal(0xC002, cpu.Regs.PC);

        // Test LD A,(C) using ReadHighC helper
        cpu.Regs.PC = 0xC002;
        cpu.Regs.C = 0x81;
        mmu.WriteByte(0xC002, 0xF2); // LD A,(C) opcode
        mmu.WriteByte(0xFF81, 0xAA); // Data to load

        cycles = cpu.Step();

        Assert.Equal(0xAA, cpu.Regs.A);
        Assert.Equal(8, cycles);
    }

    #endregion
}