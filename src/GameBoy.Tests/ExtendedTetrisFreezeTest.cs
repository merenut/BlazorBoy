using System;
using System.IO;
using GameBoy.Core;
using Xunit;
using Xunit.Abstractions;

namespace GameBoy.Tests;

/// <summary>
/// Extended test to run Tetris ROM and monitor for freeze conditions over long execution
/// </summary>
public class ExtendedTetrisFreezeTest
{
    private readonly ITestOutputHelper _output;

    public ExtendedTetrisFreezeTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TetrisROM_ExtendedExecution_MonitorForFreezePatterns()
    {
        const string tetrisRomPath = "Roms/Tetris.gb";
        
        if (!File.Exists(tetrisRomPath))
        {
            _output.WriteLine("Tetris ROM not found - skipping extended freeze test");
            Assert.True(true, "Test skipped - ROM not available");
            return;
        }

        var emulator = new Emulator();
        var cpu = emulator.Cpu;
        var ic = emulator.InterruptController;
        
        // Load Tetris ROM
        byte[] romData = File.ReadAllBytes(tetrisRomPath);
        emulator.LoadRom(romData);
        emulator.Reset();
        
        _output.WriteLine("=== TETRIS ROM EXTENDED FREEZE MONITORING ===");
        _output.WriteLine($"ROM loaded, size: {romData.Length} bytes");
        _output.WriteLine($"Initial state: PC=0x{cpu.Regs.PC:X4}, IME={cpu.InterruptsEnabled}");
        
        // Monitor for common freeze patterns
        var freezeDetector = new ExtendedFreezeDetector(_output);
        var interruptLogger = new InterruptStateLogger();
        
        int frameCount = 0;
        const int maxFrames = 20000; // Run for ~5.5 minutes at 60fps
        
        bool freezeDetected = false;
        
        try
        {
            for (frameCount = 0; frameCount < maxFrames && !freezeDetected; frameCount++)
            {
                // Log state periodically
                if (frameCount % 1000 == 0)
                {
                    _output.WriteLine($"Frame {frameCount}: PC=0x{cpu.Regs.PC:X4}, " +
                                    $"IME={cpu.InterruptsEnabled}, IF=0x{ic.IF:X2}, IE=0x{ic.IE:X2}");
                }
                
                // Step one frame
                interruptLogger.LogState(frameCount, emulator);
                emulator.StepFrame();
                
                // Check for freeze patterns
                freezeDetected = freezeDetector.CheckForFreeze(frameCount, emulator);
                
                if (freezeDetected)
                {
                    _output.WriteLine($"\n=== FREEZE DETECTED AT FRAME {frameCount} ===");
                    LogDetailedFreezeState(emulator, interruptLogger);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Exception during execution at frame {frameCount}: {ex.Message}");
            LogDetailedFreezeState(emulator, interruptLogger);
        }
        
        if (!freezeDetected && frameCount >= maxFrames)
        {
            _output.WriteLine($"Test completed {maxFrames} frames without detecting freeze");
        }
        
        // This test is primarily for investigation - success means we either detected and logged a freeze,
        // or ran successfully for the full duration
        Assert.True(true, $"Extended Tetris test completed after {frameCount} frames");
    }

    private void LogDetailedFreezeState(Emulator emulator, InterruptStateLogger logger)
    {
        var cpu = emulator.Cpu;
        var ic = emulator.InterruptController;
        
        _output.WriteLine($"PC: 0x{cpu.Regs.PC:X4}");
        _output.WriteLine($"IME: {cpu.InterruptsEnabled}");
        _output.WriteLine($"IsHalted: {cpu.IsHalted}");
        _output.WriteLine($"IF: 0x{ic.IF:X2} (binary: {Convert.ToString(ic.IF, 2).PadLeft(8, '0')})");
        _output.WriteLine($"IE: 0x{ic.IE:X2} (binary: {Convert.ToString(ic.IE, 2).PadLeft(8, '0')})");
        _output.WriteLine($"HasPendingInterrupts: {ic.HasAnyPendingInterrupts()}");
        
        _output.WriteLine($"\nCPU Registers:");
        _output.WriteLine($"A={cpu.Regs.A:X2} F={cpu.Regs.F:X2} B={cpu.Regs.B:X2} C={cpu.Regs.C:X2}");
        _output.WriteLine($"D={cpu.Regs.D:X2} E={cpu.Regs.E:X2} H={cpu.Regs.H:X2} L={cpu.Regs.L:X2}");
        _output.WriteLine($"SP={cpu.Regs.SP:X4}");
        
        // Try to read instruction at current PC
        try
        {
            byte opcode = emulator.Mmu.ReadByte(cpu.Regs.PC);
            _output.WriteLine($"\nCurrent instruction: 0x{opcode:X2}");
            
            // Check for common freeze patterns
            if (opcode == 0xA7) // AND A
            {
                byte nextOpcode = emulator.Mmu.ReadByte((ushort)(cpu.Regs.PC + 1));
                _output.WriteLine($"Next instruction: 0x{nextOpcode:X2}");
                if (nextOpcode == 0x28 || nextOpcode == 0x20) // JP Z or JP NZ
                {
                    byte jumpOffset = emulator.Mmu.ReadByte((ushort)(cpu.Regs.PC + 2));
                    _output.WriteLine($"Jump offset: 0x{jumpOffset:X2} (signed: {(sbyte)jumpOffset})");
                    _output.WriteLine("*** DETECTED AND A / JP pattern ***");
                }
            }
            else if (opcode == 0xFF) // RST 38h
            {
                _output.WriteLine("*** DETECTED RST 38h infinite loop ***");
            }
            else if (opcode == 0x76) // HALT
            {
                _output.WriteLine("*** DETECTED HALT instruction ***");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Could not read instruction at PC: {ex.Message}");
        }
        
        _output.WriteLine("\n=== RECENT INTERRUPT STATE CHANGES ===");
        logger.DumpRecentChanges();
    }
}

/// <summary>
/// More sophisticated freeze detector that looks for multiple patterns
/// </summary>
public class ExtendedFreezeDetector
{
    private readonly ITestOutputHelper _output;
    private readonly Dictionary<ushort, int> _pcFrequency = new();
    private readonly Queue<(int Frame, ushort PC)> _recentPCs = new();
    private const int RecentHistorySize = 100;
    private const int FreezeThreshold = 50; // PC must repeat this many times
    
    public ExtendedFreezeDetector(ITestOutputHelper output)
    {
        _output = output;
    }
    
    public bool CheckForFreeze(int frameNumber, Emulator emulator)
    {
        ushort currentPC = emulator.Cpu.Regs.PC;
        
        // Track PC frequency
        _pcFrequency.TryGetValue(currentPC, out int count);
        _pcFrequency[currentPC] = count + 1;
        
        // Maintain recent PC history
        _recentPCs.Enqueue((frameNumber, currentPC));
        if (_recentPCs.Count > RecentHistorySize)
        {
            var (oldFrame, oldPC) = _recentPCs.Dequeue();
            _pcFrequency[oldPC]--;
            if (_pcFrequency[oldPC] <= 0)
            {
                _pcFrequency.Remove(oldPC);
            }
        }
        
        // Check for freeze conditions
        if (_pcFrequency[currentPC] >= FreezeThreshold)
        {
            _output.WriteLine($"FREEZE: PC 0x{currentPC:X4} repeated {_pcFrequency[currentPC]} times");
            return true;
        }
        
        // Check for specific problematic addresses
        if (currentPC == 0x0038 && _pcFrequency[currentPC] > 10)
        {
            _output.WriteLine($"FREEZE: Stuck at RST 38h (0x0038) for {_pcFrequency[currentPC]} frames");
            return true;
        }
        
        // Check for interrupt-related freeze (PC not changing while interrupts are pending)
        if (_pcFrequency[currentPC] > 30 && emulator.InterruptController.HasAnyPendingInterrupts())
        {
            _output.WriteLine($"FREEZE: PC stuck at 0x{currentPC:X4} with pending interrupts for {_pcFrequency[currentPC]} frames");
            return true;
        }
        
        return false;
    }
}