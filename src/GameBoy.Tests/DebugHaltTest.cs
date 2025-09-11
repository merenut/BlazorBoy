using GameBoy.Core;
using Xunit;
using Xunit.Abstractions;

namespace GameBoy.Tests;

/// <summary>
/// Debug test to understand HALT behavior
/// </summary>
public class DebugHaltTest
{
    private readonly ITestOutputHelper _output;

    public DebugHaltTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Debug_HALT_Behavior()
    {
        var emulator = new Emulator();
        var cpu = emulator.Cpu;
        var ic = emulator.InterruptController;

        // Move PC to writable RAM area
        cpu.Regs.PC = 0xC000;

        _output.WriteLine($"Initial state:");
        _output.WriteLine($"PC: 0x{cpu.Regs.PC:X4}");
        _output.WriteLine($"IME: {cpu.InterruptsEnabled}");
        _output.WriteLine($"IF: 0x{ic.IF:X2}");
        _output.WriteLine($"IE: 0x{ic.IE:X2}");
        _output.WriteLine($"IsHalted: {cpu.IsHalted}");

        // Check what's in memory before we write HALT
        byte originalInstruction = emulator.Mmu.ReadByte(cpu.Regs.PC);
        _output.WriteLine($"Original instruction at PC: 0x{originalInstruction:X2}");

        // Properly clear interrupt flags and set up for HALT test
        ic.SetIF(0x00); // This should clear _if to 0x00
        ic.SetIE(0x00); // Disable all interrupts initially
        cpu.InterruptsEnabled = true;

        _output.WriteLine($"\nAfter clearing interrupts:");
        _output.WriteLine($"IME: {cpu.InterruptsEnabled}");
        _output.WriteLine($"IF: 0x{ic.IF:X2}");
        _output.WriteLine($"IE: 0x{ic.IE:X2}");
        _output.WriteLine($"HasPendingInterrupts: {ic.HasAnyPendingInterrupts()}");

        // Set up HALT instruction
        emulator.Mmu.WriteByte(cpu.Regs.PC, 0x76);
        byte writtenInstruction = emulator.Mmu.ReadByte(cpu.Regs.PC);
        _output.WriteLine($"\nHALT instruction written at PC: 0x{cpu.Regs.PC:X4}, read back: 0x{writtenInstruction:X2}");

        if (writtenInstruction != 0x76)
        {
            _output.WriteLine("WARNING: Could not write HALT instruction to memory!");
            return;
        }

        // Execute HALT
        ushort pcBefore = cpu.Regs.PC;
        int cycles = cpu.Step();

        _output.WriteLine($"\nAfter HALT execution:");
        _output.WriteLine($"PC: 0x{cpu.Regs.PC:X4} (was 0x{pcBefore:X4})");
        _output.WriteLine($"IME: {cpu.InterruptsEnabled}");
        _output.WriteLine($"IF: 0x{ic.IF:X2}");
        _output.WriteLine($"IE: 0x{ic.IE:X2}");
        _output.WriteLine($"IsHalted: {cpu.IsHalted}");
        _output.WriteLine($"Cycles: {cycles}");

        if (cpu.IsHalted)
        {
            // Now enable VBlank interrupt and request it
            ic.SetIE(0x01); // Enable VBlank
            ic.Request(InterruptType.VBlank);

            _output.WriteLine($"\nAfter requesting VBlank interrupt:");
            _output.WriteLine($"IF: 0x{ic.IF:X2}");
            _output.WriteLine($"IE: 0x{ic.IE:X2}");
            _output.WriteLine($"HasPendingInterrupts: {ic.HasAnyPendingInterrupts()}");
            _output.WriteLine($"IsHalted: {cpu.IsHalted}");

            // Step again - should wake up and service interrupt
            ushort pcBeforeWakeup = cpu.Regs.PC;
            int wakeupCycles = cpu.Step();

            _output.WriteLine($"\nAfter wake-up step:");
            _output.WriteLine($"PC: 0x{cpu.Regs.PC:X4} (was 0x{pcBeforeWakeup:X4})");
            _output.WriteLine($"IME: {cpu.InterruptsEnabled}");
            _output.WriteLine($"IF: 0x{ic.IF:X2}");
            _output.WriteLine($"IsHalted: {cpu.IsHalted}");
            _output.WriteLine($"Cycles: {wakeupCycles}");
        }

        // This test is just for debugging, always pass
        Assert.True(true);
    }
}