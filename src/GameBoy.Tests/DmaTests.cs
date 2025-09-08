using Xunit;
using GameBoy.Core;

namespace GameBoy.Tests;

/// <summary>
/// Tests for the DMA (Direct Memory Access) transfer functionality.
/// Validates that writing to the DMA register triggers correct transfer behavior,
/// timing, and source address validation.
/// </summary>
public class DmaTests
{
    [Fact]
    public void DMA_WriteToRegister_StoresValue()
    {
        var mmu = new Mmu();

        mmu.WriteByte(Mmu.DMA, 0x80);
        Assert.Equal(0x80, mmu.ReadByte(Mmu.DMA));

        mmu.WriteByte(Mmu.DMA, 0xC0);
        Assert.Equal(0xC0, mmu.ReadByte(Mmu.DMA));
    }

    [Theory]
    [InlineData(0x80, 0x8000)] // VRAM source
    [InlineData(0xC0, 0xC000)] // Work RAM source  
    [InlineData(0xD0, 0xD000)] // Work RAM source
    public void DMA_ValidSources_CopiesCorrectly(byte dmaValue, ushort expectedSource)
    {
        var mmu = new Mmu();

        // Set up test data in source region
        for (int i = 0; i < 160; i++)
        {
            mmu.WriteByte((ushort)(expectedSource + i), (byte)(i & 0xFF));
        }

        // Clear OAM to ensure we're testing the transfer
        for (int i = 0; i < 160; i++)
        {
            mmu.WriteByte((ushort)(Mmu.OamStart + i), 0x00);
        }

        // Trigger DMA transfer
        mmu.WriteByte(Mmu.DMA, dmaValue);

        // Verify data was copied to OAM
        for (int i = 0; i < 160; i++)
        {
            byte expected = (byte)(i & 0xFF);
            byte actual = mmu.ReadByte((ushort)(Mmu.OamStart + i));
            Assert.Equal(expected, actual);
        }
    }

    [Theory]
    [InlineData(0xE0)] // Echo RAM - invalid
    [InlineData(0xFF)] // I/O region - invalid
    [InlineData(0xFE)] // Unusable/OAM region - invalid
    public void DMA_InvalidSources_HandledGracefully(byte dmaValue)
    {
        var mmu = new Mmu();

        // Set up known pattern in OAM
        for (int i = 0; i < 160; i++)
        {
            mmu.WriteByte((ushort)(Mmu.OamStart + i), 0xAA);
        }

        // Trigger DMA transfer with invalid source
        mmu.WriteByte(Mmu.DMA, dmaValue);

        // OAM should remain unchanged
        for (int i = 0; i < 160; i++)
        {
            Assert.Equal(0xAA, mmu.ReadByte((ushort)(Mmu.OamStart + i)));
        }

        // DMA should not be active for invalid transfers
        Assert.False(mmu.IsDmaActive);
    }

    [Fact]
    public void DMA_Transfer_Copies160BytesToOAM()
    {
        var mmu = new Mmu();

        // Set up test pattern in VRAM (0x8000)
        ushort sourceAddr = 0x8000;
        for (int i = 0; i < 160; i++)
        {
            mmu.WriteByte((ushort)(sourceAddr + i), (byte)(0x55 + i));
        }

        // Clear OAM
        for (int i = 0; i < 160; i++)
        {
            mmu.WriteByte((ushort)(Mmu.OamStart + i), 0x00);
        }

        // Trigger DMA transfer
        mmu.WriteByte(Mmu.DMA, 0x80); // Source = 0x8000

        // Verify exactly 160 bytes were copied
        for (int i = 0; i < 160; i++)
        {
            byte expected = (byte)(0x55 + i);
            byte actual = mmu.ReadByte((ushort)(Mmu.OamStart + i));
            Assert.Equal(expected, actual);
        }

        // Verify no data beyond 160 bytes was affected
        Assert.Equal(0xFF, mmu.ReadByte(Mmu.OamEnd + 1)); // Should read 0xFF from unusable region
    }

    [Fact]
    public void DMA_Timing_Activates640Cycles()
    {
        var mmu = new Mmu();

        // Set up source data
        for (int i = 0; i < 160; i++)
        {
            mmu.WriteByte((ushort)(0x8000 + i), (byte)i);
        }

        // Trigger DMA transfer
        mmu.WriteByte(Mmu.DMA, 0x80);

        // DMA should be active immediately after transfer
        Assert.True(mmu.IsDmaActive);

        // Step 639 cycles - should still be active
        mmu.StepDma(639);
        Assert.True(mmu.IsDmaActive);

        // Step 1 more cycle - should complete (640 total)
        mmu.StepDma(1);
        Assert.False(mmu.IsDmaActive);
    }

    [Fact]
    public void DMA_Timing_CompletesAfter640Cycles()
    {
        var mmu = new Mmu();

        // Trigger DMA transfer
        mmu.WriteByte(Mmu.DMA, 0x80);
        Assert.True(mmu.IsDmaActive);

        // Step exactly 640 cycles
        mmu.StepDma(640);
        Assert.False(mmu.IsDmaActive);
    }

    [Fact]
    public void DMA_Timing_CompletesWithExcessCycles()
    {
        var mmu = new Mmu();

        // Trigger DMA transfer
        mmu.WriteByte(Mmu.DMA, 0x80);
        Assert.True(mmu.IsDmaActive);

        // Step more than 640 cycles
        mmu.StepDma(1000);
        Assert.False(mmu.IsDmaActive);
    }

    [Fact]
    public void DMA_StepWhenInactive_DoesNothing()
    {
        var mmu = new Mmu();

        // Ensure DMA is not active
        Assert.False(mmu.IsDmaActive);

        // Step should do nothing
        mmu.StepDma(100);
        Assert.False(mmu.IsDmaActive);
    }

    [Fact]
    public void DMA_Reset_ClearsActiveState()
    {
        var mmu = new Mmu();

        // Trigger DMA transfer
        mmu.WriteByte(Mmu.DMA, 0x80);
        Assert.True(mmu.IsDmaActive);

        // Reset should clear DMA state
        mmu.Reset();
        Assert.False(mmu.IsDmaActive);
    }

    [Fact]
    public void DMA_MultipleTransfers_WorksCorrectly()
    {
        var mmu = new Mmu();

        // First transfer
        for (int i = 0; i < 160; i++)
        {
            mmu.WriteByte((ushort)(0x8000 + i), 0x11);
        }
        mmu.WriteByte(Mmu.DMA, 0x80);

        // Complete first transfer
        mmu.StepDma(640);
        Assert.False(mmu.IsDmaActive);

        // Verify first transfer
        Assert.Equal(0x11, mmu.ReadByte(Mmu.OamStart));

        // Second transfer with different data
        for (int i = 0; i < 160; i++)
        {
            mmu.WriteByte((ushort)(0xC000 + i), 0x22);
        }
        mmu.WriteByte(Mmu.DMA, 0xC0);

        // Should be active again
        Assert.True(mmu.IsDmaActive);

        // Complete second transfer
        mmu.StepDma(640);
        Assert.False(mmu.IsDmaActive);

        // Verify second transfer overwrote first
        Assert.Equal(0x22, mmu.ReadByte(Mmu.OamStart));
    }
}