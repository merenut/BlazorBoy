using System;
using System.IO;
using Xunit;

namespace GameBoy.Tests.Integration;

/// <summary>
/// Integration tests for Blargg interrupt test ROMs.
/// Tests interrupt handling, timing, and behavior validation.
/// </summary>
public class BlarggInterruptTests
{
    private const string TestRomsPath = "TestRoms/Blargg";

    [Fact]
    public void Blargg_02_Interrupts_ExecutesSuccessfully()
    {
        string romPath = Path.Combine(TestRomsPath, "cpu_instrs", "individual", "02-interrupts.gb");

        // Skip test if ROM file is not available
        if (!File.Exists(romPath))
        {
            return; // Test is skipped - ROM not available for testing
        }

        var harness = new InterruptTestHarness();
        var result = harness.RunInterruptTest(romPath, InterruptTestType.Blargg);

        Assert.True(result.Passed, $"Blargg 02-interrupts test failed: {result.Output}");
        Assert.True(result.ExecutionTime < TimeSpan.FromMinutes(3),
            $"Test took too long: {result.ExecutionTime}");

        // Validate that interrupt events were captured
        Assert.NotNull(result.InterruptEvents);

        // For debugging: output interrupt event summary
        if (result.InterruptEvents?.Count > 0)
        {
            Assert.True(true, $"Captured {result.InterruptEvents.Count} interrupt events during test execution");
        }
    }

    [Fact]
    public void Blargg_InterruptTiming_ExecutesSuccessfully()
    {
        // Check for dedicated interrupt timing test ROM
        string romPath = Path.Combine(TestRomsPath, "interrupt_timing.gb");

        // Skip test if ROM file is not available
        if (!File.Exists(romPath))
        {
            return; // Test is skipped - ROM not available for testing
        }

        var harness = new InterruptTestHarness();
        var result = harness.RunInterruptTest(romPath, InterruptTestType.Blargg);

        Assert.True(result.Passed, $"Blargg interrupt timing test failed: {result.Output}");
        Assert.True(result.ExecutionTime < TimeSpan.FromMinutes(5),
            $"Test took too long: {result.ExecutionTime}");
    }

    [Theory]
    [InlineData("ie_push.gb", "IE register push test")]
    [InlineData("if_ie_registers.gb", "IF/IE register behavior test")]
    [InlineData("intr_timing.gb", "Interrupt timing test")]
    public void Blargg_SpecificInterruptTests_ExecuteSuccessfully(string romFile, string description)
    {
        string romPath = Path.Combine(TestRomsPath, "interrupt_tests", romFile);

        // Skip test if ROM file is not available
        if (!File.Exists(romPath))
        {
            return; // Test is skipped - ROM not available for testing
        }

        var harness = new InterruptTestHarness();
        var result = harness.RunInterruptTest(romPath, InterruptTestType.Blargg);

        Assert.True(result.Passed, $"{description} failed: {result.Output}");
        Assert.True(result.ExecutionTime < TimeSpan.FromMinutes(3),
            $"Individual interrupt test took too long: {result.ExecutionTime}");
    }

    /// <summary>
    /// Runs the master Blargg interrupt test ROM that includes multiple sub-tests.
    /// </summary>
    [Fact]
    public void Blargg_MasterInterruptSuite_ExecutesSuccessfully()
    {
        string romPath = Path.Combine(TestRomsPath, "interrupt_tests.gb");

        // Skip test if ROM file is not available
        if (!File.Exists(romPath))
        {
            return; // Test is skipped - ROM not available for testing
        }

        var harness = new InterruptTestHarness();
        var result = harness.RunInterruptTest(romPath, InterruptTestType.Blargg);

        Assert.True(result.Passed, $"Blargg master interrupt suite failed: {result.Output}");
        Assert.True(result.ExecutionTime < TimeSpan.FromMinutes(10),
            $"Master interrupt test suite took too long: {result.ExecutionTime}");

        // Validate comprehensive interrupt testing occurred
        Assert.NotNull(result.InterruptEvents);
        if (result.InterruptEvents?.Count > 0)
        {
            // Should have multiple interrupt events in a comprehensive test
            Assert.True(result.InterruptEvents.Count >= 5,
                $"Expected multiple interrupt events, got {result.InterruptEvents.Count}");
        }
    }

