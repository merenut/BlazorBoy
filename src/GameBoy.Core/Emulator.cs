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

    private int _cycleAccumulator = 0;

    public Joypad Joypad { get; } = new();

    public Ppu Ppu => _ppu;
    public Cpu Cpu => _cpu;
    public Mmu Mmu => _mmu;
    public InterruptController InterruptController => _mmu.InterruptController;

    public Emulator()
    {
        _mmu = new Mmu();
        _cpu = new Cpu(_mmu);
        _ppu = new Ppu();
        _timer = new Timer();
    }

    /// <summary>
    /// Resets the emulator to post-BIOS state.
    /// </summary>
    public void Reset()
    {
        _mmu.Reset();
        _cpu.Reset();
    }

    /// <summary>
    /// Loads a ROM image into the emulator.
    /// </summary>
    public void LoadRom(byte[] rom) => _mmu.LoadRom(rom);

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
    /// Runs a single CPU step and returns whether a frame is ready.
    /// Accumulates cycles until 70224 cycles reached (one frame).
    /// </summary>
    public bool StepFrame()
    {
        int cycles = _cpu.Step();
        _timer.Step(cycles);
        _ppu.Step(cycles);
        _cycleAccumulator += cycles;

        if (_cycleAccumulator >= CyclesPerFrame)
        {
            // Frame is complete - reset accumulator for next frame
            _cycleAccumulator -= CyclesPerFrame;
            return true;
        }

        return false;
    }
}
