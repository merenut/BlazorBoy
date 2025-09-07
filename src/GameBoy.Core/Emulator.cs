using System;

namespace GameBoy.Core;

/// <summary>
/// High-level emulator facade coordinating CPU, MMU, PPU, timers, and input.
/// </summary>
public sealed class Emulator
{
    private readonly Mmu _mmu;
    private readonly Cpu _cpu;
    private readonly Ppu _ppu;
    private readonly Timer _timer;
    private readonly InterruptController _intc;

    public Joypad Joypad { get; } = new();

    public Ppu Ppu => _ppu;
    public Cpu Cpu => _cpu;
    public Mmu Mmu => _mmu;

    public Emulator()
    {
        _mmu = new Mmu();
        _cpu = new Cpu(_mmu);
        _ppu = new Ppu();
        _timer = new Timer();
        _intc = new InterruptController();
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
    /// Runs a small number of cycles and returns whether a frame is ready.
    /// </summary>
    public bool StepFrame()
    {
        int cycles = _cpu.Step();
        _timer.Step(cycles);
        return _ppu.Step(cycles);
    }
}
