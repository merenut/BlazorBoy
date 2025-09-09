using System;

namespace GameBoy.Core;

/// <summary>
/// High-level emulator facade coordinating CPU, MMU, PPU, timers, and input.
/// </summary>
public sealed class Emulator
{
    private const int CyclesPerFrame = 70224; // 154 scanlines * 456 cycles per scanline

    private readonly Mmu _mmu;
    private readonly Cpu _cpu;
    private readonly Ppu _ppu;
    private readonly Timer _timer;
    private readonly Serial _serial;

    private int _cycleAccumulator = 0;

    public Joypad Joypad { get; }

    public Ppu Ppu => _ppu;
    public Cpu Cpu => _cpu;
    public Mmu Mmu => _mmu;
    public Timer Timer => _timer;
    public InterruptController InterruptController => _mmu.InterruptController;
    public Serial Serial => _serial;

    public Emulator()
    {
        _mmu = new Mmu();
        _cpu = new Cpu(_mmu);

        // Pass InterruptController to all components that need to request interrupts
        _ppu = new Ppu(_mmu.InterruptController);
        _timer = new Timer(_mmu.InterruptController);
        _serial = new Serial(_mmu.InterruptController);
        Joypad = new Joypad(_mmu.InterruptController);

        // Connect timer and PPU to MMU for I/O register coordination
        _mmu.Timer = _timer;
        _mmu.Ppu = _ppu;
        _mmu.Joypad = Joypad;
        _ppu.Mmu = _mmu;
    }

    /// <summary>
    /// Resets the emulator to post-BIOS state.
    /// </summary>
    public void Reset()
    {
        _mmu.Reset();
        _cpu.Reset();
        _ppu.Reset();
    }

    /// <summary>
    /// Loads a ROM image into the emulator.
    /// </summary>
    public void LoadRom(byte[] rom) => _mmu.LoadRom(rom);

    /// <summary>
    /// Creates a test pattern in VRAM to verify rendering pipeline.
    /// This method is for debugging purposes only.
    /// </summary>
    public void CreateTestPattern()
    {
        // Create a simple checkerboard pattern in tile 0
        // Tile 0 starts at VRAM address 0x8000

        // Each tile is 8x8 pixels, 2 bits per pixel, 16 bytes total
        // Each row is 2 bytes (low bits + high bits)

        // Create a checkerboard pattern (alternating colors 0 and 3)
        for (int y = 0; y < 8; y++)
        {
            byte lowBits = 0;
            byte highBits = 0;

            for (int x = 0; x < 8; x++)
            {
                // Checkerboard: use color 3 if (x+y) is odd, color 0 if even
                bool useColor3 = ((x + y) % 2) == 1;
                if (useColor3)
                {
                    lowBits |= (byte)(1 << (7 - x));  // Set low bit
                    highBits |= (byte)(1 << (7 - x)); // Set high bit (color 3 = 11 binary)
                }
                // For color 0, both bits remain 0 (already initialized)
            }

            // Write the row data to VRAM
            ushort tileAddr = (ushort)(0x8000 + (y * 2));
            _mmu.WriteByte(tileAddr, lowBits);       // Low bit plane
            _mmu.WriteByte((ushort)(tileAddr + 1), highBits); // High bit plane
        }

        // Fill background tile map with tile 0 (creates a checkerboard screen)
        for (int i = 0; i < 32 * 32; i++)
        {
            _mmu.WriteByte((ushort)(0x9800 + i), 0); // Tile map pointing to tile 0
        }

        // Force the PPU to render the frame immediately
        RenderVramTestFrame();
    }

    /// <summary>
    /// Creates a direct test pattern in the frame buffer to test canvas rendering.
    /// This bypasses the PPU and directly modifies the frame buffer.
    /// </summary>
    public void CreateDirectTestPattern()
    {
        var frameBuffer = _ppu.FrameBuffer;

        // Create a simple pattern directly in the frame buffer
        for (int y = 0; y < Ppu.ScreenHeight; y++)
        {
            for (int x = 0; x < Ppu.ScreenWidth; x++)
            {
                int index = y * Ppu.ScreenWidth + x;

                // Create a simple pattern
                if ((x / 20 + y / 20) % 2 == 0)
                {
                    frameBuffer[index] = Palette.ToRgba(0); // Light green
                }
                else
                {
                    frameBuffer[index] = Palette.ToRgba(3); // Dark green
                }
            }
        }
    }

    /// <summary>
    /// Forces the PPU to render the entire frame from VRAM data.
    /// Used for testing VRAM patterns without running the full emulation loop.
    /// </summary>
    private void RenderVramTestFrame()
    {
        // Reset PPU state and force a complete frame render
        _ppu.Reset();
        _ppu.ForceRenderFrame();
    }

    /// <summary>
    /// Gets the battery-backed external RAM data for saving.
    /// </summary>
    /// <returns>The external RAM data, or null if no battery-backed RAM exists or no data to save.</returns>
    public byte[]? GetBatteryRam()
    {
        if (_mmu.Cartridge is IBatteryBacked batteryCartridge)
        {
            return batteryCartridge.GetExternalRam();
        }
        return null;
    }

    /// <summary>
    /// Loads battery-backed external RAM data.
    /// </summary>
    /// <param name="data">The external RAM data to load, or null if no save data exists.</param>
    public void LoadBatteryRam(byte[]? data)
    {
        if (_mmu.Cartridge is IBatteryBacked batteryCartridge)
        {
            batteryCartridge.LoadExternalRam(data);
        }
    }

    /// <summary>
    /// Gets whether the current cartridge has battery-backed RAM.
    /// </summary>
    public bool HasBatteryRam => _mmu.Cartridge is IBatteryBacked batteryCartridge && batteryCartridge.HasBattery;

    /// <summary>
    /// Runs the emulator for a target number of cycles or until a frame is completed.
    /// </summary>
    /// <param name="targetCycles">Maximum cycles to run (default: run until frame complete)</param>
    /// <returns>True if a complete frame was rendered</returns>
    public bool StepFrame(int targetCycles = CyclesPerFrame)
    {
        bool frameCompleted = false;
        int cyclesRun = 0;

        while (_cycleAccumulator < CyclesPerFrame && cyclesRun < targetCycles)
        {
            int cycles = _cpu.Step();
            _timer.Step(cycles);
            if (_ppu.Step(cycles))
            {
                frameCompleted = true;
            }
            _serial.Step(cycles);
            _mmu.StepDma(cycles);
            _cycleAccumulator += cycles;
            cyclesRun += cycles;
        }

        // If frame is complete, reset accumulator for next frame
        if (_cycleAccumulator >= CyclesPerFrame)
        {
            _cycleAccumulator -= CyclesPerFrame;
            frameCompleted = true;
        }

        return frameCompleted;
    }
}
