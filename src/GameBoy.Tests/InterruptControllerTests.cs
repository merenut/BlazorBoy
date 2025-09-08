using GameBoy.Core;

namespace GameBoy.Tests;

/// <summary>
/// Tests for the InterruptController class to validate core interrupt logic.
/// </summary>
public class InterruptControllerTests
{
    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        var controller = new InterruptController();

        // Default values should match post-BIOS initialization
        Assert.Equal(0xE1, controller.IF); // 0x01 with upper 3 bits set to 1
        Assert.Equal(0x00, controller.IE);
    }

    [Fact]
    public void InitializePostBiosDefaults_SetsCorrectValues()
    {
        var controller = new InterruptController();

        // Change values first
        controller.SetIF(0x1F);
        controller.SetIE(0xFF);

        // Reset to defaults
        controller.InitializePostBiosDefaults();

        Assert.Equal(0xE1, controller.IF); // 0x01 with upper 3 bits set to 1
        Assert.Equal(0x00, controller.IE);
    }

    [Theory]
    [InlineData(0x00, 0xE0)] // All bits clear should read as 0xE0 (upper 3 bits forced)
    [InlineData(0x01, 0xE1)] // VBlank bit set
    [InlineData(0x02, 0xE2)] // LCD STAT bit set
    [InlineData(0x04, 0xE4)] // Timer bit set
    [InlineData(0x08, 0xE8)] // Serial bit set
    [InlineData(0x10, 0xF0)] // Joypad bit set
    [InlineData(0x1F, 0xFF)] // All lower 5 bits set
    [InlineData(0xFF, 0xFF)] // All bits set should mask to lower 5 bits
    public void IF_SetAndRead_ReturnsCorrectValues(byte setValue, byte expectedRead)
    {
        var controller = new InterruptController();
        controller.SetIF(setValue);
        Assert.Equal(expectedRead, controller.IF);
    }

    [Theory]
    [InlineData(0x00)]
    [InlineData(0x01)]
    [InlineData(0x42)]
    [InlineData(0xFF)]
    public void IE_SetAndRead_ReturnsExactValue(byte value)
    {
        var controller = new InterruptController();
        controller.SetIE(value);
        Assert.Equal(value, controller.IE);
    }

    [Theory]
    [InlineData(InterruptType.VBlank, 0x01)]
    [InlineData(InterruptType.LCDStat, 0x02)]
    [InlineData(InterruptType.Timer, 0x04)]
    [InlineData(InterruptType.Serial, 0x08)]
    [InlineData(InterruptType.Joypad, 0x10)]
    public void Request_SetsCorrectIFBit(InterruptType interruptType, byte expectedBit)
    {
        var controller = new InterruptController();
        controller.SetIF(0x00); // Clear all bits

        controller.Request(interruptType);

        Assert.Equal(expectedBit | 0xE0, controller.IF); // Expected bit + forced upper 3 bits
    }

    [Fact]
    public void Request_MultipleInterrupts_SetsMultipleBits()
    {
        var controller = new InterruptController();
        controller.SetIF(0x00); // Clear all bits

        controller.Request(InterruptType.VBlank);
        controller.Request(InterruptType.Timer);
        controller.Request(InterruptType.Joypad);

        byte expected = 0x01 | 0x04 | 0x10; // VBlank + Timer + Joypad bits
        Assert.Equal(expected | 0xE0, controller.IF); // Expected bits + forced upper 3 bits
    }

    [Fact]
    public void TryGetPending_NoInterruptsEnabled_ReturnsFalse()
    {
        var controller = new InterruptController();
        controller.SetIF(0x1F); // Set all interrupt flags
        controller.SetIE(0x00); // Disable all interrupts

        bool hasPending = controller.TryGetPending(out InterruptType interruptType);

        Assert.False(hasPending);
        Assert.Equal(default(InterruptType), interruptType);
    }

    [Fact]
    public void TryGetPending_NoInterruptsRequested_ReturnsFalse()
    {
        var controller = new InterruptController();
        controller.SetIF(0x00); // Clear all interrupt flags
        controller.SetIE(0x1F); // Enable all interrupts

        bool hasPending = controller.TryGetPending(out InterruptType interruptType);

        Assert.False(hasPending);
        Assert.Equal(default(InterruptType), interruptType);
    }

    [Theory]
    [InlineData(0x01, 0x01, InterruptType.VBlank)]   // VBlank requested and enabled
    [InlineData(0x02, 0x02, InterruptType.LCDStat)] // LCD STAT requested and enabled
    [InlineData(0x04, 0x04, InterruptType.Timer)]   // Timer requested and enabled
    [InlineData(0x08, 0x08, InterruptType.Serial)]  // Serial requested and enabled
    [InlineData(0x10, 0x10, InterruptType.Joypad)]  // Joypad requested and enabled
    public void TryGetPending_SingleInterrupt_ReturnsCorrectType(byte ifValue, byte ieValue, InterruptType expectedType)
    {
        var controller = new InterruptController();
        controller.SetIF(ifValue);
        controller.SetIE(ieValue);

        bool hasPending = controller.TryGetPending(out InterruptType interruptType);

        Assert.True(hasPending);
        Assert.Equal(expectedType, interruptType);
    }

    [Fact]
    public void TryGetPending_MultipleInterrupts_ReturnsHighestPriority()
    {
        var controller = new InterruptController();

        // Test VBlank has highest priority
        controller.SetIF(0x1F); // All interrupts requested
        controller.SetIE(0x1F); // All interrupts enabled

        bool hasPending = controller.TryGetPending(out InterruptType interruptType);

        Assert.True(hasPending);
        Assert.Equal(InterruptType.VBlank, interruptType);

        // Test LCD STAT is next highest when VBlank is not pending
        controller.SetIF(0x1E); // All except VBlank
        controller.SetIE(0x1F); // All enabled

        hasPending = controller.TryGetPending(out interruptType);

        Assert.True(hasPending);
        Assert.Equal(InterruptType.LCDStat, interruptType);

        // Test Timer is next
        controller.SetIF(0x1C); // Timer, Serial, Joypad
        controller.SetIE(0x1F); // All enabled

        hasPending = controller.TryGetPending(out interruptType);

        Assert.True(hasPending);
        Assert.Equal(InterruptType.Timer, interruptType);

        // Test Serial is next
        controller.SetIF(0x18); // Serial, Joypad
        controller.SetIE(0x1F); // All enabled

        hasPending = controller.TryGetPending(out interruptType);

        Assert.True(hasPending);
        Assert.Equal(InterruptType.Serial, interruptType);

        // Test Joypad is lowest priority
        controller.SetIF(0x10); // Only Joypad
        controller.SetIE(0x1F); // All enabled

        hasPending = controller.TryGetPending(out interruptType);

        Assert.True(hasPending);
        Assert.Equal(InterruptType.Joypad, interruptType);
    }

    [Theory]
    [InlineData(InterruptType.VBlank, 0x0040)]
    [InlineData(InterruptType.LCDStat, 0x0048)]
    [InlineData(InterruptType.Timer, 0x0050)]
    [InlineData(InterruptType.Serial, 0x0058)]
    [InlineData(InterruptType.Joypad, 0x0060)]
    public void Service_ReturnsCorrectVectorAddress(InterruptType interruptType, ushort expectedVector)
    {
        var controller = new InterruptController();

        ushort vectorAddress = controller.Service(interruptType);

        Assert.Equal(expectedVector, vectorAddress);
    }

    [Theory]
    [InlineData(InterruptType.VBlank, 0x01)]
    [InlineData(InterruptType.LCDStat, 0x02)]
    [InlineData(InterruptType.Timer, 0x04)]
    [InlineData(InterruptType.Serial, 0x08)]
    [InlineData(InterruptType.Joypad, 0x10)]
    public void Service_ClearsCorrectIFBit(InterruptType interruptType, byte bitMask)
    {
        var controller = new InterruptController();
        controller.SetIF(0x1F); // Set all interrupt flags

        controller.Service(interruptType);

        byte expectedIF = (byte)((0x1F & ~bitMask) | 0xE0); // Clear specific bit, preserve upper 3 bits
        Assert.Equal(expectedIF, controller.IF);
    }

    [Fact]
    public void Service_MultipleInterrupts_ClearsOnlySpecifiedBit()
    {
        var controller = new InterruptController();
        controller.SetIF(0x1F); // Set all interrupt flags

        // Service VBlank interrupt
        controller.Service(InterruptType.VBlank);

        // Should clear VBlank bit but leave others
        byte expectedIF = (byte)((0x1F & ~0x01) | 0xE0); // Clear VBlank bit
        Assert.Equal(expectedIF, controller.IF);

        // Other interrupts should still be pending if enabled
        controller.SetIE(0x1E); // Enable all except VBlank
        bool hasPending = controller.TryGetPending(out InterruptType nextInterrupt);

        Assert.True(hasPending);
        Assert.Equal(InterruptType.LCDStat, nextInterrupt); // Next highest priority
    }

    [Fact]
    public void Service_UnknownInterruptType_ThrowsArgumentException()
    {
        var controller = new InterruptController();

        Assert.Throws<ArgumentException>(() => controller.Service((InterruptType)255));
    }

    [Fact]
    public void DocumentedApiExamples_WorkCorrectly()
    {
        var interruptController = new InterruptController();
        interruptController.SetIF(0x00); // Clear all bits

        // Test all documented API usage examples work correctly:

        // PPU: Request VBlank interrupt at end of frame
        interruptController.Request(InterruptType.VBlank);
        Assert.Equal(0xE1, interruptController.IF); // 0x01 | 0xE0

        // PPU: Request LCD STAT interrupt on mode changes  
        interruptController.Request(InterruptType.LCDStat);
        Assert.Equal(0xE3, interruptController.IF); // 0x03 | 0xE0

        // Timer: Request Timer interrupt on TIMA overflow
        interruptController.Request(InterruptType.Timer);
        Assert.Equal(0xE7, interruptController.IF); // 0x07 | 0xE0

        // Serial: Request Serial interrupt on transfer complete
        interruptController.Request(InterruptType.Serial);
        Assert.Equal(0xEF, interruptController.IF); // 0x0F | 0xE0

        // Joypad: Request Joypad interrupt on button press
        interruptController.Request(InterruptType.Joypad);
        Assert.Equal(0xFF, interruptController.IF); // 0x1F | 0xE0

        // Verify all 5 interrupt sources are properly set
        Assert.True((interruptController.IF & 0x1F) == 0x1F, "All 5 interrupt bits should be set");
    }
}