using System;
using System.IO;
using GameBoy.Core;

namespace GameBoy.Tests.Integration;

/// <summary>
/// Specialized harness for running interrupt-focused test ROMs from Blargg and Mooneye.
/// Provides enhanced detection and reporting specifically for interrupt behavior validation.
/// </summary>
public class InterruptTestHarness
{
    private const int MaxFrames = 15_000; // Slightly longer timeout for interrupt tests
    private const int ResultsCheckInterval = 50; // Check more frequently for interrupt tests

    private readonly Emulator _emulator;

    public InterruptTestHarness()
    {
        _emulator = new Emulator();
    }

    /// <summary>
    /// Runs an interrupt test ROM and returns detailed interrupt-specific results.
    /// </summary>
    /// <param name="romPath">Path to the interrupt test ROM file</param>
    /// <param name="testType">Type of interrupt test (Blargg or Mooneye)</param>
    /// <returns>Detailed interrupt test result</returns>
    public InterruptTestResult RunInterruptTest(string romPath, InterruptTestType testType)
    {
        if (!File.Exists(romPath))
        {
            return new InterruptTestResult(
                false,
                $"ROM file not found: {romPath}",
                TimeSpan.Zero,
                testType,
                null);
        }

        var startTime = DateTime.UtcNow;
        var interruptEvents = new List<InterruptEvent>();

        try
        {
            // Load ROM
            byte[] romData = File.ReadAllBytes(romPath);
            _emulator.LoadRom(romData);
            _emulator.Reset();

            // Track initial interrupt state
            var initialIF = _emulator.InterruptController.IF;
            var initialIE = _emulator.InterruptController.IE;

            // Run emulation loop with interrupt monitoring
            for (int frame = 0; frame < MaxFrames; frame++)
            {
                // Capture interrupt state before step
                var ifBefore = _emulator.InterruptController.IF;
                var ieBefore = _emulator.InterruptController.IE;

                _emulator.StepFrame();

                // Capture interrupt state after step
                var ifAfter = _emulator.InterruptController.IF;
                var ieAfter = _emulator.InterruptController.IE;

                // Track interrupt flag changes
                if (ifBefore != ifAfter || ieBefore != ieAfter)
                {
                    interruptEvents.Add(new InterruptEvent(
                        frame,
                        ifBefore, ifAfter,
                        ieBefore, ieAfter));
                }

                // Check for completion every ResultsCheckInterval frames
                if (frame % ResultsCheckInterval == 0)
                {
                    var result = CheckInterruptTestCompletion(testType);
                    if (result.HasValue)
                    {
                        var elapsed = DateTime.UtcNow - startTime;
                        return new InterruptTestResult(
                            result.Value.Passed,
                            result.Value.Output,
                            elapsed,
                            testType,
                            interruptEvents);
                    }
                }
            }

            // Test timed out
            var timeoutElapsed = DateTime.UtcNow - startTime;
            return new InterruptTestResult(
                false,
                "Interrupt test ROM execution timed out",
                timeoutElapsed,
                testType,
                interruptEvents);
        }
        catch (Exception ex)
        {
            var errorElapsed = DateTime.UtcNow - startTime;
            return new InterruptTestResult(
                false,
                $"Interrupt test ROM execution failed: {ex.Message}",
                errorElapsed,
                testType,
                interruptEvents);
        }
    }

    /// <summary>
    /// Checks if the interrupt test ROM has completed using test-type-specific detection.
    /// </summary>
    private (bool Passed, string Output)? CheckInterruptTestCompletion(InterruptTestType testType)
    {
        return testType switch
        {
            InterruptTestType.Blargg => CheckBlarggInterruptCompletion(),
            InterruptTestType.Mooneye => CheckMooneyeInterruptCompletion(),
            _ => null
        };
    }

    /// <summary>
    /// Checks completion for Blargg interrupt test ROMs.
    /// </summary>
    private (bool Passed, string Output)? CheckBlarggInterruptCompletion()
    {
        // Blargg interrupt tests often use serial output
        byte serialControl = _emulator.Mmu.ReadByte(0xFF02);
        if (serialControl == 0x81) // Start bit set, indicating output ready
        {
            byte serialData = _emulator.Mmu.ReadByte(0xFF01);
            if (serialData != 0)
            {
                string output = ReadBlarggTestOutput();
                bool passed = IsBlarggInterruptTestPassed(output);
                return (passed, output);
            }
        }

        // Check for memory signature completion
        uint signature = ReadMemorySignature(0xA000);
        if (signature == 0x50415353) // "PASS" in ASCII
        {
            return (true, "Blargg interrupt test passed");
        }
        if (signature == 0x4641494C) // "FAIL" in ASCII
        {
            string output = ReadBlarggTestOutput();
            return (false, string.IsNullOrEmpty(output) ? "Blargg interrupt test failed" : output);
        }

        // Check for interrupt-specific completion patterns
        // Blargg 02-interrupts.gb often writes to specific locations
        byte interruptResult = _emulator.Mmu.ReadByte(0xA002);
        if (interruptResult == 0x00)
        {
            string detailedOutput = ReadBlarggTestOutput();
            if (!string.IsNullOrEmpty(detailedOutput))
            {
                return (true, $"Interrupt test passed: {detailedOutput}");
            }
        }

        return null; // Test still running
    }

