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
        _serial.Step(cycles);
        _mmu.StepDma(cycles);
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
