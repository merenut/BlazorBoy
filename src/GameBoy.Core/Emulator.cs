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
    private readonly Apu _apu;

    private int _cycleAccumulator = 0;

    public Joypad Joypad { get; }

    public Ppu Ppu => _ppu;
    public Cpu Cpu => _cpu;
    public Mmu Mmu => _mmu;
    public Timer Timer => _timer;
    public InterruptController InterruptController => _mmu.InterruptController;
    public Serial Serial => _serial;
    public Apu Apu => _apu;

    public Emulator()
    {
        _mmu = new Mmu();
        _cpu = new Cpu(_mmu);

        // Pass InterruptController to all components that need to request interrupts
        _ppu = new Ppu(_mmu.InterruptController);
        _timer = new Timer(_mmu.InterruptController);
        _serial = new Serial(_mmu.InterruptController);
        _apu = new Apu();
        Joypad = new Joypad(_mmu.InterruptController);

        // Connect timer and PPU to MMU for I/O register coordination
        _mmu.Timer = _timer;
        _mmu.Ppu = _ppu;
        _mmu.Apu = _apu;
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
        _apu.Reset();
    }

    /// <summary>
    /// Loads a ROM image into the emulator.
    /// </summary>
    public void LoadRom(byte[] rom)
    {
        _mmu.LoadRom(rom);
        Reset(); // Reset emulator state when loading a new ROM
    }

    /// <summary>
    /// Gets debugging information about the current emulator state.
    /// </summary>
    public string GetDebugInfo()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"CPU PC: 0x{_cpu.Regs.PC:X4}");
        sb.AppendLine($"CPU AF: 0x{_cpu.Regs.AF:X4}");
        sb.AppendLine($"CPU BC: 0x{_cpu.Regs.BC:X4}");
        sb.AppendLine($"CPU DE: 0x{_cpu.Regs.DE:X4}");
        sb.AppendLine($"CPU HL: 0x{_cpu.Regs.HL:X4}");
        sb.AppendLine($"CPU SP: 0x{_cpu.Regs.SP:X4}");
        sb.AppendLine($"CPU Halted: {_cpu.IsHalted}");
        sb.AppendLine($"CPU IME: {_cpu.InterruptsEnabled}");
        sb.AppendLine($"Cartridge: {(_mmu.Cartridge?.GetType().Name ?? "None")}");
        sb.AppendLine($"ROM @ 0x0100: 0x{_mmu.ReadByte(0x0100):X2}");
        sb.AppendLine($"ROM @ 0x0101: 0x{_mmu.ReadByte(0x0101):X2}");
        sb.AppendLine($"ROM @ 0x0102: 0x{_mmu.ReadByte(0x0102):X2}");
        sb.AppendLine($"ROM @ 0x0103: 0x{_mmu.ReadByte(0x0103):X2}");

        // Show current instruction being executed
        if (_mmu.Cartridge != null)
        {
            sb.AppendLine($"Current Instr @ PC: 0x{_mmu.ReadByte(_cpu.Regs.PC):X2}");
            sb.AppendLine($"Next Instr @ PC+1: 0x{_mmu.ReadByte((ushort)(_cpu.Regs.PC + 1)):X2}");
        }

        sb.AppendLine($"LCDC: 0x{_mmu.ReadByte(IoRegs.LCDC):X2}");
        sb.AppendLine($"BGP: 0x{_mmu.ReadByte(IoRegs.BGP):X2}");
        sb.AppendLine($"SCX: 0x{_mmu.ReadByte(IoRegs.SCX):X2}");
        sb.AppendLine($"SCY: 0x{_mmu.ReadByte(IoRegs.SCY):X2}");
        sb.AppendLine($"LY: 0x{_mmu.ReadByte(IoRegs.LY):X2}");
        sb.AppendLine($"IE: 0x{_mmu.ReadByte(0xFFFF):X2}");
        sb.AppendLine($"IF: 0x{_mmu.ReadByte(IoRegs.IF):X2}");

        // Check if anything is in VRAM
        bool vramEmpty = true;
        for (int i = 0x8000; i < 0x9000 && vramEmpty; i++)
        {
            if (_mmu.ReadByte((ushort)i) != 0)
                vramEmpty = false;
        }
        sb.AppendLine($"VRAM Empty: {vramEmpty}");

        // Check a few tile map bytes
        sb.AppendLine($"Tile Map @ 0x9800: 0x{_mmu.ReadByte(0x9800):X2}");
        sb.AppendLine($"Tile Map @ 0x9801: 0x{_mmu.ReadByte(0x9801):X2}");
        sb.AppendLine($"Tile Map @ 0x9802: 0x{_mmu.ReadByte(0x9802):X2}");

        // Check first few bytes of tile 0x2F (which is in the tile map)
        int tileAddr = 0x8000 + (0x2F * 16);
        sb.AppendLine($"Tile 0x2F @ 0x{tileAddr:X4}: 0x{_mmu.ReadByte((ushort)tileAddr):X2}");
        sb.AppendLine($"Tile 0x2F @ 0x{tileAddr + 1:X4}: 0x{_mmu.ReadByte((ushort)(tileAddr + 1)):X2}");

        return sb.ToString();
    }

    /// <summary>
    /// Creates a test pattern in VRAM to verify rendering pipeline.
    /// This method is for debugging purposes only.
    /// </summary>
    public void CreateTestPattern()
    {
        // Ensure LCD and background are enabled for rendering
        _mmu.WriteByte(IoRegs.LCDC, 0x91); // LCD enable + BG enable + 8x8 sprites + BG tile map at 0x9800 + BG tile data at 0x8000
        _mmu.WriteByte(IoRegs.BGP, 0xE4);  // Background palette: 11,10,01,00 (dark to light)

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
        // Don't reset PPU state - we want to keep the registers we just configured
        // Just force a complete frame render
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
    /// Creates a save state containing the complete emulator state.
    /// </summary>
    /// <returns>A save state object with all emulator data</returns>
    public Persistence.SaveState CreateSaveState()
    {
        if (_mmu.Cartridge == null)
            throw new InvalidOperationException("No ROM loaded");

        var saveState = new Persistence.SaveState();

        // Get ROM data for validation
        var romData = _mmu.GetRomData();
        if (romData != null)
        {
            saveState.CartridgeTitle = _mmu.Cartridge.GetType().Name; // Simple title for now
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                saveState.CartridgeHash = sha256.ComputeHash(romData[..Math.Min(romData.Length, 32768)]);
            }
        }

        // CPU State
        saveState.Cpu = new Persistence.CpuState
        {
            PC = _cpu.Regs.PC,
            SP = _cpu.Regs.SP,
            AF = _cpu.Regs.AF,
            BC = _cpu.Regs.BC,
            DE = _cpu.Regs.DE,
            HL = _cpu.Regs.HL,
            InterruptsEnabled = _cpu.InterruptsEnabled,
            IsHalted = _cpu.IsHalted
        };

        // Memory State
        saveState.WorkRam = _mmu.GetWorkRam();
        saveState.VideoRam = _mmu.GetVideoRam();
        saveState.OamRam = _mmu.GetOamRam();
        saveState.HighRam = _mmu.GetHighRam();

        // External RAM (if present)
        if (_mmu.Cartridge is IBatteryBacked batteryCartridge)
        {
            saveState.ExternalRam = batteryCartridge.GetExternalRam() ?? Array.Empty<byte>();
        }

        // Timer State
        saveState.Timer = new Persistence.TimerState
        {
            DIV = _mmu.ReadByte(IoRegs.DIV),
            TIMA = _mmu.ReadByte(IoRegs.TIMA),
            TMA = _mmu.ReadByte(IoRegs.TMA),
            TAC = _mmu.ReadByte(IoRegs.TAC)
        };

        // PPU State
        saveState.Ppu = new Persistence.PpuState
        {
            LCDC = _mmu.ReadByte(IoRegs.LCDC),
            STAT = _mmu.ReadByte(IoRegs.STAT),
            SCY = _mmu.ReadByte(IoRegs.SCY),
            SCX = _mmu.ReadByte(IoRegs.SCX),
            LY = _mmu.ReadByte(IoRegs.LY),
            LYC = _mmu.ReadByte(IoRegs.LYC),
            WY = _mmu.ReadByte(IoRegs.WY),
            WX = _mmu.ReadByte(IoRegs.WX),
            BGP = _mmu.ReadByte(IoRegs.BGP),
            OBP0 = _mmu.ReadByte(IoRegs.OBP0),
            OBP1 = _mmu.ReadByte(IoRegs.OBP1)
        };

        // APU State
        saveState.Apu = new Persistence.ApuState
        {
            NR10 = _mmu.ReadByte(0xFF10),
            NR11 = _mmu.ReadByte(0xFF11),
            NR12 = _mmu.ReadByte(0xFF12),
            NR13 = _mmu.ReadByte(0xFF13),
            NR14 = _mmu.ReadByte(0xFF14),
            NR21 = _mmu.ReadByte(0xFF16),
            NR22 = _mmu.ReadByte(0xFF17),
            NR23 = _mmu.ReadByte(0xFF18),
            NR24 = _mmu.ReadByte(0xFF19),
            NR30 = _mmu.ReadByte(0xFF1A),
            NR31 = _mmu.ReadByte(0xFF1B),
            NR32 = _mmu.ReadByte(0xFF1C),
            NR33 = _mmu.ReadByte(0xFF1D),
            NR34 = _mmu.ReadByte(0xFF1E),
            NR41 = _mmu.ReadByte(0xFF20),
            NR42 = _mmu.ReadByte(0xFF21),
            NR43 = _mmu.ReadByte(0xFF22),
            NR44 = _mmu.ReadByte(0xFF23),
            NR50 = _mmu.ReadByte(0xFF24),
            NR51 = _mmu.ReadByte(0xFF25),
            NR52 = _mmu.ReadByte(0xFF26)
        };

        // Wave pattern RAM
        for (int i = 0; i < 16; i++)
        {
            saveState.Apu.WavePattern[i] = _mmu.ReadByte((ushort)(0xFF30 + i));
        }

        // Interrupt State
        saveState.Interrupts = new Persistence.InterruptState
        {
            IE = _mmu.ReadByte(0xFFFF),
            IF = _mmu.ReadByte(IoRegs.IF)
        };

        // Joypad State
        saveState.Joypad = new Persistence.JoypadState
        {
            Up = Joypad.Up,
            Down = Joypad.Down,
            Left = Joypad.Left,
            Right = Joypad.Right,
            A = Joypad.A,
            B = Joypad.B,
            Start = Joypad.Start,
            Select = Joypad.Select,
            Register = _mmu.ReadByte(IoRegs.P1_JOYP)
        };

        // MBC State (basic for now)
        saveState.Mbc = new Persistence.MbcState
        {
            MbcType = _mmu.Cartridge.GetType().Name
        };

        // I/O Registers
        saveState.IoRegisters = new Dictionary<ushort, byte>();
        for (ushort addr = 0xFF00; addr <= 0xFF7F; addr++)
        {
            saveState.IoRegisters[addr] = _mmu.ReadByte(addr);
        }

        return saveState;
    }

    /// <summary>
    /// Loads a save state and restores the emulator to that exact state.
    /// </summary>
    /// <param name="saveState">The save state to load</param>
    public void LoadSaveState(Persistence.SaveState saveState)
    {
        if (saveState == null)
            throw new ArgumentNullException(nameof(saveState));

        if (_mmu.Cartridge == null)
            throw new InvalidOperationException("No ROM loaded");

        // Validate compatibility
        var romData = _mmu.GetRomData();
        if (romData != null && !Persistence.SaveStateSerializer.ValidateCompatibility(saveState, romData))
        {
            throw new InvalidOperationException("Save state is not compatible with current ROM");
        }

        // Restore CPU State
        _cpu.Regs.PC = saveState.Cpu.PC;
        _cpu.Regs.SP = saveState.Cpu.SP;
        _cpu.Regs.AF = saveState.Cpu.AF;
        _cpu.Regs.BC = saveState.Cpu.BC;
        _cpu.Regs.DE = saveState.Cpu.DE;
        _cpu.Regs.HL = saveState.Cpu.HL;
        _cpu.InterruptsEnabled = saveState.Cpu.InterruptsEnabled;
        // Note: IsHalted is read-only, would need to add setter if needed

        // Restore Memory State
        _mmu.LoadWorkRam(saveState.WorkRam);
        _mmu.LoadVideoRam(saveState.VideoRam);
        _mmu.LoadOamRam(saveState.OamRam);
        _mmu.LoadHighRam(saveState.HighRam);

        // Restore External RAM
        if (_mmu.Cartridge is IBatteryBacked batteryCartridge && saveState.ExternalRam.Length > 0)
        {
            batteryCartridge.LoadExternalRam(saveState.ExternalRam);
        }

        // Restore I/O Registers
        foreach (var kvp in saveState.IoRegisters)
        {
            _mmu.WriteByte(kvp.Key, kvp.Value);
        }

        // Reset components to ensure consistent state
        _ppu.Reset();
        _timer.Reset();
        _apu.Reset();
    }

    /// <summary>
    /// Serializes the current emulator state to a compressed byte array.
    /// </summary>
    /// <returns>Serialized save state data</returns>
    public byte[] SerializeSaveState()
    {
        var saveState = CreateSaveState();
        return Persistence.SaveStateSerializer.Serialize(saveState);
    }

    /// <summary>
    /// Deserializes and loads a save state from compressed byte array.
    /// </summary>
    /// <param name="data">Serialized save state data</param>
    public void DeserializeSaveState(byte[] data)
    {
        var saveState = Persistence.SaveStateSerializer.Deserialize(data);
        LoadSaveState(saveState);
    }

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
            _apu.Step(cycles);
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