    /// <summary>
    /// Checks completion for Mooneye interrupt test ROMs.
    /// </summary>
    private (bool Passed, string Output)? CheckMooneyeInterruptCompletion()
    {
        // Mooneye interrupt tests often use specific memory signatures
        // Check high RAM locations commonly used by Mooneye
        uint signature = ReadMemorySignature(0xFF80);

        // Common Mooneye interrupt test completion signatures
        if (signature == 0x03050307) // Pass signature
        {
            return (true, "Mooneye interrupt test passed");
        }
        if (signature == 0xDEADBEEF) // Failure signature
        {
            return (false, "Mooneye interrupt test failed");
        }

        // Check for register-based completion specific to interrupt tests
        var regs = _emulator.Cpu.Regs;
        if (regs.B == 3 && regs.C == 5 && regs.D == 8 && regs.A == 0)
        {
            return (true, "Mooneye interrupt test passed (register pattern)");
        }
        if (regs.B == 3 && regs.C == 5 && regs.D == 8 && regs.A != 0)
        {
            return (false, $"Mooneye interrupt test failed (A={regs.A:X2})");
        }

        // Check for interrupt-specific VRAM patterns
        if (IsInterruptVramCompletion())
        {
            bool passed = CheckInterruptVramPattern();
            return (passed, passed ? "Interrupt VRAM test passed" : "Interrupt VRAM test failed");
        }

        return null; // Test still running
    }

    /// <summary>
    /// Reads test output from Blargg interrupt test ROMs.
    /// </summary>
    private string ReadBlarggTestOutput()
    {
        var output = new System.Text.StringBuilder();

        // Read from common Blargg output locations
        for (ushort addr = 0xA004; addr < 0xA100; addr++)
        {
            byte b = _emulator.Mmu.ReadByte(addr);
            if (b == 0) break; // Null terminator
            if (b >= 0x20 && b <= 0x7E) // Printable ASCII
            {
                output.Append((char)b);
            }
        }

        return output.ToString();
    }

    /// <summary>
    /// Determines if Blargg interrupt test output indicates success.
    /// </summary>
    private static bool IsBlarggInterruptTestPassed(string output)
    {
        if (string.IsNullOrEmpty(output))
            return false;

        string lower = output.ToLowerInvariant();

        // Interrupt-specific pass indicators
        if (lower.Contains("interrupt") && (lower.Contains("passed") || lower.Contains("ok")))
            return true;

        // General pass indicators
        if (lower.Contains("passed") || lower.Contains("ok") || lower.Contains("success"))
            return true;

        // Interrupt-specific failure indicators
        if (lower.Contains("interrupt") && (lower.Contains("failed") || lower.Contains("error")))
            return false;

        return false;
    }

    /// <summary>
    /// Checks if VRAM contains interrupt test completion patterns.
    /// </summary>
    private bool IsInterruptVramCompletion()
    {
        // Look for specific patterns that Mooneye interrupt tests might write
        byte pattern1 = _emulator.Mmu.ReadByte(0x8000);
        byte pattern2 = _emulator.Mmu.ReadByte(0x8010);

        // Interrupt tests might write specific patterns
        return pattern1 == 0xAA && pattern2 == 0x55;
    }

    /// <summary>
    /// Validates VRAM interrupt test pattern for pass/fail.
    /// </summary>
    private bool CheckInterruptVramPattern()
    {
        // Check for expected interrupt test VRAM patterns
        for (int i = 0; i < 8; i++)
        {
            byte expected = (byte)(0x10 + i); // Expected pattern
            byte actual = _emulator.Mmu.ReadByte((ushort)(0x8000 + i));
            if (actual != expected)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Reads a 32-bit signature from memory in little-endian format.
    /// </summary>
    private uint ReadMemorySignature(ushort address)
    {
        byte b0 = _emulator.Mmu.ReadByte(address);
        byte b1 = _emulator.Mmu.ReadByte((ushort)(address + 1));
        byte b2 = _emulator.Mmu.ReadByte((ushort)(address + 2));
        byte b3 = _emulator.Mmu.ReadByte((ushort)(address + 3));

        return (uint)(b0 | (b1 << 8) | (b2 << 16) | (b3 << 24));
    }
}

/// <summary>
/// Type of interrupt test ROM.
/// </summary>
public enum InterruptTestType
{
    Blargg,
    Mooneye
}

/// <summary>
/// Represents an interrupt state change event during test execution.
/// </summary>
public record InterruptEvent(
    int Frame,
    byte IFBefore, byte IFAfter,
    byte IEBefore, byte IEAfter);

/// <summary>
/// Result of running an interrupt test ROM with detailed interrupt-specific information.
/// </summary>
public record InterruptTestResult(
    bool Passed,
    string Output,
    TimeSpan ExecutionTime,
    InterruptTestType TestType,
    List<InterruptEvent>? InterruptEvents);