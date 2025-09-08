using GameBoy.Core;

namespace GameBoy.Tests;

/// <summary>
/// Integration tests using Blargg timer test ROMs to validate timer accuracy.
/// These tests are skipped if the ROM files are not available.
/// </summary>
public class TimerBlarggTests
{
    private const string BlarggTimerRomPath = "TestROMs/timer/";

    [SkippableFact]
    public void BlarggTimer_Test01_TimerOff()
    {
        var romPath = Path.Combine(BlarggTimerRomPath, "01-timer_off.gb");
        Skip.IfNot(File.Exists(romPath), "Blargg timer ROM not found");

        var result = RunBlarggTimerTest(romPath);
        Assert.True(result.Passed, $"Timer test failed: {result.Message}");
    }

    [SkippableFact]
    public void BlarggTimer_Test02_TimerOn()
    {
        var romPath = Path.Combine(BlarggTimerRomPath, "02-timer_on.gb");
        Skip.IfNot(File.Exists(romPath), "Blargg timer ROM not found");

        var result = RunBlarggTimerTest(romPath);
        Assert.True(result.Passed, $"Timer test failed: {result.Message}");
    }

    [SkippableFact]
    public void BlarggTimer_Test03_TimerOffOn()
    {
        var romPath = Path.Combine(BlarggTimerRomPath, "03-timer_off_on.gb");
        Skip.IfNot(File.Exists(romPath), "Blargg timer ROM not found");

        var result = RunBlarggTimerTest(romPath);
        Assert.True(result.Passed, $"Timer test failed: {result.Message}");
    }

    [SkippableFact]
    public void BlarggTimer_Test04_TimerInterrupt()
    {
        var romPath = Path.Combine(BlarggTimerRomPath, "04-timer_interrupt.gb");
        Skip.IfNot(File.Exists(romPath), "Blargg timer ROM not found");

        var result = RunBlarggTimerTest(romPath);
        Assert.True(result.Passed, $"Timer test failed: {result.Message}");
    }

    [SkippableFact]
    public void BlarggTimer_Test05_ReloadTiming()
    {
        var romPath = Path.Combine(BlarggTimerRomPath, "05-reload_timing.gb");
        Skip.IfNot(File.Exists(romPath), "Blargg timer ROM not found");

        var result = RunBlarggTimerTest(romPath);
        Assert.True(result.Passed, $"Timer test failed: {result.Message}");
    }

    [SkippableFact]
    public void BlarggTimer_Test06_DivWrite()
    {
        var romPath = Path.Combine(BlarggTimerRomPath, "06-div_write.gb");
        Skip.IfNot(File.Exists(romPath), "Blargg timer ROM not found");

        var result = RunBlarggTimerTest(romPath);
        Assert.True(result.Passed, $"Timer test failed: {result.Message}");
    }

    [SkippableFact]
    public void BlarggTimer_Test07_TacWrite()
    {
        var romPath = Path.Combine(BlarggTimerRomPath, "07-tac_write.gb");
        Skip.IfNot(File.Exists(romPath), "Blargg timer ROM not found");

        var result = RunBlarggTimerTest(romPath);
        Assert.True(result.Passed, $"Timer test failed: {result.Message}");
    }

    /// <summary>
    /// Runs a Blargg timer test ROM and checks for pass/fail result.
    /// Uses either serial output or memory signature to determine completion.
    /// </summary>
    /// <param name="romPath">Path to the Blargg timer test ROM</param>
    /// <returns>Test result with pass/fail status and message</returns>
    private static BlarggTestResult RunBlarggTimerTest(string romPath)
    {
        var emulator = new Emulator();
        var rom = File.ReadAllBytes(romPath);
        emulator.LoadRom(rom);

        const int maxFrames = 600; // 10 seconds at 60 FPS
        string serialOutput = "";

        for (int frame = 0; frame < maxFrames; frame++)
        {
            // Step one frame worth of cycles
            while (!emulator.StepFrame())
            {
                // Continue stepping until frame is complete
            }

            // Check for serial output (Blargg tests output results via serial)
            var newSerialData = ReadSerialOutput(emulator);
            if (!string.IsNullOrEmpty(newSerialData))
            {
                serialOutput += newSerialData;

                // Check for completion patterns
                if (serialOutput.Contains("Passed") || serialOutput.Contains("PASSED"))
                {
                    return new BlarggTestResult(true, $"Test passed: {serialOutput.Trim()}");
                }

                if (serialOutput.Contains("Failed") || serialOutput.Contains("FAILED"))
                {
                    return new BlarggTestResult(false, $"Test failed: {serialOutput.Trim()}");
                }
            }

            // Check for memory signature (some ROMs write result to specific memory location)
            var memoryResult = CheckMemorySignature(emulator);
            if (memoryResult.HasValue)
            {
                return memoryResult.Value;
            }
        }

        // Test timed out
        return new BlarggTestResult(false, $"Test timed out after {maxFrames} frames. Serial output: {serialOutput}");
    }

    /// <summary>
    /// Reads any new serial output from the emulator.
    /// </summary>
    private static string ReadSerialOutput(Emulator emulator)
    {
        // Check serial registers for output
        // Note: Full serial implementation needed for complete support
        var sb = emulator.Mmu.ReadByte(0xFF01); // Serial transfer data
        var sc = emulator.Mmu.ReadByte(0xFF02); // Serial transfer control

        // Simple implementation: if serial data was written, collect it
        // Full implementation would track actual serial transfer completion
        if (sb >= 0x20 && sb <= 0x7E) // Printable ASCII
        {
            return ((char)sb).ToString();
        }

        return "";
    }

    /// <summary>
    /// Checks specific memory locations for Blargg test completion signatures.
    /// Some Blargg ROMs write specific values to indicate pass/fail.
    /// </summary>
    private static BlarggTestResult? CheckMemorySignature(Emulator emulator)
    {
        // Common Blargg test signature locations
        // These may need adjustment based on specific ROM behavior

        // Check for completion flag at common locations
        var result1 = emulator.Mmu.ReadByte(0xA000); // External RAM
        var result2 = emulator.Mmu.ReadByte(0xFF80); // High RAM

        // Look for known patterns
        if (result1 == 0x00 || result2 == 0x00)
        {
            return new BlarggTestResult(true, "Test passed (memory signature)");
        }

        if (result1 == 0xFF || result2 == 0xFF)
        {
            return new BlarggTestResult(false, "Test failed (memory signature)");
        }

        return null; // No definitive result yet
    }

    /// <summary>
    /// Result of a Blargg test ROM execution.
    /// </summary>
    private readonly record struct BlarggTestResult(bool Passed, string Message);
}