using GameBoy.Core;

namespace GameBoy.Tests;

/// <summary>
/// Tests for Interrupt Service Routine (ISR) functionality and CPU interrupt integration.
/// Validates interrupt handling, priority ordering, IME delay, and HALT behavior.
/// </summary>
public class InterruptServiceRoutineTests
{
    [Fact]
    public void ServiceInterrupt_PushesPC_JumpsToVector_ClearsIME()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Setup initial state
        cpu.Regs.PC = 0x1234;
        cpu.Regs.SP = 0xFFFE;
        cpu.InterruptsEnabled = true;

        // Request VBlank interrupt
        mmu.InterruptController.Request(InterruptType.VBlank);
        mmu.InterruptController.SetIE(0x01); // Enable VBlank interrupt

        // Step should service the interrupt
        int cycles = cpu.Step();

        // Check interrupt was serviced
        Assert.False(cpu.InterruptsEnabled); // IME should be cleared
        Assert.Equal(0x0040, cpu.Regs.PC);  // Should jump to VBlank vector
        Assert.Equal(0xFFFC, cpu.Regs.SP);  // SP should be decremented by 2

        // Check PC was pushed onto stack
        ushort pushedPC = mmu.ReadWord(0xFFFC);
        Assert.Equal(0x1234, pushedPC);

        // Check cycles consumed (20 for interrupt handling)
        Assert.Equal(20, cycles);

