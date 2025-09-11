using GameBoy.Core;
using Xunit;
using Xunit.Abstractions;

namespace GameBoy.Tests;

/// <summary>
/// Investigate what happens at address 0x0038 - appears to be where CPU gets stuck
/// </summary>
public class Address0x0038InvestigationTest
{
    private readonly ITestOutputHelper _output;

    public Address0x0038InvestigationTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Investigate_CPU_Behavior_At_0x0038()
    {
        var emulator = new Emulator();
        var cpu = emulator.Cpu;
        var ic = emulator.InterruptController;
        
        _output.WriteLine("=== INVESTIGATING ADDRESS 0x0038 ===");
        
        // Check what's at various memory addresses around 0x0038
        for (ushort addr = 0x0030; addr <= 0x0050; addr++)
        {
            byte value = emulator.Mmu.ReadByte(addr);
            _output.WriteLine($"Address 0x{addr:X4}: 0x{value:X2}");
        }
        
        // Check interrupt vectors specifically
        _output.WriteLine("\n=== INTERRUPT VECTORS ===");
        _output.WriteLine($"0x0040 (VBlank): 0x{emulator.Mmu.ReadByte(0x0040):X2}");
        _output.WriteLine($"0x0048 (LCD STAT): 0x{emulator.Mmu.ReadByte(0x0048):X2}");
        _output.WriteLine($"0x0050 (Timer): 0x{emulator.Mmu.ReadByte(0x0050):X2}");
        _output.WriteLine($"0x0058 (Serial): 0x{emulator.Mmu.ReadByte(0x0058):X2}");
        _output.WriteLine($"0x0060 (Joypad): 0x{emulator.Mmu.ReadByte(0x0060):X2}");
        
        // Now set PC to 0x0038 and see what happens
        cpu.Regs.PC = 0x0038;
        _output.WriteLine($"\n=== EXECUTING FROM 0x0038 ===");
        
        for (int step = 0; step < 10; step++)
        {
            ushort pcBefore = cpu.Regs.PC;
            byte opcode = emulator.Mmu.ReadByte(cpu.Regs.PC);
            
            int cycles = cpu.Step();
            
            _output.WriteLine($"Step {step}: PC=0x{pcBefore:X4}->0x{cpu.Regs.PC:X4}, " +
                             $"opcode=0x{opcode:X2}, cycles={cycles}");
            
            // If PC stays the same, we found the infinite loop
            if (cpu.Regs.PC == pcBefore)
            {
                _output.WriteLine($"INFINITE LOOP DETECTED at PC=0x{cpu.Regs.PC:X4}!");
                break;
            }
        }
        
        Assert.True(true); // This is just an investigation test
    }

    [Fact]
    public void Investigate_Jump_To_0x0038_Pattern()
    {
        var emulator = new Emulator();
        var cpu = emulator.Cpu;
        
        // Set up in writable RAM
        cpu.Regs.PC = 0xC000;
        
        // Create an invalid relative jump that would go to 0x0038
        // From C002, to get to 0x0038 we need: 0x0038 - 0xC003 = 0x4035
        // But relative jumps are signed 8-bit, so this should wrap
        emulator.Mmu.WriteByte(0xC000, 0xA7); // AND A
        emulator.Mmu.WriteByte(0xC001, 0x28); // JP Z,
        emulator.Mmu.WriteByte(0xC002, 0x35); // This should be problematic
        
        cpu.Regs.A = 0x00; // Make sure JP Z branches
        
        _output.WriteLine("=== TESTING JUMP CALCULATION ===");
        _output.WriteLine($"PC starts at: 0x{cpu.Regs.PC:X4}");
        
        // Step 1: AND A
        cpu.Step();
        _output.WriteLine($"After AND A: PC=0x{cpu.Regs.PC:X4}, A=0x{cpu.Regs.A:X2}, ZeroFlag={cpu.GetZeroFlag()}");
        
        // Step 2: JP Z
        ushort pcBefore = cpu.Regs.PC;
        byte jumpOffset = emulator.Mmu.ReadByte((ushort)(cpu.Regs.PC + 1));
        _output.WriteLine($"About to execute JP Z with offset 0x{jumpOffset:X2} (signed: {(sbyte)jumpOffset})");
        _output.WriteLine($"Current PC: 0x{pcBefore:X4}");
        _output.WriteLine($"After reading JP Z instruction, PC will be: 0x{(ushort)(pcBefore + 2):X4}");
        _output.WriteLine($"Jump target: 0x{(ushort)(pcBefore + 2 + (sbyte)jumpOffset):X4}");
        
        cpu.Step();
        _output.WriteLine($"After JP Z: PC=0x{cpu.Regs.PC:X4}");
        
        Assert.True(true);
    }
}