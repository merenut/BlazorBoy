using GameBoy.Core;

namespace GameBoy.Tests;

public class EmulatorTests
{
    [Fact]
    public void Constructor_InitializesPostBiosState()
    {
        var emulator = new Emulator();
        
        // Verify CPU registers are initialized to post-BIOS values
        Assert.Equal(0xFFFE, emulator.Cpu.Regs.SP);
        Assert.Equal(0x0100, emulator.Cpu.Regs.PC);
        Assert.Equal(0x01B0, emulator.Cpu.Regs.AF);
        Assert.Equal(0x0013, emulator.Cpu.Regs.BC);
        Assert.Equal(0x00D8, emulator.Cpu.Regs.DE);
        Assert.Equal(0x014D, emulator.Cpu.Regs.HL);
        Assert.True(emulator.Cpu.InterruptsEnabled);
        
        // Verify MMU I/O registers are initialized to post-BIOS values
        Assert.Equal(0xCF, emulator.Mmu.ReadByte(0xFF00)); // JOYP
        Assert.Equal(0x91, emulator.Mmu.ReadByte(0xFF40)); // LCDC
        Assert.Equal(0xFC, emulator.Mmu.ReadByte(0xFF47)); // BGP
        Assert.Equal(0x00, emulator.Mmu.ReadByte(0xFFFF)); // IE
    }

    [Fact]
    public void Reset_RestoresPostBiosState()
    {
        var emulator = new Emulator();
        
        // Modify CPU and MMU state
        emulator.Cpu.Regs.SP = 0x1234;
        emulator.Cpu.Regs.PC = 0x5678;
        emulator.Cpu.Regs.AF = 0x9ABC;
        emulator.Cpu.InterruptsEnabled = false;
        emulator.Mmu.WriteByte(0xFF00, 0x12);
        emulator.Mmu.WriteByte(0xFF40, 0x34);
        
        // Reset should restore post-BIOS state
        emulator.Reset();
        
        // Verify CPU registers are restored
        Assert.Equal(0xFFFE, emulator.Cpu.Regs.SP);
        Assert.Equal(0x0100, emulator.Cpu.Regs.PC);
        Assert.Equal(0x01B0, emulator.Cpu.Regs.AF);
        Assert.Equal(0x0013, emulator.Cpu.Regs.BC);
        Assert.Equal(0x00D8, emulator.Cpu.Regs.DE);
        Assert.Equal(0x014D, emulator.Cpu.Regs.HL);
        Assert.True(emulator.Cpu.InterruptsEnabled);
        
        // Verify MMU I/O registers are restored
        Assert.Equal(0xCF, emulator.Mmu.ReadByte(0xFF00)); // JOYP
        Assert.Equal(0x91, emulator.Mmu.ReadByte(0xFF40)); // LCDC
    }
}