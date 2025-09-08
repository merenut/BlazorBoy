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

    [Fact]
    public void HALT_Bug_ExecutesNextInstructionTwice()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.PC = 0xC000;
        cpu.InterruptsEnabled = false; // IME disabled - triggers HALT bug
        cpu.Regs.A = 0x00;

        mmu.WriteByte(0xC000, 0x76); // HALT
        mmu.WriteByte(0xC001, 0x3C); // INC A

        // Request and enable interrupt (creates pending interrupt condition)
        mmu.InterruptController.Request(InterruptType.VBlank);
        mmu.InterruptController.SetIE(0x01);

        // Execute HALT - bug should trigger since IME=0 and IE&IF≠0
        cpu.Step();
        
        // CPU should not be halted due to bug
        Assert.False(cpu.IsHalted);
        Assert.Equal(0xC001, cpu.Regs.PC); // Should be at INC A instruction

        // Execute INC A first time
        cpu.Step();
        Assert.Equal(0x01, cpu.Regs.A); // A should be incremented to 1
        Assert.Equal(0xC001, cpu.Regs.PC); // PC should NOT advance due to HALT bug

        // Execute INC A second time (HALT bug causes instruction to run twice)
        cpu.Step();
        Assert.Equal(0x02, cpu.Regs.A); // A should be incremented again to 2
        Assert.Equal(0xC002, cpu.Regs.PC); // PC should advance normally this time
    }

    [Fact]
    public void HALT_Bug_DoesNotTriggerWhenIMEEnabled()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.PC = 0xC000;
        cpu.InterruptsEnabled = true; // IME enabled - no HALT bug

        mmu.WriteByte(0xC000, 0x76); // HALT

        // Request and enable interrupt
        mmu.InterruptController.Request(InterruptType.VBlank);
        mmu.InterruptController.SetIE(0x01);

        // Execute HALT - should service interrupt normally, no bug
        int cycles = cpu.Step();
        
        Assert.False(cpu.IsHalted); // Woke up to service interrupt
        Assert.Equal(20, cycles); // Interrupt handling cycles
        Assert.Equal(0x0040, cpu.Regs.PC); // Should jump to VBlank vector
        Assert.False(cpu.InterruptsEnabled); // IME cleared during interrupt
    }

    [Fact]
    public void HALT_Bug_DoesNotTriggerWhenNoInterruptPending()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.PC = 0xC000;
        cpu.InterruptsEnabled = false; // IME disabled

        mmu.WriteByte(0xC000, 0x76); // HALT

        // No interrupt requested or enabled - no pending interrupt
        mmu.InterruptController.SetIF(0x00);
        mmu.InterruptController.SetIE(0x00);

        // Execute HALT - should halt normally, no bug
        cpu.Step();
        
        Assert.True(cpu.IsHalted); // Should be properly halted
        Assert.Equal(0xC001, cpu.Regs.PC); // PC should advance past HALT
    }

    [Fact]
    public void InterruptService_PreservesFlags()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Set specific flag values
        cpu.Regs.PC = 0x1234;
        cpu.Regs.SP = 0xFFFE;
        cpu.Regs.F = 0xF0; // All flags set (Z, N, H, C)
        cpu.InterruptsEnabled = true;

        // Request interrupt
        mmu.InterruptController.Request(InterruptType.Timer);
        mmu.InterruptController.SetIE(0x04);

        // Service interrupt
        cpu.Step();

        // Flags should be preserved during interrupt service
        Assert.Equal(0xF0, cpu.Regs.F);
        Assert.Equal(0x0050, cpu.Regs.PC); // Timer vector
        Assert.False(cpu.InterruptsEnabled); // IME cleared
    }

    [Fact]
    public void InterruptService_StackEdgeCase_NearBoundary()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Set stack pointer near memory boundary
        cpu.Regs.PC = 0x1234;
        cpu.Regs.SP = 0x0002; // Very low stack pointer
        cpu.InterruptsEnabled = true;

        // Request interrupt
        mmu.InterruptController.Request(InterruptType.VBlank);
        mmu.InterruptController.SetIE(0x01);

        // Service interrupt
        cpu.Step();

        // Check that PC was pushed correctly even with low SP
        Assert.Equal(0x0000, cpu.Regs.SP); // SP decremented by 2
        ushort pushedPC = mmu.ReadWord(0x0000);
        Assert.Equal(0x1234, pushedPC);
        Assert.Equal(0x0040, cpu.Regs.PC); // VBlank vector
    }

    [Fact]
    public void MultiplePendingInterrupts_ServicesHighestPriorityFirst()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.PC = 0x1000;
        cpu.Regs.SP = 0xFFFE;
        cpu.InterruptsEnabled = true;

        // Request multiple interrupts
        mmu.InterruptController.Request(InterruptType.Joypad);  // Lowest priority
        mmu.InterruptController.Request(InterruptType.Timer);   // Mid priority  
        mmu.InterruptController.Request(InterruptType.LCDStat); // Higher priority
        mmu.InterruptController.SetIE(0x1F); // Enable all

        // First step should service LCDStat (highest priority)
        cpu.Step();
        Assert.Equal(0x0048, cpu.Regs.PC); // LCDStat vector

        // Reset for next interrupt
        cpu.Regs.PC = 0x1000;
        cpu.InterruptsEnabled = true;

        // Next step should service Timer (next highest remaining)
        cpu.Step();
        Assert.Equal(0x0050, cpu.Regs.PC); // Timer vector

        // Reset for next interrupt
        cpu.Regs.PC = 0x1000;
        cpu.InterruptsEnabled = true;

        // Final step should service Joypad (lowest remaining)
        cpu.Step();
        Assert.Equal(0x0060, cpu.Regs.PC); // Joypad vector
    }

    [Fact]
    public void EI_DelayWithDifferentInstructions()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        // Test EI delay with various instruction types after EI
        var testCases = new[]
        {
            (opcode: 0x00, mnemonic: "NOP", cycles: 4),
            (opcode: 0x3C, mnemonic: "INC A", cycles: 4),
            (opcode: 0x06, mnemonic: "LD B,n", cycles: 8), // 2-byte instruction
        };

        foreach (var (opcode, mnemonic, cycles) in testCases)
        {
            // Reset state
            cpu.Reset();
            cpu.Regs.PC = 0xC000;
            cpu.InterruptsEnabled = false;

            // Setup: EI followed by test instruction
            mmu.WriteByte(0xC000, 0xFB); // EI
            mmu.WriteByte(0xC001, (byte)opcode);
            if (opcode == 0x06) mmu.WriteByte(0xC002, (byte)0xFF); // Immediate value for LD B,n

            // Request interrupt
            mmu.InterruptController.Request(InterruptType.VBlank);
            mmu.InterruptController.SetIE(0x01);

            // Execute EI
            cpu.Step();
            Assert.False(cpu.InterruptsEnabled); // Still disabled after EI

            // Execute following instruction - should enable interrupts but not service yet
            int instructionCycles = cpu.Step();
            Assert.Equal(cycles, instructionCycles); // Should execute the instruction normally
            Assert.True(cpu.InterruptsEnabled); // Now enabled

            // Next step should service the interrupt
            int interruptCycles = cpu.Step();
            Assert.Equal(20, interruptCycles); // Interrupt handling cycles
            Assert.Equal(0x0040, cpu.Regs.PC); // VBlank vector
        }
    }

    [Fact]
    public void IME_DelayComplexScenario_EI_DI_Sequence()
    {
        var mmu = new Mmu();
        var cpu = new Cpu(mmu);

        cpu.Regs.PC = 0xC000;
        cpu.InterruptsEnabled = false;

        // Setup: EI, NOP, DI sequence
        mmu.WriteByte(0xC000, 0xFB); // EI
        mmu.WriteByte(0xC001, 0x00); // NOP
        mmu.WriteByte(0xC002, 0xF3); // DI

        // Request interrupt
        mmu.InterruptController.Request(InterruptType.VBlank);
        mmu.InterruptController.SetIE(0x01);

        // Execute EI - delay starts
        cpu.Step();
        Assert.False(cpu.InterruptsEnabled);

        // Execute NOP - should enable interrupts
        cpu.Step();
        Assert.True(cpu.InterruptsEnabled);

        // Execute DI - should immediately disable interrupts before interrupt service
        cpu.Step();
        Assert.False(cpu.InterruptsEnabled);
        Assert.Equal(0xC003, cpu.Regs.PC); // Should continue to next instruction, not service interrupt
    }
}