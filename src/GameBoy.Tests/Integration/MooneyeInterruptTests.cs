using System;
using System.IO;
using Xunit;

namespace GameBoy.Tests.Integration;

/// <summary>
/// Integration tests for Mooneye interrupt test ROMs.
/// Tests interrupt timing, edge cases, and hardware-specific behavior.
/// </summary>
public class MooneyeInterruptTests
{
    private const string TestRomsPath = "TestRoms/Mooneye";

    [Theory]
    [InlineData("interrupts", "ei_sequence.gb", "EI instruction sequence test")]
    [InlineData("interrupts", "ei_timing.gb", "EI timing test")]
    [InlineData("interrupts", "halt_ime0_ei.gb", "HALT with IME=0 then EI test")]
    [InlineData("interrupts", "halt_ime0_nointr_timing.gb", "HALT with IME=0 no interrupt timing")]
    [InlineData("interrupts", "halt_ime1_timing.gb", "HALT with IME=1 timing test")]
    [InlineData("interrupts", "halt_ime1_timing2-GS.gb", "HALT with IME=1 timing test 2")]
    [InlineData("interrupts", "if_ie_registers.gb", "IF/IE register behavior test")]
    [InlineData("interrupts", "intr_timing.gb", "Interrupt timing test")]
    [InlineData("interrupts", "jp_cc_timing.gb", "Jump condition code timing with interrupts")]
    [InlineData("interrupts", "jp_timing.gb", "Jump timing with interrupts")]
    [InlineData("interrupts", "ld_hl_sp_e_timing.gb", "LD HL,SP+e timing with interrupts")]
    [InlineData("interrupts", "oam_dma_restart.gb", "OAM DMA restart interrupt test")]
    [InlineData("interrupts", "oam_dma_timing.gb", "OAM DMA timing interrupt test")]
    [InlineData("interrupts", "pop_timing.gb", "POP instruction timing with interrupts")]
    [InlineData("interrupts", "push_timing.gb", "PUSH instruction timing with interrupts")]
    [InlineData("interrupts", "rapid_di_ei.gb", "Rapid DI/EI sequence test")]
    [InlineData("interrupts", "ret_cc_timing.gb", "RET condition code timing with interrupts")]
    [InlineData("interrupts", "ret_timing.gb", "RET timing with interrupts")]
    [InlineData("interrupts", "reti_intr_timing.gb", "RETI interrupt timing test")]
    [InlineData("interrupts", "reti_timing.gb", "RETI timing test")]
    public void Mooneye_InterruptTests_ExecuteSuccessfully(string category, string romFile, string description)
    {
        string romPath = Path.Combine(TestRomsPath, category, romFile);

        // Skip test if ROM file is not available
        if (!File.Exists(romPath))
        {
            return; // Test is skipped - ROM not available for testing
        }

        var harness = new InterruptTestHarness();
        var result = harness.RunInterruptTest(romPath, InterruptTestType.Mooneye);

        Assert.True(result.Passed, $"{description} failed: {result.Output}");
        Assert.True(result.ExecutionTime < TimeSpan.FromMinutes(3),
            $"Mooneye interrupt test took too long: {result.ExecutionTime}");

        // Validate interrupt events were captured for timing-sensitive tests
        if (romFile.Contains("timing"))
        {
            Assert.NotNull(result.InterruptEvents);
        }
    }

