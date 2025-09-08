using GameBoy.Core;

namespace GameBoy.Tests;

/// <summary>
/// Integration tests to verify each subsystem properly requests interrupts via InterruptController.
/// </summary>
public class InterruptIntegrationTests
{
    [Fact]
    public void PPU_Step_RequestsVBlankInterrupt()
    {
        var interruptController = new InterruptController();
        interruptController.SetIF(0x00); // Clear all interrupt flags
        var ppu = new Ppu(interruptController);

        // Step the PPU enough cycles to complete a frame - should request VBlank interrupt
        ppu.Step(70224);

        // Verify VBlank interrupt was requested (bit 0)
        Assert.Equal(0xE1, interruptController.IF); // 0x01 (VBlank) | 0xE0 (upper bits)
    }

    [Fact]
    public void Timer_Step_RequestsTimerInterrupt()
    {
        var interruptController = new InterruptController();
        interruptController.SetIF(0x00); // Clear all interrupt flags
        var timer = new GameBoy.Core.Timer(interruptController);

        // Step the timer until it triggers an interrupt (1024 cycles)
        timer.Step(1024);

        // Verify Timer interrupt was requested (bit 2)
        Assert.Equal(0xE4, interruptController.IF); // 0x04 (Timer) | 0xE0 (upper bits)
    }

    [Fact]
    public void Joypad_ButtonPress_RequestsJoypadInterrupt()
    {
        var interruptController = new InterruptController();
        interruptController.SetIF(0x00); // Clear all interrupt flags
        var joypad = new Joypad(interruptController);

        // Press A button (false -> true transition)
        joypad.A = true;

        // Verify Joypad interrupt was requested (bit 4)
        Assert.Equal(0xF0, interruptController.IF); // 0x10 (Joypad) | 0xE0 (upper bits)
    }

    [Fact]
    public void Joypad_MultipleButtons_EachRequestsJoypadInterrupt()
    {
        var interruptController = new InterruptController();
        var joypad = new Joypad(interruptController);

        // Test each button individually
        var buttons = new[]
        {
            () => { interruptController.SetIF(0x00); joypad.Up = true; },
            () => { interruptController.SetIF(0x00); joypad.Down = true; },
            () => { interruptController.SetIF(0x00); joypad.Left = true; },
            () => { interruptController.SetIF(0x00); joypad.Right = true; },
            () => { interruptController.SetIF(0x00); joypad.A = true; },
            () => { interruptController.SetIF(0x00); joypad.B = true; },
            () => { interruptController.SetIF(0x00); joypad.Select = true; },
            () => { interruptController.SetIF(0x00); joypad.Start = true; }
        };

        foreach (var pressButton in buttons)
        {
            pressButton();
            Assert.Equal(0xF0, interruptController.IF); // 0x10 (Joypad) | 0xE0 (upper bits)
        }
    }

    [Fact]
    public void Joypad_ButtonRelease_DoesNotRequestInterrupt()
    {
        var interruptController = new InterruptController();
        interruptController.SetIF(0x00); // Clear all interrupt flags
        var joypad = new Joypad(interruptController);

        // Press and then release A button
        joypad.A = true; // This should trigger interrupt
        interruptController.SetIF(0x00); // Clear flag
        joypad.A = false; // This should NOT trigger interrupt

        // Verify no interrupt was requested on button release
        Assert.Equal(0xE0, interruptController.IF); // Only upper bits
    }

    [Fact]
    public void Serial_StartTransfer_RequestsSerialInterrupt()
    {
        var interruptController = new InterruptController();
        interruptController.SetIF(0x00); // Clear all interrupt flags
        var serial = new Serial(interruptController);

        // Start a transfer and step until completion (512 cycles)
        serial.StartTransfer();
        serial.Step(512);

        // Verify Serial interrupt was requested (bit 3)
        Assert.Equal(0xE8, interruptController.IF); // 0x08 (Serial) | 0xE0 (upper bits)
    }

    [Fact]
    public void Serial_StepWithoutTransfer_DoesNotRequestInterrupt()
    {
        var interruptController = new InterruptController();
        interruptController.SetIF(0x00); // Clear all interrupt flags
        var serial = new Serial(interruptController);

        // Step without starting a transfer
        serial.Step(512);

        // Verify no interrupt was requested
        Assert.Equal(0xE0, interruptController.IF); // Only upper bits
    }

    [Fact]
    public void Emulator_IntegratesAllInterruptSources()
    {
        var emulator = new Emulator();
        emulator.InterruptController.SetIF(0x00); // Clear all interrupt flags

        // Test that emulator properly connects all interrupt sources
        Assert.NotNull(emulator.Ppu);
        Assert.NotNull(emulator.Joypad);
        Assert.NotNull(emulator.Serial);
        Assert.NotNull(emulator.InterruptController);

        // Verify joypad can trigger interrupt through emulator
        emulator.Joypad.Start = true;
        Assert.Equal(0xF0, emulator.InterruptController.IF); // 0x10 (Joypad) | 0xE0 (upper bits)
    }

