using GameBoy.Core;
using Xunit;

namespace GameBoy.Tests;

/// <summary>
/// Tests for the PPU (Picture Processing Unit) functionality including LCD modes, 
/// rendering pipeline, and register coordination.
/// </summary>
public class PpuTests
{
    [Fact]
    public void Constructor_InitializesWithCorrectDefaults()
    {
        // Arrange
        var interruptController = new InterruptController();

        // Act
        var ppu = new Ppu(interruptController);

        // Assert
        Assert.Equal(LcdMode.OamScan, ppu.Mode);
        Assert.Equal(0, ppu.LY);
        Assert.Equal(0x85, ppu.STAT);
        Assert.NotNull(ppu.FrameBuffer);
        Assert.Equal(Ppu.ScreenWidth * Ppu.ScreenHeight, ppu.FrameBuffer.Length);
    }

    [Fact]
    public void Reset_RestoresDefaultState()
    {
        // Arrange
        var interruptController = new InterruptController();
        var ppu = new Ppu(interruptController);

        // Simulate some state changes
        ppu.Step(1000); // Advance some cycles

        // Act
        ppu.Reset();

        // Assert
        Assert.Equal(LcdMode.OamScan, ppu.Mode);
        Assert.Equal(0, ppu.LY);
        Assert.Equal(0x85, ppu.STAT);
    }

    [Fact]
    public void Step_WithLcdDisabled_ReturnsWithoutProcessing()
    {
        // Arrange
        var interruptController = new InterruptController();
        var ppu = new Ppu(interruptController);
        var mmu = new Mmu();

        // Disable LCD by setting LCDC bit 7 to 0
        mmu.WriteByte(IoRegs.LCDC, 0x00); // LCD disabled
        ppu.Mmu = mmu;

        // Act
        bool frameCompleted = ppu.Step(1000);

        // Assert
        Assert.False(frameCompleted);
        Assert.Equal(0, ppu.LY); // Should not advance
        Assert.Equal(LcdMode.OamScan, ppu.Mode); // Should not change
    }

    [Fact]
    public void Step_CompletesFrameAfter70224Cycles()
    {
        // Arrange
        var interruptController = new InterruptController();
        interruptController.SetIF(0x00);
        var ppu = new Ppu(interruptController);

        // Act
        bool frameCompleted = ppu.Step(70224);

        // Assert
        Assert.True(frameCompleted);
        // VBlank interrupt should be requested (bit 0 set)
        Assert.Equal(0xE1, interruptController.IF);
    }

    [Fact]
    public void Step_AdvancesThroughLcdModes()
    {
        // Arrange
        var interruptController = new InterruptController();
        var ppu = new Ppu(interruptController);

        // Act & Assert - OAM Scan -> Drawing
        Assert.Equal(LcdMode.OamScan, ppu.Mode);
        ppu.Step(80); // Complete OAM scan (80 cycles)
        Assert.Equal(LcdMode.Drawing, ppu.Mode);

        // Drawing -> H-Blank
        ppu.Step(172); // Complete drawing (172 cycles minimum)
        Assert.Equal(LcdMode.HBlank, ppu.Mode);

        // H-Blank -> next scanline OAM Scan
        ppu.Step(204); // Complete H-Blank (204 cycles)
        Assert.Equal(LcdMode.OamScan, ppu.Mode);
        Assert.Equal(1, ppu.LY); // Should advance to next scanline
    }

    [Fact]
    public void Step_TriggersVBlankAtScanline144()
    {
        // Arrange
        var interruptController = new InterruptController();
        interruptController.SetIF(0x00);
        var ppu = new Ppu(interruptController);

        // Act - Step through 144 scanlines (144 * 456 = 65664 cycles)
        ppu.Step(65664);

        // Assert
        Assert.Equal(LcdMode.VBlank, ppu.Mode);
        Assert.Equal(144, ppu.LY);
        Assert.True((interruptController.IF & 0x01) != 0); // VBlank interrupt requested
    }

