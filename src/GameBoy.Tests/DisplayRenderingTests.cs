using GameBoy.Core;
using Xunit;

namespace GameBoy.Tests
{
    public class DisplayRenderingTests
    {
        [Fact]
        public void PPU_WithBasicTileData_RendersPattern()
        {
            // Arrange
            var emulator = new Emulator();

            // Load a minimal ROM to initialize the system
            byte[] rom = new byte[0x8000];
            emulator.LoadRom(rom);

            // Set up LCD for background rendering
            emulator.Mmu.WriteByte(IoRegs.LCDC, 0x91); // LCD on, BG on, tiles at 0x8000
            emulator.Mmu.WriteByte(IoRegs.BGP, 0xE4);  // Default palette

            // Create a simple tile pattern in VRAM (tile 0)
            // Tile 0 at 0x8000: create a checkerboard pattern
            for (int y = 0; y < 8; y++)
            {
                byte lowBits = (byte)(y % 2 == 0 ? 0xAA : 0x55); // Alternating pattern
                byte highBits = (byte)(y % 2 == 0 ? 0x55 : 0xAA); // Inverse pattern
                emulator.Mmu.WriteByte((ushort)(0x8000 + y * 2), lowBits);
                emulator.Mmu.WriteByte((ushort)(0x8000 + y * 2 + 1), highBits);
            }

            // Set background tile map to use tile 0 for first few tiles
            for (int i = 0; i < 32; i++)
            {
                emulator.Mmu.WriteByte((ushort)(0x9800 + i), 0x00); // Use tile 0
            }

            // Act - Step through enough cycles to render a few scanlines
            for (int i = 0; i < 5000; i++)
            {
                int cycles = emulator.Cpu.Step();
                emulator.Timer.Step(cycles);
                emulator.Ppu.Step(cycles);
                emulator.Serial.Step(cycles);
                emulator.Mmu.StepDma(cycles);

                // Check if we've rendered some scanlines
                if (emulator.Ppu.LY > 5) break;
            }

            // Assert - Frame buffer should contain pattern, not solid color
            var frameBuffer = emulator.Ppu.FrameBuffer;

            // Check that the frame buffer is not entirely one color
            int firstPixelColor = frameBuffer[0];
            bool hasVariation = false;

            // Check first scanline for color variation
            for (int x = 0; x < Ppu.ScreenWidth && x < frameBuffer.Length; x++)
            {
                if (frameBuffer[x] != firstPixelColor)
                {
                    hasVariation = true;
                    break;
                }
            }

            Assert.True(hasVariation, "Frame buffer should show tile pattern, not solid color");
        }

        [Fact]
        public void PPU_RegisterSynchronization_LYUpdatesInMMU()
        {
            // Arrange
            var emulator = new Emulator();
            byte[] rom = new byte[0x8000];
            emulator.LoadRom(rom);

            // Enable LCD
            emulator.Mmu.WriteByte(IoRegs.LCDC, 0x91);

            byte initialLY = emulator.Mmu.ReadByte(IoRegs.LY);

            // Act - Step through some cycles to advance LY
            for (int i = 0; i < 1000; i++)
            {
                int cycles = emulator.Cpu.Step();
                emulator.Timer.Step(cycles);
                emulator.Ppu.Step(cycles);
                emulator.Serial.Step(cycles);
                emulator.Mmu.StepDma(cycles);

                if (emulator.Ppu.LY > initialLY) break;
            }

            // Assert - LY should be synchronized between PPU and MMU
            byte ppuLY = emulator.Ppu.LY;
            byte mmuLY = emulator.Mmu.ReadByte(IoRegs.LY);

            Assert.Equal(ppuLY, mmuLY);
            Assert.True(ppuLY > initialLY, "LY should have advanced");
        }
    }
}