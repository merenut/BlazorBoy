using System;
using System.IO;
using GameBoy.Core;

namespace GameBoy.Tests.Integration;

/// <summary>
/// Harness for running Blargg test ROMs and detecting pass/fail status.
/// Implements the automated test ROM execution strategy outlined in README.md Phase 14.
/// </summary>
public class BlarggHarness
{
    private const int MaxFrames = 10_000;
    private const int ResultsCheckInterval = 100; // Check results every 100 frames

    private readonly Emulator _emulator;

    public BlarggHarness()
    {
        _emulator = new Emulator();
    }

    /// <summary>
    /// Runs a Blargg test ROM and returns the test result.
    /// </summary>
    /// <param name="romPath">Path to the test ROM file</param>
    /// <returns>Test result indicating pass/fail status and any output</returns>
    public BlarggTestResult RunTest(string romPath)
    {
        if (!File.Exists(romPath))
        {
            return new BlarggTestResult(false, $"ROM file not found: {romPath}", TimeSpan.Zero);
        }

        var startTime = DateTime.UtcNow;

        try
        {
            // Load ROM
            byte[] romData = File.ReadAllBytes(romPath);
            _emulator.LoadRom(romData);
            _emulator.Reset();

            // Run emulation loop
            for (int frame = 0; frame < MaxFrames; frame++)
            {
                _emulator.StepFrame();

                // Check for completion every ResultsCheckInterval frames
                if (frame % ResultsCheckInterval == 0)
                {
                    var result = CheckTestCompletion();
                    if (result.HasValue)
                    {
                        var elapsed = DateTime.UtcNow - startTime;
                        return new BlarggTestResult(result.Value.Passed, result.Value.Output, elapsed);
                    }
                }
            }

            // Test timed out
            var timeoutElapsed = DateTime.UtcNow - startTime;
            return new BlarggTestResult(false, "Test ROM execution timed out after 10,000 frames", timeoutElapsed);
        }
        catch (Exception ex)
        {
            var errorElapsed = DateTime.UtcNow - startTime;
            return new BlarggTestResult(false, $"Test ROM execution failed: {ex.Message}", errorElapsed);
        }
    }

    /// <summary>
    /// Checks if the test ROM has completed and returns the result.
    /// Blargg test ROMs typically signal completion by writing results to specific memory locations.
    /// </summary>
    private (bool Passed, string Output)? CheckTestCompletion()
    {
        // Blargg test ROMs typically use these methods to signal completion:
        // 1. Write result to memory location 0xFF02 (serial control register)
        // 2. Write test output string starting at a known memory location
        // 3. Halt execution with specific register values

        // Check serial output method (common in Blargg ROMs)
        byte serialControl = _emulator.Mmu.ReadByte(0xFF02);
        if (serialControl == 0x81) // Start bit set, indicating output ready
        {
            byte serialData = _emulator.Mmu.ReadByte(0xFF01);
            if (serialData != 0)
            {
                // Read the output string from serial buffer or known memory location
                string output = ReadTestOutput();
                bool passed = IsTestPassed(output);
                return (passed, output);
            }
        }

        // Check for test completion via memory signature at 0xA000-0xA003
        // Some Blargg tests write a completion signature here
        uint signature = ReadMemorySignature(0xA000);
        if (signature == 0x50415353) // "PASS" in ASCII
        {
            return (true, "Test completed successfully");
        }
        if (signature == 0x4641494C) // "FAIL" in ASCII  
        {
            string output = ReadTestOutput();
            return (false, string.IsNullOrEmpty(output) ? "Test failed" : output);
        }

        // Check for HALT instruction execution (PC not advancing)
        // This is a fallback method for ROMs that halt after completion
        if (IsHalted())
        {
            string output = ReadTestOutput();
            if (!string.IsNullOrEmpty(output))
            {
                bool passed = IsTestPassed(output);
                return (passed, output);
            }
        }

        return null; // Test still running
    }

    /// <summary>
    /// Reads the test output string from memory.
    /// Blargg ROMs often write results starting at 0xA004 or use serial output.
    /// </summary>
    private string ReadTestOutput()
    {
        var output = new System.Text.StringBuilder();

        // Try reading from common output locations
        // Method 1: Read from 0xA004 onwards (common in cpu_instrs)
        for (ushort addr = 0xA004; addr < 0xA100; addr++)
        {
            byte b = _emulator.Mmu.ReadByte(addr);
            if (b == 0) break; // Null terminator
            if (b >= 0x20 && b <= 0x7E) // Printable ASCII
            {
                output.Append((char)b);
            }
        }

        if (output.Length > 0)
        {
            return output.ToString();
        }

        // Method 2: Check serial output buffer (if implemented)
        // This would require tracking serial writes over time

        return string.Empty;
    }

    /// <summary>
    /// Determines if the test output indicates a passing result.
    /// </summary>
    private static bool IsTestPassed(string output)
    {
        if (string.IsNullOrEmpty(output))
        {
            return false;
        }

        string lowerOutput = output.ToLowerInvariant();

        // Common pass indicators
        if (lowerOutput.Contains("passed") ||
            lowerOutput.Contains("ok") ||
            lowerOutput.Contains("success"))
        {
            return true;
        }

        // Common fail indicators
        if (lowerOutput.Contains("failed") ||
            lowerOutput.Contains("error") ||
            lowerOutput.Contains("fail"))
        {
            return false;
        }

        // For cpu_instrs specifically, check for completion message
        if (lowerOutput.Contains("cpu_instrs") &&
            (lowerOutput.Contains("passed") || !lowerOutput.Contains("failed")))
        {
            return true;
        }

        // Default to failure if output format is unknown
        return false;
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

    /// <summary>
    /// Checks if the CPU is in a halted state.
    /// </summary>
    private bool IsHalted()
    {
        // Simple heuristic: if PC hasn't changed in recent frames, likely halted
        // This is a simplified check - a more robust implementation would track
        // the actual HALT instruction execution state
        return false; // TODO: Implement proper halt detection
    }
}

/// <summary>
/// Represents the result of running a Blargg test ROM.
/// </summary>
public record BlarggTestResult(bool Passed, string Output, TimeSpan ExecutionTime);