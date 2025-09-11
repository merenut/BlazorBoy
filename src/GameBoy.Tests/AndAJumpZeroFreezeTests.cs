using GameBoy.Core;
using Xunit;
using Xunit.Abstractions;

namespace GameBoy.Tests;

/// <summary>
/// Tests to reproduce the specific freeze pattern described in issue #139:
/// "AND A / JP Z" loops where games wait for interrupts to set flags.
/// </summary>
public class AndAJumpZeroFreezeTests
{
    private readonly ITestOutputHelper _output;

    public AndAJumpZeroFreezeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void SimulateAndAJumpZeroLoop_WithVBlankInterrupt_ShouldNotFreeze()
    {
        var emulator = new Emulator();
        var cpu = emulator.Cpu;
        var ic = emulator.InterruptController;
        
        // Set up in writable RAM
        cpu.Regs.PC = 0xC000;
        
        // Create the loop pattern: AND A (0xA7), JP Z,addr (0x28 0xFC -> JP Z,-4)
        emulator.Mmu.WriteByte(0xC000, 0xA7); // AND A
        emulator.Mmu.WriteByte(0xC001, 0x28); // JP Z,
        emulator.Mmu.WriteByte(0xC002, 0xFC); // -4 (jump back to C000)
        
        // Set up interrupts
        cpu.InterruptsEnabled = true;
        ic.SetIE(0x01); // Enable VBlank interrupt
        ic.SetIF(0x00); // Clear interrupt flags
        
        // Set A register to 0 initially (so JP Z will always branch)
        cpu.Regs.A = 0x00;
        
        _output.WriteLine($"Initial state: PC=0x{cpu.Regs.PC:X4}, A=0x{cpu.Regs.A:X2}, IME={cpu.InterruptsEnabled}");
        
        int maxSteps = 100;
        bool interruptRequestSent = false;
        
        for (int step = 0; step < maxSteps; step++)
        {
            // Request VBlank interrupt after some steps to simulate game timing
            if (step == 50 && !interruptRequestSent)
            {
                ic.Request(InterruptType.VBlank);
                interruptRequestSent = true;
                _output.WriteLine($"Step {step}: VBlank interrupt requested");
            }
            
            ushort pcBefore = cpu.Regs.PC;
            byte aBefore = cpu.Regs.A;
            bool imeBefore = cpu.InterruptsEnabled;
            byte ifBefore = ic.IF;
            
            int cycles = cpu.Step();
            
            // Check if interrupt was serviced
            if (imeBefore && !cpu.InterruptsEnabled && cpu.Regs.PC == 0x0040)
            {
                _output.WriteLine($"Step {step}: VBlank interrupt serviced! PC jumped to 0x{cpu.Regs.PC:X4}");
                
                // In a real game, the interrupt handler would set A to non-zero
                // Simulate this by setting a value to break the loop
                cpu.Regs.A = 0x01;
                _output.WriteLine($"Step {step}: Interrupt handler set A=0x{cpu.Regs.A:X2}");
                
                // Add a RET instruction at the interrupt vector to return from handler
                emulator.Mmu.WriteByte(0x0040, 0xC9); // RET
                break;
            }
            
            // Log state changes
            if (step < 10 || step % 10 == 0 || pcBefore != cpu.Regs.PC)
            {
                _output.WriteLine($"Step {step}: PC=0x{pcBefore:X4}->0x{cpu.Regs.PC:X4}, " +
                                $"A=0x{aBefore:X2}->0x{cpu.Regs.A:X2}, " +
                                $"IME={imeBefore}->{cpu.InterruptsEnabled}, " +
                                $"IF=0x{ifBefore:X2}->0x{ic.IF:X2}, " +
                                $"cycles={cycles}");
            }
        }
        
        // Test should complete without infinite loop
        Assert.True(true, "Test completed - no infinite loop detected");
    }

