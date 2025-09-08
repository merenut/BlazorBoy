using Xunit;
using GameBoy.Core;

namespace GameBoy.Tests;

/// <summary>
/// Integration tests for DMA functionality within the full emulator context.
/// Tests DMA timing coordination with emulator step cycles.
/// </summary>
public class DmaIntegrationTests
{
    [Fact]
    public void Emulator_DMAIntegration_TimingWorksCorrectly()
    {
        var emulator = new Emulator();
        
        // Set up test data in VRAM
        for (int i = 0; i < 160; i++)
        {
            emulator.Mmu.WriteByte((ushort)(0x8000 + i), (byte)(i + 0x10));
        }
        
        // Trigger DMA transfer via emulator's MMU
        emulator.Mmu.WriteByte(Mmu.DMA, 0x80);
        
        // DMA should be active
        Assert.True(emulator.Mmu.IsDmaActive);
        
        // Verify data was copied to OAM
        for (int i = 0; i < 160; i++)
        {
            byte expected = (byte)(i + 0x10);
            byte actual = emulator.Mmu.ReadByte((ushort)(Mmu.OamStart + i));
            Assert.Equal(expected, actual);
        }
        
        // Step emulator to complete DMA timing (need roughly 640 cycles)
        // Each StepFrame can execute variable cycles based on CPU instruction
        int stepCount = 0;
        while (emulator.Mmu.IsDmaActive && stepCount < 1000)
        {
            emulator.StepFrame(); // This internally calls StepDma
            stepCount++;
        }
        
        // DMA should complete and be inactive
        Assert.False(emulator.Mmu.IsDmaActive);
        Assert.True(stepCount < 1000); // Should not take more than 1000 steps
    }

    [Fact] 
    public void Emulator_MultipleDMATransfers_WorkInSequence()
    {
        var emulator = new Emulator();
        
        // First DMA transfer
        for (int i = 0; i < 160; i++)
        {
            emulator.Mmu.WriteByte((ushort)(0x8000 + i), 0x11);
        }
        emulator.Mmu.WriteByte(Mmu.DMA, 0x80);
        
        // Complete first transfer via emulator stepping
        while (emulator.Mmu.IsDmaActive)
        {
            emulator.StepFrame();
        }
        
        // Verify first transfer
        Assert.Equal(0x11, emulator.Mmu.ReadByte(Mmu.OamStart));
        
        // Second DMA transfer  
        for (int i = 0; i < 160; i++)
        {
            emulator.Mmu.WriteByte((ushort)(0xC000 + i), 0x22);
        }
        emulator.Mmu.WriteByte(Mmu.DMA, 0xC0);
        
        // Should be active again
        Assert.True(emulator.Mmu.IsDmaActive);
        
        // Complete second transfer
        while (emulator.Mmu.IsDmaActive)
        {
            emulator.StepFrame();
        }
        
        // Verify second transfer overwrote first
        Assert.Equal(0x22, emulator.Mmu.ReadByte(Mmu.OamStart));
    }
}