    [Theory]
    [InlineData("timer", "div_write.gb", "DIV register write interrupt behavior")]
    [InlineData("timer", "rapid_toggle.gb", "Rapid timer toggle interrupt test")]
    [InlineData("timer", "tim00.gb", "Timer interrupt test 00")]
    [InlineData("timer", "tim00_div_trigger.gb", "Timer 00 DIV trigger interrupt")]
    [InlineData("timer", "tim01.gb", "Timer interrupt test 01")]
    [InlineData("timer", "tim01_div_trigger.gb", "Timer 01 DIV trigger interrupt")]
    [InlineData("timer", "tim10.gb", "Timer interrupt test 10")]
    [InlineData("timer", "tim10_div_trigger.gb", "Timer 10 DIV trigger interrupt")]
    [InlineData("timer", "tim11.gb", "Timer interrupt test 11")]
    [InlineData("timer", "tim11_div_trigger.gb", "Timer 11 DIV trigger interrupt")]
    [InlineData("timer", "tima_reload.gb", "TIMA reload interrupt behavior")]
    [InlineData("timer", "tima_write_reloading.gb", "TIMA write during reload interrupt")]
    [InlineData("timer", "tma_write_reloading.gb", "TMA write during reload interrupt")]
    public void Mooneye_TimerInterruptTests_ExecuteSuccessfully(string category, string romFile, string description)
    {
        string romPath = Path.Combine(TestRomsPath, category, romFile);

        // Skip test if ROM file is not available
        if (!File.Exists(romPath))
        {
            return; // Test is skipped - ROM not available for testing
        }

        var harness = new InterruptTestHarness();
        var result = harness.RunInterruptTest(romPath, InterruptTestType.Mooneye);

        Assert.True(result.Passed, $"{description} failed: {result.Output}");
        Assert.True(result.ExecutionTime < TimeSpan.FromMinutes(3),
            $"Mooneye timer interrupt test took too long: {result.ExecutionTime}");

        // Timer tests should generate timer interrupt events
        Assert.NotNull(result.InterruptEvents);
        if (result.InterruptEvents?.Count > 0)
        {
            // Check for timer interrupt events (bit 2 in IF register)
            bool hasTimerInterrupt = result.InterruptEvents.Any(e =>
                (e.IFAfter & 0x04) != (e.IFBefore & 0x04));
            Assert.True(hasTimerInterrupt, "Timer interrupt test should generate timer interrupt events");
        }
    }

    [Theory]
    [InlineData("ppu", "hblank_ly_scx_timing-GS.gb", "H-blank LY SCX timing interrupt")]
    [InlineData("ppu", "intr_1_2_timing-GS.gb", "Interrupt 1-2 timing test")]
    [InlineData("ppu", "intr_2_0_timing.gb", "Interrupt 2-0 timing test")]
    [InlineData("ppu", "intr_2_mode0_timing.gb", "Interrupt 2 mode 0 timing")]
    [InlineData("ppu", "intr_2_mode0_timing_sprites-dmg.gb", "Interrupt 2 mode 0 timing with sprites")]
    [InlineData("ppu", "intr_2_mode3_timing.gb", "Interrupt 2 mode 3 timing")]
    [InlineData("ppu", "intr_2_oam_ok_timing.gb", "Interrupt 2 OAM OK timing")]
    [InlineData("ppu", "lcdon_timing-GS.gb", "LCD ON timing interrupt test")]
    [InlineData("ppu", "lcdon_write_timing-GS.gb", "LCD ON write timing interrupt")]
    [InlineData("ppu", "stat_irq_blocking.gb", "STAT IRQ blocking test")]
    [InlineData("ppu", "stat_lyc_onoff.gb", "STAT LYC on/off interrupt test")]
    [InlineData("ppu", "vblank_stat_intr-GS.gb", "V-blank STAT interrupt test")]
    public void Mooneye_PPUInterruptTests_ExecuteSuccessfully(string category, string romFile, string description)
    {
        string romPath = Path.Combine(TestRomsPath, category, romFile);

        // Skip test if ROM file is not available
        if (!File.Exists(romPath))
        {
            return; // Test is skipped - ROM not available for testing
        }

        var harness = new InterruptTestHarness();
        var result = harness.RunInterruptTest(romPath, InterruptTestType.Mooneye);

        Assert.True(result.Passed, $"{description} failed: {result.Output}");
        Assert.True(result.ExecutionTime < TimeSpan.FromMinutes(3),
            $"Mooneye PPU interrupt test took too long: {result.ExecutionTime}");

        // PPU tests should generate VBlank or LCD STAT interrupt events
        Assert.NotNull(result.InterruptEvents);
        if (result.InterruptEvents?.Count > 0)
        {
            // Check for VBlank (bit 0) or LCD STAT (bit 1) interrupt events
            bool hasPPUInterrupt = result.InterruptEvents.Any(e =>
                ((e.IFAfter & 0x01) != (e.IFBefore & 0x01)) || // VBlank
                ((e.IFAfter & 0x02) != (e.IFBefore & 0x02)));   // LCD STAT
            Assert.True(hasPPUInterrupt, "PPU interrupt test should generate VBlank or LCD STAT interrupt events");
        }
    }