    [Fact]
    public void LY_AdvancesCorrectlyThroughFrame()
    {
        // Arrange
        var interruptController = new InterruptController();
        var ppu = new Ppu(interruptController);

        // Act & Assert - Test scanline progression
        Assert.Equal(0, ppu.LY);

        // Complete first scanline (456 cycles)
        ppu.Step(456);
        Assert.Equal(1, ppu.LY);

        // Complete several more scanlines
        for (int i = 2; i <= 143; i++)
        {
            ppu.Step(456);
            Assert.Equal(i, ppu.LY);
        }

        // Enter VBlank
        ppu.Step(456);
        Assert.Equal(144, ppu.LY);
        Assert.Equal(LcdMode.VBlank, ppu.Mode);
    }

    [Fact]
    public void FrameBuffer_InitializedToCorrectSize()
    {
        // Arrange
        var interruptController = new InterruptController();
        var ppu = new Ppu(interruptController);

        // Assert
        Assert.Equal(160 * 144, ppu.FrameBuffer.Length);
        // Frame buffer should be initialized with default background color (opaque pixels)
        int expectedDefaultColor = Palette.ToRgba(0); // Lightest green with full opacity
        Assert.All(ppu.FrameBuffer, pixel => Assert.Equal(expectedDefaultColor, pixel));
    }

    [Fact]
    public void Step_WithMmuIntegration_UpdatesRegistersCorrectly()
    {
        // Arrange
        var interruptController = new InterruptController();
        var ppu = new Ppu(interruptController);
        var mmu = new Mmu();
        mmu.Ppu = ppu;
        ppu.Mmu = mmu;

        // Set up test register values
        mmu.WriteByte(IoRegs.SCX, 0x10);
        mmu.WriteByte(IoRegs.SCY, 0x20);
        mmu.WriteByte(IoRegs.BGP, 0xE4);

        // Act
        ppu.Step(100);

        // Assert - PPU should read these values during rendering
        Assert.Equal(0x10, mmu.ReadByte(IoRegs.SCX));
        Assert.Equal(0x20, mmu.ReadByte(IoRegs.SCY));
        Assert.Equal(0xE4, mmu.ReadByte(IoRegs.BGP));
    }

    [Fact]
    public void STAT_Register_ReflectsCurrentMode()
    {
        // Arrange
        var interruptController = new InterruptController();
        var ppu = new Ppu(interruptController);

        // Act & Assert - Check mode transitions and STAT register updates
        Assert.Equal(LcdMode.OamScan, ppu.Mode);

        ppu.Step(80); // Complete OAM scan -> Drawing mode
        Assert.Equal(LcdMode.Drawing, ppu.Mode);
        Assert.Equal((byte)LcdMode.Drawing, (byte)(ppu.STAT & 0x03));

        ppu.Step(172); // Complete drawing -> H-Blank mode
        Assert.Equal(LcdMode.HBlank, ppu.Mode);
        Assert.Equal((byte)LcdMode.HBlank, (byte)(ppu.STAT & 0x03));
    }

    [Fact]
    public void VBlankPeriod_Lasts10Scanlines()
    {
        // Arrange
        var interruptController = new InterruptController();
        var ppu = new Ppu(interruptController);

        // Act - Step to VBlank start (144 scanlines)
        ppu.Step(144 * 456);
        Assert.Equal(LcdMode.VBlank, ppu.Mode);
        Assert.Equal(144, ppu.LY);

        // Step through VBlank period (10 scanlines)
        for (int i = 144; i < 154; i++)
        {
            Assert.Equal(LcdMode.VBlank, ppu.Mode);
            ppu.Step(456);
            if (i < 153)
            {
                Assert.Equal(i + 1, ppu.LY);
            }
        }

        // Should wrap back to start of next frame
        Assert.Equal(0, ppu.LY);
        Assert.Equal(LcdMode.OamScan, ppu.Mode);
    }

    [Theory]
    [InlineData(70224, true)]   // Exactly one frame
    [InlineData(35112, false)]  // Half frame
    [InlineData(140448, true)]  // Two frames
    public void Step_FrameCompletion_ReturnsCorrectValue(int cycles, bool expectedFrameReady)
    {
        // Arrange
        var interruptController = new InterruptController();
        var ppu = new Ppu(interruptController);

        // Act
        bool actualFrameReady = ppu.Step(cycles);

        // Assert
        Assert.Equal(expectedFrameReady, actualFrameReady);
    }
}