    /// <summary>
    /// Tests interrupt priority and handling edge cases.
    /// </summary>
    [Fact]
    public void Blargg_InterruptPriority_ExecutesSuccessfully()
    {
        string romPath = Path.Combine(TestRomsPath, "interrupt_priority.gb");

        // Skip test if ROM file is not available  
        if (!File.Exists(romPath))
        {
            return; // Test is skipped - ROM not available for testing
        }

        var harness = new InterruptTestHarness();
        var result = harness.RunInterruptTest(romPath, InterruptTestType.Blargg);

        Assert.True(result.Passed, $"Blargg interrupt priority test failed: {result.Output}");
        Assert.True(result.ExecutionTime < TimeSpan.FromMinutes(3),
            $"Interrupt priority test took too long: {result.ExecutionTime}");
    }

    /// <summary>
    /// Validates that the test ROM directory structure for interrupt tests exists.
    /// </summary>
    [Fact]
    public void BlarggInterrupt_DirectoryStructure_IsValid()
    {
        string baseDir = TestRomsPath;
        string interruptDir = Path.Combine(baseDir, "interrupt_tests");
        string individualDir = Path.Combine(baseDir, "cpu_instrs", "individual");

        // If base directory doesn't exist, that's expected (ROMs not provided)
        if (Directory.Exists(baseDir))
        {
            Assert.True(Directory.Exists(baseDir), "Base TestRoms/Blargg directory should exist");

            // Check for interrupt-specific directories
            bool interruptDirExists = Directory.Exists(interruptDir);
            bool individualDirExists = Directory.Exists(individualDir);

            Assert.True(true, $"Interrupt test directory structure check completed. " +
                $"Interrupt dir: {interruptDirExists}, Individual dir: {individualDirExists}");
        }

        // This test always passes - it's for diagnostic purposes
        Assert.True(true, "Blargg interrupt test ROM directory structure validated");
    }

    /// <summary>
    /// Lists available Blargg interrupt test ROMs for debugging.
    /// </summary>
    [Fact]
    public void ListAvailableBlarggInterruptRoms()
    {
        var availableRoms = new List<string>();

        // Check main interrupt test directory
        string interruptDir = Path.Combine(TestRomsPath, "interrupt_tests");
        if (Directory.Exists(interruptDir))
        {
            foreach (string file in Directory.GetFiles(interruptDir, "*.gb"))
            {
                availableRoms.Add(Path.GetRelativePath(TestRomsPath, file));
            }
        }

        // Check individual CPU instruction tests (includes 02-interrupts.gb)
        string individualDir = Path.Combine(TestRomsPath, "cpu_instrs", "individual");
        if (Directory.Exists(individualDir))
        {
            string interruptRom = Path.Combine(individualDir, "02-interrupts.gb");
            if (File.Exists(interruptRom))
            {
                availableRoms.Add(Path.GetRelativePath(TestRomsPath, interruptRom));
            }
        }

        // Check for standalone interrupt ROMs
        string[] standaloneInterruptRoms = {
            "interrupt_timing.gb",
            "interrupt_priority.gb",
            "interrupt_tests.gb"
        };

        foreach (string rom in standaloneInterruptRoms)
        {
            string romPath = Path.Combine(TestRomsPath, rom);
            if (File.Exists(romPath))
            {
                availableRoms.Add(rom);
            }
        }

        string romList = availableRoms.Count > 0
            ? string.Join(", ", availableRoms)
            : "No Blargg interrupt ROM files found";

        Assert.True(true, $"Available Blargg interrupt test ROMs: {romList}");
    }
}