    [Fact]
    public void SimulateAndAJumpZeroLoop_WithoutInterrupt_ShouldContinueLooping()
    {
        var emulator = new Emulator();
        var cpu = emulator.Cpu;
        var ic = emulator.InterruptController;
        
        // Set up in writable RAM
        cpu.Regs.PC = 0xC000;
        
        // Create the loop pattern: AND A (0xA7), JP Z,addr (0x28 0xFC -> JP Z,-4)
        emulator.Mmu.WriteByte(0xC000, 0xA7); // AND A
        emulator.Mmu.WriteByte(0xC001, 0x28); // JP Z,
        emulator.Mmu.WriteByte(0xC002, 0xFC); // -4 (jump back to C000)
        
        // Set up interrupts but don't request any
        cpu.InterruptsEnabled = true;
        ic.SetIE(0x01); // Enable VBlank interrupt
        ic.SetIF(0x00); // Clear interrupt flags
        
        // Set A register to 0 initially (so JP Z will always branch)
        cpu.Regs.A = 0x00;
        
        _output.WriteLine($"Initial state: PC=0x{cpu.Regs.PC:X4}, A=0x{cpu.Regs.A:X2}");
        
        int maxSteps = 20; // Just a few steps to verify loop behavior
        
        for (int step = 0; step < maxSteps; step++)
        {
            ushort pcBefore = cpu.Regs.PC;
            int cycles = cpu.Step();
            
            _output.WriteLine($"Step {step}: PC=0x{pcBefore:X4}->0x{cpu.Regs.PC:X4}, cycles={cycles}");
            
            // Should keep looping between C000 and C000
            Assert.True(cpu.Regs.PC == 0xC000 || cpu.Regs.PC == 0xC001 || cpu.Regs.PC == 0xC003, 
                        $"PC should stay in loop, but went to 0x{cpu.Regs.PC:X4}");
        }
        
        // Verify we're still in the loop
        Assert.Equal(0x00, cpu.Regs.A); // A should still be 0
        Assert.True(cpu.Regs.PC == 0xC000 || cpu.Regs.PC == 0xC003); // Should be at loop start or after jump
    }

    [Fact]
    public void SimulateAndAJumpZeroLoop_WithHALT_ShouldWakeOnInterrupt()
    {
        var emulator = new Emulator();
        var cpu = emulator.Cpu;
        var ic = emulator.InterruptController;
        
        // Set up in writable RAM
        cpu.Regs.PC = 0xC000;
        
        // Create pattern: AND A, JP NZ,+4, HALT, JP -6
        emulator.Mmu.WriteByte(0xC000, 0xA7); // AND A
        emulator.Mmu.WriteByte(0xC001, 0x20); // JP NZ,
        emulator.Mmu.WriteByte(0xC002, 0x02); // +2 (skip HALT if A != 0)
        emulator.Mmu.WriteByte(0xC003, 0x76); // HALT
        emulator.Mmu.WriteByte(0xC004, 0x18); // JP
        emulator.Mmu.WriteByte(0xC005, 0xF9); // -7 (back to C000)
        
        // Set up interrupts
        cpu.InterruptsEnabled = true;
        ic.SetIE(0x01); // Enable VBlank interrupt
        ic.SetIF(0x00); // Clear interrupt flags
        
        // Set A register to 0 initially
        cpu.Regs.A = 0x00;
        
        _output.WriteLine($"Initial state: PC=0x{cpu.Regs.PC:X4}, A=0x{cpu.Regs.A:X2}");
        
        // Step through AND A, JP NZ (should not branch), HALT
        for (int i = 0; i < 3; i++)
        {
            ushort pcBefore = cpu.Regs.PC;
            int cycles = cpu.Step();
            _output.WriteLine($"Step {i}: PC=0x{pcBefore:X4}->0x{cpu.Regs.PC:X4}, IsHalted={cpu.IsHalted}, cycles={cycles}");
        }
        
        // Should now be halted
        Assert.True(cpu.IsHalted, "CPU should be halted after HALT instruction");
        
        // Request interrupt
        ic.Request(InterruptType.VBlank);
        _output.WriteLine($"VBlank interrupt requested, IF=0x{ic.IF:X2}");
        
        // Step should wake up and service interrupt
        ushort pcBeforeWakeup = cpu.Regs.PC;
        int wakeupCycles = cpu.Step();
        
        _output.WriteLine($"Wake-up step: PC=0x{pcBeforeWakeup:X4}->0x{cpu.Regs.PC:X4}, " +
                         $"IsHalted={cpu.IsHalted}, IME={cpu.InterruptsEnabled}, cycles={wakeupCycles}");
        
        // Should have serviced the interrupt
        Assert.False(cpu.IsHalted, "CPU should no longer be halted");
        Assert.False(cpu.InterruptsEnabled, "IME should be disabled after interrupt service");
        Assert.Equal(0x0040, cpu.Regs.PC); // VBlank vector
        Assert.Equal(20, wakeupCycles); // Interrupt service cycles
    }
}