    [Fact]
    public void Timer_MultipleSteps_TriggersMultipleInterrupts()
    {
        var interruptController = new InterruptController();
        interruptController.SetIF(0x00); // Clear all interrupt flags
        var timer = new GameBoy.Core.Timer(interruptController);

        // Step exactly enough to trigger first interrupt
        timer.Step(1024);
        Assert.Equal(0xE4, interruptController.IF); // First interrupt

        // Clear and step again
        interruptController.SetIF(0x00);
        timer.Step(1024);
        Assert.Equal(0xE4, interruptController.IF); // Second interrupt
    }

    [Fact]
    public void SubsystemInterrupts_OnlyTriggerUnderCorrectConditions()
    {
        var interruptController = new InterruptController();

        // Test Timer - should not trigger without proper setup
        var timer = new GameBoy.Core.Timer(interruptController);
        interruptController.SetIF(0x00);
        timer.Step(1); // Single step shouldn't trigger
        Assert.Equal(0xE0, interruptController.IF); // No timer interrupt

        // Test Joypad - should only trigger on button press (false->true transition)
        var joypad = new Joypad(interruptController);
        interruptController.SetIF(0x00);
        joypad.A = false; // Keep button released
        Assert.Equal(0xE0, interruptController.IF); // No interrupt

        joypad.A = true; // Press button (triggers interrupt)
        Assert.Equal(0xF0, interruptController.IF); // Joypad interrupt

        // Additional press should not trigger again
        interruptController.SetIF(0x00);
        joypad.A = true; // Already pressed, no new transition
        Assert.Equal(0xE0, interruptController.IF); // No additional interrupt
    }

    [Fact]
    public void ComplexEmulatorScenario_MultipleInterruptSources()
    {
        var emulator = new Emulator();
        
        // Clear all interrupts and enable specific ones
        emulator.Mmu.InterruptController.SetIF(0x00);
        emulator.Mmu.InterruptController.SetIE(0x15); // Enable VBlank, Timer, Joypad

        // Simulate a frame step that should trigger VBlank
        for (int cycles = 0; cycles < 70224; cycles += 4)
        {
            emulator.StepFrame(); // Step emulator to complete a frame
            if ((emulator.Mmu.InterruptController.IF & 0x01) != 0) break; // VBlank triggered
        }

        // VBlank should have been triggered
        Assert.True((emulator.Mmu.InterruptController.IF & 0x01) != 0, "VBlank interrupt should be triggered");

        // Test that multiple interrupts can be pending simultaneously
        emulator.Mmu.InterruptController.Request(InterruptType.Timer);
        emulator.Mmu.InterruptController.Request(InterruptType.Joypad);

        // Check that all requested interrupts are pending
        byte expectedIF = 0x01 | 0x04 | 0x10; // VBlank + Timer + Joypad
        Assert.True((emulator.Mmu.InterruptController.IF & expectedIF) == expectedIF,
            "Multiple interrupts should be pending simultaneously");
    }

    [Fact]
    public void InterruptPriorityIntegration_EmulatorLevel()
    {
        var emulator = new Emulator();

        // Setup CPU state for interrupt testing
        emulator.Cpu.Regs.PC = 0x1000;
        emulator.Cpu.InterruptsEnabled = true;

        // Request multiple interrupts in reverse priority order
        emulator.Mmu.InterruptController.SetIF(0x00);
        emulator.Mmu.InterruptController.Request(InterruptType.Joypad);  // Lowest
        emulator.Mmu.InterruptController.Request(InterruptType.Serial);  // Low
        emulator.Mmu.InterruptController.Request(InterruptType.Timer);   // Medium
        emulator.Mmu.InterruptController.Request(InterruptType.LCDStat); // High
        emulator.Mmu.InterruptController.Request(InterruptType.VBlank);  // Highest
        emulator.Mmu.InterruptController.SetIE(0x1F); // Enable all

        // Step emulator - should service VBlank first
        emulator.StepFrame();
        Assert.Equal(0x0040, emulator.Cpu.Regs.PC); // VBlank vector

        // Check that VBlank interrupt was cleared but others remain
        Assert.Equal(0xFE, emulator.Mmu.InterruptController.IF); // All except VBlank (0x01)
    }

    [Fact]
    public void HALTIntegration_EmulatorWithSubsystems()
    {
        var emulator = new Emulator();

        // Setup HALT instruction
        emulator.Mmu.WriteByte(0x1000, 0x76); // HALT at 0x1000
        emulator.Cpu.Regs.PC = 0x1000;
        emulator.Cpu.InterruptsEnabled = true;

        // Execute HALT
        emulator.StepFrame();
        Assert.True(emulator.Cpu.IsHalted);

        // Clear all interrupts initially
        emulator.Mmu.InterruptController.SetIF(0x00);
        emulator.Mmu.InterruptController.SetIE(0x01); // Enable VBlank

        // Step emulator until VBlank is triggered by PPU
        for (int i = 0; i < 1000 && emulator.Cpu.IsHalted; i++)
        {
            emulator.StepFrame(); // Step until PPU triggers VBlank
        }

        // CPU should have woken up and serviced the VBlank interrupt
        Assert.False(emulator.Cpu.IsHalted);
        Assert.Equal(0x0040, emulator.Cpu.Regs.PC); // Should be at VBlank vector
    }
}