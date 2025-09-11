using System;
using System.IO;
using GameBoy.Core;
using Xunit;

namespace GameBoy.Tests;

/// <summary>
/// Tests to investigate and reproduce the ROM freeze issue described in #139.
/// Focuses on Tetris ROM freezing after credits screen, waiting for interrupts.
/// </summary>
public class TetrisFreezeInvestigationTests
{
    private const string TetrisRomPath = "Roms/Tetris.gb";

    [Fact]
    public void Tetris_ShouldNotFreezeAfterCredits_WithInterruptLogging()
    {
        // Skip test if ROM not found
        if (!File.Exists(TetrisRomPath))
        {
            Assert.True(true, "Tetris ROM not found - skipping freeze investigation test");
            return;
        }

        var emulator = new Emulator();
        var romData = File.ReadAllBytes(TetrisRomPath);
        emulator.LoadRom(romData);
        emulator.Reset();

        // Track interrupt state changes and potential freeze patterns
        var freezeDetector = new FreezeDetector();
        var interruptLogger = new InterruptStateLogger();

        int maxFrames = 15000; // Run for up to 15000 frames (~250 seconds at 60fps)
        bool freezeDetected = false;

        for (int frame = 0; frame < maxFrames && !freezeDetected; frame++)
        {
            // Log interrupt state before frame step
            interruptLogger.LogState(frame, emulator);

            // Step one frame
            emulator.StepFrame();

            // Check for freeze pattern (PC stuck in same loop)
            freezeDetected = freezeDetector.CheckForFreeze(emulator.Cpu.Regs.PC);

            if (freezeDetected)
            {
                // Log detailed state when freeze is detected
                LogFreezeState(frame, emulator, interruptLogger);
                break;
            }
        }

        // Test should not detect a freeze - if it does, we have identified the issue
        Assert.False(freezeDetected,
            $"Tetris ROM froze after {freezeDetector.FreezeFrame} frames at PC={freezeDetector.FreezePC:X4}. " +
            "Check logged interrupt states for root cause analysis.");
    }

    /// <summary>
    /// Logs detailed freeze state for analysis.
    /// </summary>
    private void LogFreezeState(int frame, Emulator emulator, InterruptStateLogger logger)
    {
        var cpu = emulator.Cpu;
        var ic = emulator.InterruptController;

        Console.WriteLine($"=== FREEZE DETECTED AT FRAME {frame} ===");
        Console.WriteLine($"PC: 0x{cpu.Regs.PC:X4}");
        Console.WriteLine($"IME: {cpu.InterruptsEnabled}");
        Console.WriteLine($"IF: 0x{ic.IF:X2}");
        Console.WriteLine($"IE: 0x{ic.IE:X2}");
        Console.WriteLine($"IsHalted: {cpu.IsHalted}");
        Console.WriteLine($"Registers: A={cpu.Regs.A:X2} F={cpu.Regs.F:X2} B={cpu.Regs.B:X2} C={cpu.Regs.C:X2}");
        Console.WriteLine($"           D={cpu.Regs.D:X2} E={cpu.Regs.E:X2} H={cpu.Regs.H:X2} L={cpu.Regs.L:X2}");
        Console.WriteLine($"           SP={cpu.Regs.SP:X4}");

        // Log recent interrupt state changes
        Console.WriteLine("\n=== RECENT INTERRUPT STATE CHANGES ===");
        logger.DumpRecentChanges();

        // Try to disassemble instruction at freeze PC
        try
        {
            byte opcode = emulator.Mmu.ReadByte(cpu.Regs.PC);
            Console.WriteLine($"\nInstruction at freeze PC: 0x{opcode:X2}");

            // Check for common freeze patterns
            if (opcode == 0xA7) // AND A
            {
                byte nextOpcode = emulator.Mmu.ReadByte((ushort)(cpu.Regs.PC + 1));
                if (nextOpcode == 0x28 || nextOpcode == 0x20) // JP Z or JP NZ
                {
                    Console.WriteLine("Detected AND A / JP Z/NZ pattern - likely waiting for interrupt");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not read instruction at PC: {ex.Message}");
        }
    }
}

/// <summary>
/// Detects freeze patterns by monitoring PC for stuck loops.
/// </summary>
public class FreezeDetector
{
    private readonly Dictionary<ushort, int> _pcHitCounts = new();
    private const int FreezeThreshold = 100; // If PC repeats 100 times, consider it frozen

    public bool IsFrozen { get; private set; }
    public ushort FreezePC { get; private set; }
    public int FreezeFrame { get; private set; }

    public bool CheckForFreeze(ushort currentPC)
    {
        if (IsFrozen) return true;

        _pcHitCounts.TryGetValue(currentPC, out int hitCount);
        _pcHitCounts[currentPC] = hitCount + 1;

        if (_pcHitCounts[currentPC] >= FreezeThreshold)
        {
            IsFrozen = true;
            FreezePC = currentPC;
            FreezeFrame = _pcHitCounts.Values.Sum();
            return true;
        }

        return false;
    }
}

/// <summary>
/// Logs interrupt state changes for analysis.
/// </summary>
public class InterruptStateLogger
{
    private readonly List<InterruptStateSnapshot> _snapshots = new();
    private byte _lastIF = 0;
    private byte _lastIE = 0;
    private bool _lastIME = false;

    public void LogState(int frame, Emulator emulator)
    {
        var ic = emulator.InterruptController;
        var cpu = emulator.Cpu;

        byte currentIF = ic.IF;
        byte currentIE = ic.IE;
        bool currentIME = cpu.InterruptsEnabled;

        // Only log when there's a change or every 100 frames
        if (currentIF != _lastIF || currentIE != _lastIE || currentIME != _lastIME || frame % 100 == 0)
        {
            _snapshots.Add(new InterruptStateSnapshot
            {
                Frame = frame,
                PC = cpu.Regs.PC,
                IF = currentIF,
                IE = currentIE,
                IME = currentIME,
                IsHalted = cpu.IsHalted
            });

            _lastIF = currentIF;
            _lastIE = currentIE;
            _lastIME = currentIME;
        }
    }

    public void DumpRecentChanges()
    {
        var recent = _snapshots.TakeLast(10);
        foreach (var snapshot in recent)
        {
            Console.WriteLine($"Frame {snapshot.Frame:D4}: PC=0x{snapshot.PC:X4} " +
                            $"IF=0x{snapshot.IF:X2} IE=0x{snapshot.IE:X2} " +
                            $"IME={snapshot.IME} HALT={snapshot.IsHalted}");
        }
    }
}

/// <summary>
/// Snapshot of interrupt state at a specific frame.
/// </summary>
public record InterruptStateSnapshot
{
    public int Frame { get; init; }
    public ushort PC { get; init; }
    public byte IF { get; init; }
    public byte IE { get; init; }
    public bool IME { get; init; }
    public bool IsHalted { get; init; }
}