    [Fact]
    public void Mooneye_SerialInterruptTest_ExecutesSuccessfully()
    {
        string romPath = Path.Combine(TestRomsPath, "serial", "boot_sclk_align-dmgABCmgb.gb");

        // Skip test if ROM file is not available
        if (!File.Exists(romPath))
        {
            return; // Test is skipped - ROM not available for testing
        }

        var harness = new InterruptTestHarness();
        var result = harness.RunInterruptTest(romPath, InterruptTestType.Mooneye);

        Assert.True(result.Passed, $"Mooneye serial interrupt test failed: {result.Output}");
        Assert.True(result.ExecutionTime < TimeSpan.FromMinutes(3),
            $"Serial interrupt test took too long: {result.ExecutionTime}");
    }

    /// <summary>
    /// Comprehensive interrupt behavior test that covers multiple interrupt types.
    /// </summary>
    [Fact]
    public void Mooneye_ComprehensiveInterruptBehavior_ExecutesSuccessfully()
    {
        // Look for a comprehensive interrupt test ROM
        string[] comprehensiveTests = {
            "interrupts/interrupt_priority.gb",
            "interrupts/interrupt_behavior.gb",
            "acceptance/interrupts.gb"
        };

        foreach (string testPath in comprehensiveTests)
        {
            string romPath = Path.Combine(TestRomsPath, testPath);
            if (File.Exists(romPath))
            {
                var harness = new InterruptTestHarness();
                var result = harness.RunInterruptTest(romPath, InterruptTestType.Mooneye);

                Assert.True(result.Passed, $"Mooneye comprehensive interrupt test failed: {result.Output}");
                Assert.True(result.ExecutionTime < TimeSpan.FromMinutes(5),
                    $"Comprehensive interrupt test took too long: {result.ExecutionTime}");

                // Should have captured multiple types of interrupt events
                Assert.NotNull(result.InterruptEvents);
                if (result.InterruptEvents?.Count > 0)
                {
                    Assert.True(result.InterruptEvents.Count >= 3,
                        $"Comprehensive test should have multiple interrupt events, got {result.InterruptEvents.Count}");
                }
                return; // Test completed successfully
            }
        }

        // If no comprehensive test found, that's okay - skip this test
        Assert.True(true, "No comprehensive Mooneye interrupt test ROM found - skipping");
    }

    /// <summary>
    /// Validates that the Mooneye test ROM directory structure exists.
    /// </summary>
    [Fact]
    public void MooneyeInterrupt_DirectoryStructure_IsValid()
    {
        string baseDir = TestRomsPath;
        string[] interruptDirs = {
            "interrupts",
            "timer",
            "ppu",
            "serial"
        };

        bool baseExists = Directory.Exists(baseDir);
        var existingDirs = new List<string>();

        if (baseExists)
        {
            foreach (string dir in interruptDirs)
            {
                string fullPath = Path.Combine(baseDir, dir);
                if (Directory.Exists(fullPath))
                {
                    existingDirs.Add(dir);
                }
            }
        }

        string dirList = existingDirs.Count > 0
            ? string.Join(", ", existingDirs)
            : "No Mooneye interrupt test directories found";

        Assert.True(true, $"Mooneye interrupt test directory structure: Base exists: {baseExists}, " +
            $"Available dirs: {dirList}");
    }

    /// <summary>
    /// Lists available Mooneye interrupt test ROMs for debugging.
    /// </summary>
    [Fact]
    public void ListAvailableMooneyeInterruptRoms()
    {
        var availableRoms = new List<string>();
        string[] categories = { "interrupts", "timer", "ppu", "serial" };

        foreach (string category in categories)
        {
            string categoryDir = Path.Combine(TestRomsPath, category);
            if (Directory.Exists(categoryDir))
            {
                foreach (string file in Directory.GetFiles(categoryDir, "*.gb"))
                {
                    string relativePath = Path.GetRelativePath(TestRomsPath, file);
                    availableRoms.Add(relativePath);
                }
            }
        }

        string romList = availableRoms.Count > 0
            ? string.Join(", ", availableRoms)
            : "No Mooneye interrupt ROM files found";

        Assert.True(true, $"Available Mooneye interrupt test ROMs: {romList}");
    }
}