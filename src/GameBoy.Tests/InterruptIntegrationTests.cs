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

        // Step the PPU - should request VBlank interrupt
        ppu.Step(4);

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
}