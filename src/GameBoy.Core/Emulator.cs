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
    private readonly InterruptController _intc;
    
    private int _cycleAccumulator = 0;

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
