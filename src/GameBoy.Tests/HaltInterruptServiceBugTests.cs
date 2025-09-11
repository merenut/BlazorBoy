using GameBoy.Core;
using Xunit;

namespace GameBoy.Tests;

/// <summary>
/// Tests for the specific HALT wake-up and interrupt servicing bug identified in issue #139.
/// </summary>
public class HaltInterruptServiceBugTests
{
    [Fact]
    public void HALT_WithPendingInterrupt_ShouldServiceInterruptImmediatelyAfterWakeUp()
    {
        var emulator = new Emulator();
        var cpu = emulator.Cpu;
        var ic = emulator.InterruptController;
        
        // Set up memory with HALT instruction at current PC
        ushort haltAddress = cpu.Regs.PC;
        emulator.Mmu.WriteByte(haltAddress, 0x76); // HALT instruction
        
        // Enable interrupts but clear any existing interrupt flags
        cpu.InterruptsEnabled = true;
        ic.SetIE(0x01); // Enable VBlank interrupt
        ic.SetIF(0x00); // Clear any existing interrupt flags
        
        // Execute HALT instruction - should halt since no interrupts pending
        int haltCycles = cpu.Step();
        
        // Verify CPU is now halted
        Assert.True(cpu.IsHalted);
        Assert.True(cpu.InterruptsEnabled);
        Assert.Equal(4, haltCycles); // HALT takes 4 cycles
        Assert.Equal(0x00, ic.IF); // No interrupts pending yet
        
        // Request a VBlank interrupt while CPU is halted
        ic.Request(InterruptType.VBlank);
        
        // Verify interrupt is pending
        Assert.Equal(0x01, ic.IF); // VBlank flag set
        Assert.True(ic.HasAnyPendingInterrupts());
        
        // Step the CPU - this should wake up from HALT AND service the interrupt
        ushort pcBeforeWakeup = cpu.Regs.PC;
        int cycles = cpu.Step();
        
        // After step, CPU should be awake and interrupt should be serviced
        Assert.False(cpu.IsHalted); // CPU woke up
        Assert.False(cpu.InterruptsEnabled); // IME disabled by interrupt service
        Assert.Equal(0x00, ic.IF); // VBlank flag cleared by service
        Assert.Equal(0x0040, cpu.Regs.PC); // PC set to VBlank vector
        
        // The PC should have been pushed to stack
        ushort stackedPC = emulator.Mmu.ReadWord(cpu.Regs.SP);
        Assert.Equal(pcBeforeWakeup, stackedPC);
        
        // Should take 20 cycles for interrupt service (not just 4 for wake-up)
        Assert.Equal(20, cycles);
    }

    [Fact]
    public void HALT_WithPendingInterrupt_IMEDisabled_ShouldWakeUpButNotService()
    {
        var emulator = new Emulator();
        var cpu = emulator.Cpu;
        var ic = emulator.InterruptController;
        
        // Set up memory with HALT instruction
        emulator.Mmu.WriteByte(cpu.Regs.PC, 0x76); // HALT instruction
        
        // Set up state: interrupts disabled, CPU halted
        cpu.InterruptsEnabled = false;
        ic.SetIE(0x01); // Enable VBlank interrupt in IE
        
        // Execute HALT instruction
        cpu.Step();
        Assert.True(cpu.IsHalted);
        
        // Request interrupt while halted with IME=0
        ic.Request(InterruptType.VBlank);
        
        ushort pcBeforeStep = cpu.Regs.PC;
        int cycles = cpu.Step();
        
        // CPU should wake up but not service interrupt (IME=0)
        Assert.False(cpu.IsHalted); // CPU woke up
        Assert.False(cpu.InterruptsEnabled); // IME still disabled
        Assert.Equal(0x01, ic.IF); // VBlank flag NOT cleared
        Assert.Equal(pcBeforeStep, cpu.Regs.PC); // PC unchanged
        
        // Should take only 4 cycles for wake-up, not 20 for service
        Assert.Equal(4, cycles);
    }

    [Fact]
    public void HALT_WithoutPendingInterrupt_ShouldStayHalted()
    {
        var emulator = new Emulator();
        var cpu = emulator.Cpu;
        var ic = emulator.InterruptController;
        
        // Set up memory with HALT instruction
        emulator.Mmu.WriteByte(cpu.Regs.PC, 0x76); // HALT instruction
        
        // Set up state: interrupts enabled, CPU halted, no pending interrupts
        cpu.InterruptsEnabled = true;
        ic.SetIE(0x00); // No interrupts enabled
        
        // Execute HALT instruction
        cpu.Step();
        Assert.True(cpu.IsHalted);
        
        ushort pcBeforeStep = cpu.Regs.PC;
        int cycles = cpu.Step();
        
        // CPU should stay halted
        Assert.True(cpu.IsHalted);
        Assert.True(cpu.InterruptsEnabled);
        Assert.Equal(pcBeforeStep, cpu.Regs.PC);
        Assert.Equal(4, cycles); // 4 cycles for halted NOP
    }
    
    [Fact]
    public void MultipleInterrupts_HALTWakeUp_ShouldServiceHighestPriority()
    {
        var emulator = new Emulator();
        var cpu = emulator.Cpu;
        var ic = emulator.InterruptController;
        
        // Set up memory with HALT instruction
        emulator.Mmu.WriteByte(cpu.Regs.PC, 0x76); // HALT instruction
        
        // Set up state with multiple interrupts enabled
        cpu.InterruptsEnabled = true;
        ic.SetIE(0x1F); // Enable all interrupts
        
        // Execute HALT instruction
        cpu.Step();
        Assert.True(cpu.IsHalted);
        
        // Request multiple interrupts (Timer has higher priority than Joypad)
        ic.Request(InterruptType.Joypad); // Lower priority
        ic.Request(InterruptType.Timer);  // Higher priority
        
        ushort pcBeforeStep = cpu.Regs.PC;
        int cycles = cpu.Step();
        
        // Should service Timer interrupt (higher priority)
        Assert.False(cpu.IsHalted);
        Assert.False(cpu.InterruptsEnabled); // IME disabled
        Assert.Equal(0x0050, cpu.Regs.PC); // Timer vector
        Assert.Equal(20, cycles);
        
        // Timer flag should be cleared, Joypad flag should remain
        Assert.Equal(0x10, ic.IF); // Only Joypad flag set
    }
}