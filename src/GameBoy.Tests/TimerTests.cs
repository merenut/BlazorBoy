using GameBoy.Core;

namespace GameBoy.Tests;

public class TimerTests
{
    [Fact]
    public void Constructor_WithNullInterruptController_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GameBoy.Core.Timer(null!));
    }

    [Fact]
    public void Constructor_WithValidInterruptController_Initializes()
    {
        var interruptController = new InterruptController();
        var timer = new GameBoy.Core.Timer(interruptController);
        
        Assert.Equal(0x00, timer.DIV);
        Assert.Equal(0x00, timer.TIMA);
        Assert.Equal(0x00, timer.TMA);
        Assert.Equal(0xF8, timer.TAC);
    }

    [Fact]
    public void Reset_RestoresDefaultValues()
    {
        var interruptController = new InterruptController();
        var timer = new GameBoy.Core.Timer(interruptController);
        
        timer.SetTIMA(0x50);
        timer.SetTMA(0x40);
        timer.SetTAC(0x05);
        timer.Step(1000); // Advance counter
        
        timer.Reset();
        
        Assert.Equal(0x00, timer.DIV);
        Assert.Equal(0x00, timer.TIMA);
        Assert.Equal(0x00, timer.TMA);
        Assert.Equal(0xF8, timer.TAC);
    }

    [Fact]
    public void DIV_IncrementsEvery256Cycles()
    {
        var interruptController = new InterruptController();
        var timer = new GameBoy.Core.Timer(interruptController);
        
        Assert.Equal(0x00, timer.DIV);
        
        timer.Step(255);
        Assert.Equal(0x00, timer.DIV);
        
        timer.Step(1);
        Assert.Equal(0x01, timer.DIV);
        
        timer.Step(256);
        Assert.Equal(0x02, timer.DIV);
    }

    [Fact]
    public void DIV_OverflowsCorrectly()
    {
        var interruptController = new InterruptController();
        var timer = new GameBoy.Core.Timer(interruptController);
        
        // Advance to near overflow
        timer.Step(256 * 255); // DIV = 0xFF
        Assert.Equal(0xFF, timer.DIV);
        
        timer.Step(256);
        Assert.Equal(0x00, timer.DIV); // Wrapped around
    }

    [Fact]
    public void ResetDivider_ResetsInternalCounter()
    {
        var interruptController = new InterruptController();
        var timer = new GameBoy.Core.Timer(interruptController);
        
        timer.Step(512); // DIV = 2
        Assert.Equal(0x02, timer.DIV);
        
        timer.ResetDivider();
        Assert.Equal(0x00, timer.DIV);
        
        timer.Step(255);
        Assert.Equal(0x00, timer.DIV); // Still zero until 256 cycles
        
        timer.Step(1);
        Assert.Equal(0x01, timer.DIV); // Now increments
    }

    [Fact]
    public void TIMA_DoesNotIncrementWhenTimerDisabled()
    {
        var interruptController = new InterruptController();
        var timer = new GameBoy.Core.Timer(interruptController);
        
        timer.SetTAC(0x00); // Timer disabled (bit 2 = 0)
        timer.Step(2000);
        
        Assert.Equal(0x00, timer.TIMA);
    }

    [Theory]
    [InlineData(0x04, 1024)] // TAC = 00: 4096 Hz (1024 cycles)
    [InlineData(0x05, 16)]   // TAC = 01: 262144 Hz (16 cycles)
    [InlineData(0x06, 64)]   // TAC = 10: 65536 Hz (64 cycles)
    [InlineData(0x07, 256)]  // TAC = 11: 16384 Hz (256 cycles)
    public void TIMA_IncrementsAtCorrectFrequency(byte tacValue, int expectedCycles)
    {
        var interruptController = new InterruptController();
        var timer = new GameBoy.Core.Timer(interruptController);
        
        timer.SetTAC(tacValue); // Timer enabled with specific frequency
        
        Assert.Equal(0x00, timer.TIMA);
        
        timer.Step(expectedCycles - 1);
        Assert.Equal(0x00, timer.TIMA); // Should not increment yet
        
        timer.Step(1);
        Assert.Equal(0x01, timer.TIMA); // Now increments
        
        timer.Step(expectedCycles);
        Assert.Equal(0x02, timer.TIMA); // Increments again
    }

    [Fact]
    public void TIMA_OverflowReloadsFromTMAAndRequestsInterrupt()
    {
        var interruptController = new InterruptController();
        var timer = new GameBoy.Core.Timer(interruptController);
        
        timer.SetTAC(0x05); // Timer enabled, fastest frequency (16 cycles)
        timer.SetTMA(0x40); // Reload value
        timer.SetTIMA(0xFE); // Close to overflow
        
        // Clear any existing interrupts
        interruptController.SetIF(0x00);
        
        timer.Step(16); // Should increment TIMA to 0xFF
        Assert.Equal(0xFF, timer.TIMA);
        Assert.Equal(0xE0, interruptController.IF); // No interrupt yet (upper bits always set)
        
        timer.Step(16); // Should overflow TIMA and reload from TMA
        Assert.Equal(0x40, timer.TIMA); // Reloaded from TMA
        Assert.Equal((byte)(0xE0 | (1 << (int)InterruptType.Timer)), interruptController.IF); // Timer interrupt requested
    }

    [Fact]
    public void SetTIMA_UpdatesRegisterValue()
    {
        var interruptController = new InterruptController();
        var timer = new GameBoy.Core.Timer(interruptController);
        
        timer.SetTIMA(0x55);
        Assert.Equal(0x55, timer.TIMA);
    }

    [Fact]
    public void SetTMA_UpdatesRegisterValue()
    {
        var interruptController = new InterruptController();
        var timer = new GameBoy.Core.Timer(interruptController);
        
        timer.SetTMA(0xAA);
        Assert.Equal(0xAA, timer.TMA);
    }

    [Fact]
    public void SetTAC_UpdatesRegisterAndResetsTIMACounter()
    {
        var interruptController = new InterruptController();
        var timer = new GameBoy.Core.Timer(interruptController);
        
        timer.SetTAC(0x05); // Fast frequency
        timer.Step(10); // Partial way to next TIMA increment
        
        timer.SetTAC(0x04); // Change to slow frequency - should reset internal counter
        timer.Step(1014); // Almost a full slow cycle, should not increment yet
        Assert.Equal(0x00, timer.TIMA);
        
        timer.Step(10); // Complete the slow cycle
        Assert.Equal(0x01, timer.TIMA);
    }

    [Fact]
    public void TIMA_ContinuesAfterTACFrequencyChange()
    {
        var interruptController = new InterruptController();
        var timer = new GameBoy.Core.Timer(interruptController);
        
        timer.SetTAC(0x05); // Fast frequency (16 cycles)
        timer.Step(16);
        Assert.Equal(0x01, timer.TIMA);
        
        timer.SetTAC(0x04); // Slow frequency (1024 cycles)
        timer.Step(1024);
        Assert.Equal(0x02, timer.TIMA);
    }

    [Fact]
    public void TIMA_OverflowWithZeroTMAReloadsToZero()
    {
        var interruptController = new InterruptController();
        var timer = new GameBoy.Core.Timer(interruptController);
        
        timer.SetTAC(0x05); // Fast frequency
        timer.SetTMA(0x00); // Reload to 0
        timer.SetTIMA(0xFF);
        
        interruptController.SetIF(0x00);
        
        timer.Step(16); // Should overflow and reload to 0
        Assert.Equal(0x00, timer.TIMA);
        Assert.Equal((byte)(0xE0 | (1 << (int)InterruptType.Timer)), interruptController.IF);
    }

    [Fact]
    public void StepWithLargeCycles_ProcessesMultipleTIMAIncrements()
    {
        var interruptController = new InterruptController();
        var timer = new GameBoy.Core.Timer(interruptController);
        
        timer.SetTAC(0x05); // 16 cycles per increment
        
        timer.Step(48); // Should increment TIMA 3 times
        Assert.Equal(0x03, timer.TIMA);
    }

    [Fact]
    public void StepWithLargeCycles_ProcessesMultipleOverflows()
    {
        var interruptController = new InterruptController();
        var timer = new GameBoy.Core.Timer(interruptController);
        
        timer.SetTAC(0x05); // 16 cycles per increment
        timer.SetTMA(0x80);
        timer.SetTIMA(0xFE); // Will overflow after 1 increment, then again after 128 more
        
        interruptController.SetIF(0x00);
        
        timer.Step(16 * 130); // Should overflow twice
        
        // First overflow: 0xFE -> 0xFF -> 0x80
        // Then 128 more increments: 0x80 + 128 = 0x100 -> overflow -> 0x80
        Assert.Equal(0x80, timer.TIMA);
        Assert.Equal((byte)(0xE0 | (1 << (int)InterruptType.Timer)), interruptController.IF); // At least one interrupt requested
    }
}
