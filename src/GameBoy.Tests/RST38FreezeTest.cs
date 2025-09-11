using GameBoy.Core;
using Xunit;
using Xunit.Abstractions;

namespace GameBoy.Tests;

/// <summary>
/// Test to confirm that RST 38h creates an interrupt-proof freeze
/// </summary>
public class RST38FreezeTest
{
    private readonly ITestOutputHelper _output;

    public RST38FreezeTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void RST38_CreatesInterruptProofFreeze()
    {
        var emulator = new Emulator();
        var cpu = emulator.Cpu;
        var ic = emulator.InterruptController;

        // Set PC to 0x0038 where RST 38h instruction is
        cpu.Regs.PC = 0x0038;

        // Enable interrupts and request VBlank
        cpu.InterruptsEnabled = true;
        ic.SetIE(0x01); // Enable VBlank
        ic.SetIF(0x00); // Clear flags
        ic.Request(InterruptType.VBlank); // Request interrupt

        _output.WriteLine($"Initial state: PC=0x{cpu.Regs.PC:X4}, IME={cpu.InterruptsEnabled}");
        _output.WriteLine($"IF=0x{ic.IF:X2}, IE=0x{ic.IE:X2}");
        _output.WriteLine($"HasPendingInterrupts: {ic.HasAnyPendingInterrupts()}");

        // Step multiple times - should stay frozen at 0x0038 despite pending interrupt
        for (int step = 0; step < 10; step++)
        {
            ushort pcBefore = cpu.Regs.PC;
            byte opcode = emulator.Mmu.ReadByte(cpu.Regs.PC);
            bool imeBefore = cpu.InterruptsEnabled;

            int cycles = cpu.Step();

            _output.WriteLine($"Step {step}: PC=0x{pcBefore:X4}->0x{cpu.Regs.PC:X4}, " +
                             $"opcode=0x{opcode:X2}, IME={imeBefore}->{cpu.InterruptsEnabled}, cycles={cycles}");

            // Should stay frozen at 0x0038 - interrupts can't break RST 38h loop
            Assert.Equal(0x0038, cpu.Regs.PC);
            Assert.True(cpu.InterruptsEnabled); // IME should stay enabled
            Assert.Equal(0xFF, opcode); // Should always be executing RST 38h
            Assert.Equal(16, cycles); // RST takes 16 cycles
        }

        // Interrupt should still be pending but never serviced
        Assert.True(ic.HasAnyPendingInterrupts());
        Assert.Equal(0x01, (byte)(ic.IF & 0x1F)); // VBlank flag still set
    }

    [Fact]
    public void NormalInstruction_AllowsInterruptService()
    {
        var emulator = new Emulator();
        var cpu = emulator.Cpu;
        var ic = emulator.InterruptController;

        // Set PC to writable RAM and put a NOP instruction
        cpu.Regs.PC = 0xC000;
        emulator.Mmu.WriteByte(0xC000, 0x00); // NOP

        // Enable interrupts and request VBlank
        cpu.InterruptsEnabled = true;
        ic.SetIE(0x01); // Enable VBlank
        ic.SetIF(0x00); // Clear flags
        ic.Request(InterruptType.VBlank); // Request interrupt

        _output.WriteLine($"Initial state: PC=0x{cpu.Regs.PC:X4}, IME={cpu.InterruptsEnabled}");
        _output.WriteLine($"IF=0x{ic.IF:X2}, IE=0x{ic.IE:X2}");

        // Step once - should service the interrupt immediately
        ushort pcBefore = cpu.Regs.PC;
        int cycles = cpu.Step();

        _output.WriteLine($"After step: PC=0x{pcBefore:X4}->0x{cpu.Regs.PC:X4}, " +
                         $"IME={cpu.InterruptsEnabled}, cycles={cycles}");

        // Should have serviced the interrupt
        Assert.False(cpu.InterruptsEnabled); // IME disabled
        Assert.Equal(0x0040, cpu.Regs.PC); // VBlank vector
        Assert.Equal(20, cycles); // Interrupt service cycles
        Assert.False(ic.HasAnyPendingInterrupts()); // Interrupt cleared
    }
}