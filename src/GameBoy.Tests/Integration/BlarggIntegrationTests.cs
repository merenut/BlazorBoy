using System;
using System.Collections.Generic;
using System.IO;
using GameBoy.Tests.Integration;

namespace GameBoy.Tests.Integration;

/// <summary>
/// Integration tests that run Blargg test ROMs to validate CPU instruction correctness.
/// These tests require the actual test ROM files to be present in the test environment.
/// </summary>
public class BlarggIntegrationTests
{
    private const string TestRomsPath = "TestRoms/Blargg";

    [Fact]
    public void CpuInstrs_ExecutesSuccessfully()
    {
        string romPath = Path.Combine(TestRomsPath, "cpu_instrs.gb");

        // Skip test if ROM file is not available
        if (!File.Exists(romPath))
        {
            return; // Test is skipped - ROM not available for testing
        }

        var harness = new BlarggHarness();
        var result = harness.RunTest(romPath);

        Assert.True(result.Passed, $"cpu_instrs test failed: {result.Output}");
        Assert.True(result.ExecutionTime < TimeSpan.FromMinutes(5),
            $"Test took too long: {result.ExecutionTime}");
    }

    [Fact]
    public void InstrTiming_ExecutesSuccessfully()
    {
        string romPath = Path.Combine(TestRomsPath, "instr_timing.gb");

        // Skip test if ROM file is not available
        if (!File.Exists(romPath))
        {
            return; // Test is skipped - ROM not available for testing
        }

        var harness = new BlarggHarness();
        var result = harness.RunTest(romPath);

        Assert.True(result.Passed, $"instr_timing test failed: {result.Output}");
        Assert.True(result.ExecutionTime < TimeSpan.FromMinutes(5),
            $"Test took too long: {result.ExecutionTime}");
    }

    [Theory]
    [InlineData("01-special.gb", "Special instructions test")]
    [InlineData("02-interrupts.gb", "Interrupt handling test")]
    [InlineData("03-op sp,hl.gb", "OP SP,HL instruction test")]
    [InlineData("04-op r,imm.gb", "OP r,imm instruction test")]
    [InlineData("05-op rp.gb", "OP rp instruction test")]
    [InlineData("06-ld r,r.gb", "LD r,r instruction test")]
    [InlineData("07-jr,jp,call,ret,rst.gb", "Jump and call instruction test")]
    [InlineData("08-misc instrs.gb", "Miscellaneous instruction test")]
    [InlineData("09-op r,r.gb", "OP r,r instruction test")]
    [InlineData("10-bit ops.gb", "Bit operation instruction test")]
    [InlineData("11-op a,(hl).gb", "OP A,(HL) instruction test")]
    public void CpuInstrs_IndividualTests_ExecuteSuccessfully(string romFile, string description)
    {
        string romPath = Path.Combine(TestRomsPath, "cpu_instrs", "individual", romFile);

        // Skip test if ROM file is not available
        if (!File.Exists(romPath))
        {
            return; // Test is skipped - ROM not available for testing
        }

        var harness = new BlarggHarness();
        var result = harness.RunTest(romPath);

        Assert.True(result.Passed, $"{description} failed: {result.Output}");
        Assert.True(result.ExecutionTime < TimeSpan.FromMinutes(2),
            $"Individual test took too long: {result.ExecutionTime}");
    }

    [Fact]
    public void Mem_Timing_ExecutesSuccessfully()
    {
        string romPath = Path.Combine(TestRomsPath, "mem_timing.gb");

        // Skip test if ROM file is not available
        if (!File.Exists(romPath))
        {
            return; // Test is skipped - ROM not available for testing
        }

        var harness = new BlarggHarness();
        var result = harness.RunTest(romPath);

        Assert.True(result.Passed, $"mem_timing test failed: {result.Output}");
        Assert.True(result.ExecutionTime < TimeSpan.FromMinutes(3),
            $"Test took too long: {result.ExecutionTime}");
    }

    [Fact]
    public void Mem_Timing2_ExecutesSuccessfully()
    {
        string romPath = Path.Combine(TestRomsPath, "mem_timing-2.gb");

        // Skip test if ROM file is not available
        if (!File.Exists(romPath))
        {
            return; // Test is skipped - ROM not available for testing
        }

        var harness = new BlarggHarness();
        var result = harness.RunTest(romPath);

        Assert.True(result.Passed, $"mem_timing-2 test failed: {result.Output}");
        Assert.True(result.ExecutionTime < TimeSpan.FromMinutes(3),
            $"Test took too long: {result.ExecutionTime}");
    }

    [Fact]
    public void Halt_Bug_ExecutesSuccessfully()
    {
        string romPath = Path.Combine(TestRomsPath, "halt_bug.gb");

        // Skip test if ROM file is not available
        if (!File.Exists(romPath))
        {
            return; // Test is skipped - ROM not available for testing
        }

        var harness = new BlarggHarness();
        var result = harness.RunTest(romPath);

        Assert.True(result.Passed, $"halt_bug test failed: {result.Output}");
        Assert.True(result.ExecutionTime < TimeSpan.FromMinutes(2),
            $"Test took too long: {result.ExecutionTime}");
    }

    /// <summary>
    /// Validates that the test ROM directory structure exists and is accessible.
    /// This test helps diagnose test environment setup issues.
    /// </summary>
    [Fact]
    public void TestRoms_DirectoryStructure_IsValid()
    {
        // This test validates the expected directory structure for Blargg ROMs
        // It doesn't require actual ROM files to exist, just checks the path setup

        string baseDir = TestRomsPath;
        string individualDir = Path.Combine(baseDir, "cpu_instrs", "individual");

        // These directories should exist if ROMs are properly set up
        bool baseExists = Directory.Exists(baseDir);
        bool individualExists = Directory.Exists(individualDir);

        // If base directory doesn't exist, that's expected (ROMs not provided)
        // But if it does exist, the structure should be correct
        if (baseExists)
        {
            Assert.True(Directory.Exists(baseDir), "Base TestRoms/Blargg directory should exist");

            // Only check for individual directory if we have any cpu_instrs setup
            string cpuInstrsDir = Path.Combine(baseDir, "cpu_instrs");
            if (Directory.Exists(cpuInstrsDir))
            {
                Assert.True(individualExists, "cpu_instrs/individual directory should exist");
            }
        }

        // This test always passes - it's just for diagnostic purposes
        Assert.True(true, $"Test ROM setup check completed. Base: {baseExists}, Individual: {individualExists}");
    }

    /// <summary>
    /// Lists available test ROMs for debugging purposes.
    /// </summary>
    [Fact]
    public void ListAvailableTestRoms()
    {
        if (!Directory.Exists(TestRomsPath))
        {
            Assert.True(true, "TestRoms directory not found - this is expected if ROMs are not provided");
            return;
        }

        var availableRoms = new List<string>();

        // Scan for .gb files
        foreach (string file in Directory.GetFiles(TestRomsPath, "*.gb", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(TestRomsPath, file);
            availableRoms.Add(relativePath);
        }

        string romList = availableRoms.Count > 0
            ? string.Join(", ", availableRoms)
            : "No ROM files found";

        Assert.True(true, $"Available test ROMs: {romList}");
    }
}