        // Check IF bit was cleared
        Assert.Equal(0xE0, mmu.InterruptController.IF); // Should be 0x00 | 0xE0
    }

    [Fact]
    public void InterruptPriority_VBlankHighestPriority()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.PC = 0x1000;
        cpu.InterruptsEnabled = true;

        // Request multiple interrupts
        mmu.InterruptController.Request(InterruptType.Timer);   // Lower priority
        mmu.InterruptController.Request(InterruptType.VBlank);  // Highest priority
        mmu.InterruptController.Request(InterruptType.Joypad); // Lowest priority
        mmu.InterruptController.SetIE(0x1F); // Enable all interrupts

        // Step should service VBlank (highest priority)
        cpu.Step();

        Assert.Equal(0x0040, cpu.Regs.PC); // VBlank vector
    }

    [Fact]
    public void InterruptPriority_CorrectOrder()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);
        cpu.InterruptsEnabled = true;
        mmu.InterruptController.SetIE(0x1F); // Enable all interrupts

        // Test each priority level
        var priorities = new[]
        {
            (InterruptType.VBlank, 0x0040),
            (InterruptType.LCDStat, 0x0048),
            (InterruptType.Timer, 0x0050),
            (InterruptType.Serial, 0x0058),
            (InterruptType.Joypad, 0x0060)
        };

        foreach (var (interruptType, expectedVector) in priorities)
        {
            // Clear all interrupts and request only this one
            mmu.InterruptController.SetIF(0x00);
            mmu.InterruptController.Request(interruptType);

            cpu.Regs.PC = 0x1000;
            cpu.InterruptsEnabled = true;
            cpu.Step();

            Assert.Equal(expectedVector, cpu.Regs.PC);
        }
    }

    [Fact]
    public void EI_HasOneInstructionDelay()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.PC = 0xC000;
        cpu.InterruptsEnabled = false;

        // Setup: EI followed by NOP
        mmu.WriteByte(0xC000, 0xFB); // EI
        mmu.WriteByte(0xC001, 0x00); // NOP

        // Request an interrupt
        mmu.InterruptController.Request(InterruptType.VBlank);
        mmu.InterruptController.SetIE(0x01);

        // Execute EI instruction
        cpu.Step();

        // Interrupts should still be disabled immediately after EI
        Assert.False(cpu.InterruptsEnabled);

        // Execute next instruction (NOP) - this should enable interrupts but not service yet
        cpu.Step();

        // Now interrupts should be enabled
        Assert.True(cpu.InterruptsEnabled);

        // Next step should service the interrupt
        int cycles = cpu.Step();
        Assert.Equal(20, cycles); // Interrupt handling cycles
        Assert.Equal(0x0040, cpu.Regs.PC); // VBlank vector
    }

    [Fact]
    public void DI_ImmediatelyDisablesInterrupts()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.PC = 0xC000;
        cpu.InterruptsEnabled = true;

        mmu.WriteByte(0xC000, 0xF3); // DI

        // Request an interrupt
        mmu.InterruptController.Request(InterruptType.VBlank);
        mmu.InterruptController.SetIE(0x01);

        // Execute DI instruction
        cpu.Step();

        // Interrupts should be disabled
        Assert.False(cpu.InterruptsEnabled);

        // Next step should not service interrupt
        int cycles = cpu.Step();
        Assert.NotEqual(20, cycles); // Should not be interrupt handling cycles
        Assert.NotEqual(0x0040, cpu.Regs.PC); // Should not jump to vector
    }

    [Fact]
    public void HALT_WakesUpOnInterrupt_EvenIfIMEDisabled()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.PC = 0xC000;
        cpu.InterruptsEnabled = false; // IME disabled

        mmu.WriteByte(0xC000, 0x76); // HALT
        mmu.WriteByte(0xC001, 0x00); // NOP

        // Execute HALT
        cpu.Step();
        Assert.True(cpu.IsHalted);

        // Request interrupt but don't enable in IE
        mmu.InterruptController.Request(InterruptType.VBlank);
        mmu.InterruptController.SetIE(0x00); // Interrupt not enabled

        // CPU should stay halted
        cpu.Step();
        Assert.True(cpu.IsHalted);

        // Now enable the interrupt
        mmu.InterruptController.SetIE(0x01);

        // Next step should wake up from HALT but not service interrupt (IME disabled)
        cpu.Step();
        Assert.False(cpu.IsHalted);
        Assert.Equal(0xC001, cpu.Regs.PC); // Should continue to next instruction
    }

    [Fact]
    public void HALT_ServicesInterrupt_WhenIMEEnabled()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.PC = 0xC000;
        cpu.InterruptsEnabled = true;

        mmu.WriteByte(0xC000, 0x76); // HALT

        // Execute HALT
        cpu.Step();
        Assert.True(cpu.IsHalted);

        // Request and enable interrupt
        mmu.InterruptController.Request(InterruptType.Timer);
        mmu.InterruptController.SetIE(0x04);

        // Next step should wake up and service interrupt
        int cycles = cpu.Step();
        Assert.False(cpu.IsHalted);
        Assert.Equal(20, cycles); // Interrupt handling cycles
        Assert.Equal(0x0050, cpu.Regs.PC); // Timer vector
        Assert.False(cpu.InterruptsEnabled); // IME cleared
    }

    [Fact]
    public void RETI_ReturnsFromInterrupt_EnablesIME()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Setup: simulate being in an interrupt handler
        cpu.Regs.SP = 0xFFFC;
        cpu.Regs.PC = 0xC000; // Use RAM address instead of ROM vector
        cpu.InterruptsEnabled = false; // IME disabled during interrupt

        // Put return address on stack
        mmu.WriteWord(0xFFFC, 0x1234);

        mmu.WriteByte(0xC000, 0xD9); // RETI

        // Execute RETI
        int cycles = cpu.Step();

        // Check RETI behavior
        Assert.Equal(0x1234, cpu.Regs.PC); // Should return to address from stack
        Assert.Equal(0xFFFE, cpu.Regs.SP); // SP should be restored
        Assert.True(cpu.InterruptsEnabled); // IME should be enabled
        Assert.Equal(16, cycles); // RETI takes 16 cycles
    }

    [Fact]
    public void NoInterrupt_WhenIMEDisabled()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.PC = 0xC000;
        cpu.InterruptsEnabled = false;

        mmu.WriteByte(0xC000, 0x00); // NOP

        // Request and enable interrupt
        mmu.InterruptController.Request(InterruptType.VBlank);
        mmu.InterruptController.SetIE(0x01);

        // Execute instruction - should not service interrupt
        int cycles = cpu.Step();

        Assert.Equal(4, cycles); // NOP cycles, not interrupt cycles
        Assert.Equal(0xC001, cpu.Regs.PC); // Should continue normally
        Assert.False(cpu.InterruptsEnabled); // IME should remain disabled
    }

    [Fact]
    public void NoInterrupt_WhenInterruptNotEnabled()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.PC = 0xC000;
        cpu.InterruptsEnabled = true;

        mmu.WriteByte(0xC000, 0x00); // NOP

        // Request interrupt but don't enable it
        mmu.InterruptController.Request(InterruptType.VBlank);
        mmu.InterruptController.SetIE(0x00); // No interrupts enabled

        // Execute instruction - should not service interrupt
        int cycles = cpu.Step();

        Assert.Equal(4, cycles); // NOP cycles, not interrupt cycles
        Assert.Equal(0xC001, cpu.Regs.PC); // Should continue normally
        Assert.True(cpu.InterruptsEnabled); // IME should remain enabled
    }

    // [Fact]
    // public void HALT_Bug_ExecutesNextInstructionTwice()
    // {
    //     // HALT bug implementation commented out - requires more detailed hardware behavior research
    //     // The bug occurs when HALT is executed with IME=0 and IE&IF≠0
    //     // This causes the instruction after HALT to be executed twice
    //     // Implementation complexity exceeds current scope
    // }
}