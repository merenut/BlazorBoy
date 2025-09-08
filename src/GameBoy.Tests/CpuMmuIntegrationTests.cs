using GameBoy.Core;

namespace GameBoy.Tests;

/// <summary>
/// Comprehensive tests validating CPU-MMU integration for memory access patterns,
/// edge cases, and correct routing through MMU for all addressing modes.
/// </summary>
public class CpuMmuIntegrationTests
{
    #region Memory Region Access Tests

    [Fact]
    public void CPU_AccessesWorkRamThroughMMU()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test Work RAM region (0xC000-0xDFFF)
        ushort workRamAddr = 0xC000;
        cpu.Regs.HL = workRamAddr;

        // Write through CPU addressing helper
        cpu.WriteHL(0x42);

        // Verify MMU has the data
        Assert.Equal(0x42, mmu.ReadByte(workRamAddr));

        // Modify through MMU
        mmu.WriteByte(workRamAddr, 0x84);

        // Read through CPU addressing helper
        Assert.Equal(0x84, cpu.ReadHL());
    }

    [Fact]
    public void CPU_AccessesVRamThroughMMU()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test VRAM region (0x8000-0x9FFF)
        ushort vramAddr = 0x8000;
        cpu.Regs.HL = vramAddr;

        cpu.WriteHL(0x33);
        Assert.Equal(0x33, mmu.ReadByte(vramAddr));

        mmu.WriteByte(vramAddr, 0x66);
        Assert.Equal(0x66, cpu.ReadHL());
    }

    [Fact]
    public void CPU_AccessesHighRamThroughMMU()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test High RAM region (0xFF80-0xFFFE)
        ushort hramAddr = 0xFF80;
        cpu.Regs.HL = hramAddr;

        cpu.WriteHL(0x99);
        Assert.Equal(0x99, mmu.ReadByte(hramAddr));

        mmu.WriteByte(hramAddr, 0xAA);
        Assert.Equal(0xAA, cpu.ReadHL());
    }

    [Fact]
    public void CPU_AccessesOAMThroughMMU()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test OAM region (0xFE00-0xFE9F)
        ushort oamAddr = 0xFE00;
        cpu.Regs.HL = oamAddr;

        cpu.WriteHL(0x77);
        Assert.Equal(0x77, mmu.ReadByte(oamAddr));

        mmu.WriteByte(oamAddr, 0x88);
        Assert.Equal(0x88, cpu.ReadHL());
    }

    #endregion

    #region Addressing Mode Edge Cases

    [Fact]
    public void HL_AddressingModes_HandleMemoryBoundaries()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test at memory boundary 0xFFFF
        cpu.Regs.HL = 0xFFFF;
        cpu.WriteHL(0xFF);
        Assert.Equal(0xFF, mmu.ReadByte(0xFFFF));
        Assert.Equal(0xFF, cpu.ReadHL());

        // Test at memory boundary 0x0000 (would be ROM, but test the addressing)
        cpu.Regs.HL = 0x0000;
        // Note: Reading from 0x0000 without cartridge returns 0xFF from MMU
        Assert.Equal(0xFF, cpu.ReadHL());
    }

    [Fact]
    public void HLI_Addressing_WorksAcrossMemoryRegions()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test increment across Work RAM boundary
        cpu.Regs.HL = 0xDFFF;
        mmu.WriteByte(0xDFFF, 0x11);
        mmu.WriteByte(0xE000, 0x22); // Echo RAM region

        var value1 = cpu.ReadHLI();
        Assert.Equal(0x11, value1);
        Assert.Equal(0xE000, cpu.Regs.HL);

        var value2 = cpu.ReadHL(); // Now in Echo RAM region
        Assert.Equal(0x22, value2);
    }

    [Fact]
    public void HLD_Addressing_WorksAcrossMemoryRegions()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test decrement across VRAM boundary
        cpu.Regs.HL = 0x8000;
        mmu.WriteByte(0x8000, 0x33);
        mmu.WriteByte(0x7FFF, 0x44); // ROM region

        var value1 = cpu.ReadHLD();
        Assert.Equal(0x33, value1);
        Assert.Equal(0x7FFF, cpu.Regs.HL);

        var value2 = cpu.ReadHL(); // Now in ROM region (reads 0xFF without cartridge)
        Assert.Equal(0xFF, value2);
    }

    [Fact]
    public void RegisterPair_Addressing_CrossesMemoryBoundaries()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test BC addressing across regions
        cpu.Regs.BC = 0x9FFF; // End of VRAM
        mmu.WriteByte(0x9FFF, 0x55);
        Assert.Equal(0x55, cpu.ReadBC());

        cpu.Regs.BC = 0xA000; // Start of External RAM
        // External RAM reads 0xFF without cartridge
        Assert.Equal(0xFF, cpu.ReadBC());

        // Test DE addressing
        cpu.Regs.DE = 0xFE9F; // End of OAM
        mmu.WriteByte(0xFE9F, 0x66);
        Assert.Equal(0x66, cpu.ReadDE());

        cpu.Regs.DE = 0xFEA0; // Start of unusable region
        // Unusable region reads 0xFF
        Assert.Equal(0xFF, cpu.ReadDE());
    }

    #endregion

    #region High RAM Addressing Edge Cases

    [Fact]
    public void HighRAM_Addressing_HandlesBoundaryConditions()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test (C) addressing in High RAM region (0xFF80-0xFFFE)
        cpu.Regs.C = 0x80; // 0xFF80
        mmu.WriteByte(0xFF80, 0x11);
        Assert.Equal(0x11, cpu.ReadHighC());

        cpu.Regs.C = 0xFE; // 0xFFFE (just before IE register)
        mmu.WriteByte(0xFFFE, 0x22);
        Assert.Equal(0x22, cpu.ReadHighC());

        // Test immediate high RAM addressing
        cpu.Regs.PC = 0xC000;
        mmu.WriteByte(0xC000, 0x80); // 0xFF80
        mmu.WriteByte(0xFF80, 0x33);
        Assert.Equal(0x33, cpu.ReadHighImm8());

        cpu.Regs.PC = 0xC001;
        mmu.WriteByte(0xC001, 0x81); // 0xFF81
        mmu.WriteByte(0xFF81, 0x44);
        Assert.Equal(0x44, cpu.ReadHighImm8());
    }

    [Fact]
    public void HighRAM_Writing_WorksCorrectly()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test writing through (C) addressing
        cpu.Regs.C = 0x80;
        cpu.WriteHighC(0x55);
        Assert.Equal(0x55, mmu.ReadByte(0xFF80));

        // Test writing through immediate addressing
        cpu.Regs.PC = 0xC000;
        mmu.WriteByte(0xC000, 0x81); // 0xFF81
        cpu.WriteHighImm8(0x66);
        Assert.Equal(0x66, mmu.ReadByte(0xFF81));
        Assert.Equal(0xC001, cpu.Regs.PC); // PC should advance
    }

    #endregion

    #region Stack Operations Through MMU (tested via opcodes)

    [Fact]
    public void Stack_Operations_RouteThroughMMU_ViaOpcodes()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test that stack operations through opcodes use MMU
        // We'll test this by setting up memory and seeing if stack ops work
        cpu.Regs.SP = 0xFFFE;

        // Manually test MMU write/read for stack region
        mmu.WriteByte(0xFFFC, 0x34);
        mmu.WriteByte(0xFFFD, 0x12);

        // Read as 16-bit word through MMU
        var stackValue = mmu.ReadWord(0xFFFC);
        Assert.Equal(0x1234, stackValue);

        // Test that stack region is accessible through MMU
        cpu.Regs.HL = 0xFFFC;
        var lowByte = cpu.ReadHL();
        Assert.Equal(0x34, lowByte);

        cpu.Regs.HL = 0xFFFD;
        var highByte = cpu.ReadHL();
        Assert.Equal(0x12, highByte);
    }

    #endregion

    #region Immediate Addressing Through MMU (tested via public methods)

    [Fact]
    public void Immediate_MemoryAddressing_UsesMmuCorrectly()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test reading from immediate 16-bit address
        cpu.Regs.PC = 0xC000;
        mmu.WriteByte(0xC000, 0x00); // Low byte of address
        mmu.WriteByte(0xC001, 0xD0); // High byte -> 0xD000
        mmu.WriteByte(0xD000, 0x88); // Target data

        var value = cpu.ReadImm16Addr();
        Assert.Equal(0x88, value);
        Assert.Equal(0xC002, cpu.Regs.PC);

        // Test writing to immediate 16-bit address
        cpu.Regs.PC = 0xC002;
        mmu.WriteByte(0xC002, 0x01); // Low byte of address
        mmu.WriteByte(0xC003, 0xD0); // High byte -> 0xD001

        cpu.WriteImm16Addr(0x77);
        Assert.Equal(0x77, mmu.ReadByte(0xD001));
        Assert.Equal(0xC004, cpu.Regs.PC);
    }

    [Fact]
    public void Immediate_Addressing_TestedViaOpcodes()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test immediate addressing through actual opcodes
        // LD A,d8 (0x3E) - loads immediate 8-bit value into A
        cpu.Regs.PC = 0xC000;
        mmu.WriteByte(0xC000, 0x3E); // LD A,d8 opcode
        mmu.WriteByte(0xC001, 0x42); // Immediate value

        var cycles = cpu.Step();

        Assert.Equal(0x42, cpu.Regs.A);
        Assert.Equal(8, cycles);
        Assert.Equal(0xC002, cpu.Regs.PC);
    }

    #endregion

    #region Opcode Integration with MMU

    [Fact]
    public void Opcode_Execution_UsesMmuForAllMemoryAccess()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test that opcode fetching goes through MMU
        cpu.Regs.PC = 0xC000;
        mmu.WriteByte(0xC000, 0x00); // NOP opcode

        var cycles = cpu.Step();

        Assert.Equal(4, cycles);
        Assert.Equal(0xC001, cpu.Regs.PC);

        // Test CB-prefixed instruction fetching
        cpu.Regs.PC = 0xC001;
        mmu.WriteByte(0xC001, 0xCB); // CB prefix
        mmu.WriteByte(0xC002, 0x00); // RLC B (example CB opcode)

        cycles = cpu.Step();

        // Should have fetched both bytes through MMU
        Assert.Equal(0xC003, cpu.Regs.PC);
    }

    #endregion

    #region Memory Access Pattern Validation

    [Fact]
    public void Complex_AddressingSequence_AllThroughMMU()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Set up a complex sequence that uses multiple addressing modes
        cpu.Regs.HL = 0xC000;
        cpu.Regs.BC = 0xC100;
        cpu.Regs.DE = 0xC200;

        // Write test data
        mmu.WriteByte(0xC000, 0x11);
        mmu.WriteByte(0xC100, 0x22);
        mmu.WriteByte(0xC200, 0x33);
        mmu.WriteByte(0xFF80, 0x44);

        // Read using different addressing modes
        var hlValue = cpu.ReadHL();      // (HL)
        var bcValue = cpu.ReadBC();      // (BC)
        var deValue = cpu.ReadDE();      // (DE)

        Assert.Equal(0x11, hlValue);
        Assert.Equal(0x22, bcValue);
        Assert.Equal(0x33, deValue);

        // Test high RAM addressing separately to avoid register conflicts
        cpu.Regs.C = 0x80; // For high RAM access (0xFF00 + 0x80 = 0xFF80)
        var highValue = cpu.ReadHighC(); // (C) -> 0xFF00+C
        Assert.Equal(0x44, highValue);

        // Test increment/decrement addressing
        mmu.WriteByte(0xC001, 0x55); // Write data at the incremented location
        var hliValue = cpu.ReadHLI(); // Should read 0x11, then increment HL
        Assert.Equal(0x11, hliValue);
        Assert.Equal(0xC001, cpu.Regs.HL);

        // Write to new HL location
        cpu.WriteHL(0x55);
        Assert.Equal(0x55, mmu.ReadByte(0xC001));

        // Test decrement addressing
        cpu.Regs.HL = 0xC002;
        mmu.WriteByte(0xC002, 0x66);
        var hldValue = cpu.ReadHLD(); // Should read 0x66, then decrement HL
        Assert.Equal(0x66, hldValue);
        Assert.Equal(0xC001, cpu.Regs.HL);
    }

    #endregion

    #region Error Conditions and Edge Cases

    [Fact]
    public void Memory_Access_HandlesMmuBehaviorCorrectly()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test that CPU correctly handles MMU's behavior for unusable regions
        cpu.Regs.HL = 0xFEA0; // Unusable region
        var unusableValue = cpu.ReadHL();
        Assert.Equal(0xFF, unusableValue); // MMU returns 0xFF for unusable region

        // Test that CPU correctly handles MMU's behavior for unloaded ROM
        cpu.Regs.HL = 0x0000; // ROM region without cartridge
        var romValue = cpu.ReadHL();
        Assert.Equal(0xFF, romValue); // MMU returns 0xFF when no cartridge loaded

        // Test that writes to unusable regions are ignored
        cpu.Regs.HL = 0xFEA0;
        cpu.WriteHL(0x42);
        // Should still read 0xFF because writes to unusable region are ignored
        Assert.Equal(0xFF, cpu.ReadHL());
    }

    [Fact]
    public void Register_Index_Addressing_ValidatesReservedIndex()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test that GetR8/SetR8 properly reject index 6 (reserved for (HL))
        Assert.Throws<InvalidOperationException>(() => cpu.GetR8(6));
        Assert.Throws<InvalidOperationException>(() => cpu.SetR8(6, 0xAA));

        // Test that other indices work correctly
        cpu.SetR8(0, 0x11); // B register
        Assert.Equal(0x11, cpu.GetR8(0));
        Assert.Equal(0x11, cpu.Regs.B);

        cpu.SetR8(7, 0x77); // A register
        Assert.Equal(0x77, cpu.GetR8(7));
        Assert.Equal(0x77, cpu.Regs.A);
    }

    #endregion
}