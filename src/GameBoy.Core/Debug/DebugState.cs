using System;

namespace GameBoy.Core.Debug;

/// <summary>
/// Represents the complete debug state of the CPU.
/// </summary>
public readonly struct CpuState
{
    public ushort PC { get; }
    public ushort SP { get; }
    public ushort AF { get; }
    public ushort BC { get; }
    public ushort DE { get; }
    public ushort HL { get; }
    public bool InterruptsEnabled { get; }
    public bool IsHalted { get; }

    public byte A => (byte)(AF >> 8);
    public byte F => (byte)(AF & 0xF0);
    public byte B => (byte)(BC >> 8);
    public byte C => (byte)(BC & 0xFF);
    public byte D => (byte)(DE >> 8);
    public byte E => (byte)(DE & 0xFF);
    public byte H => (byte)(HL >> 8);
    public byte L => (byte)(HL & 0xFF);

    public bool FlagZ => (F & 0x80) != 0;
    public bool FlagN => (F & 0x40) != 0;
    public bool FlagH => (F & 0x20) != 0;
    public bool FlagC => (F & 0x10) != 0;

    public CpuState(Cpu.Registers registers, bool interruptsEnabled, bool isHalted)
    {
        PC = registers.PC;
        SP = registers.SP;
        AF = registers.AF;
        BC = registers.BC;
        DE = registers.DE;
        HL = registers.HL;
        InterruptsEnabled = interruptsEnabled;
        IsHalted = isHalted;
    }
}

/// <summary>
/// Represents the debug state of the PPU.
/// </summary>
public readonly struct PpuState
{
    public byte LCDC { get; }
    public byte STAT { get; }
    public byte SCY { get; }
    public byte SCX { get; }
    public byte LY { get; }
    public byte LYC { get; }
    public byte WY { get; }
    public byte WX { get; }
    public byte BGP { get; }
    public byte OBP0 { get; }
    public byte OBP1 { get; }

    public int Mode => STAT & 0x03;
    public bool LcdEnabled => (LCDC & 0x80) != 0;
    public bool WindowEnabled => (LCDC & 0x20) != 0;
    public bool SpritesEnabled => (LCDC & 0x02) != 0;
    public bool BackgroundEnabled => (LCDC & 0x01) != 0;

    public PpuState(byte lcdc, byte stat, byte scy, byte scx, byte ly, byte lyc,
                   byte wy, byte wx, byte bgp, byte obp0, byte obp1)
    {
        LCDC = lcdc;
        STAT = stat;
        SCY = scy;
        SCX = scx;
        LY = ly;
        LYC = lyc;
        WY = wy;
        WX = wx;
        BGP = bgp;
        OBP0 = obp0;
        OBP1 = obp1;
    }
}

/// <summary>
/// Represents the debug state of the Timer.
/// </summary>
public readonly struct TimerState
{
    public byte DIV { get; }
    public byte TIMA { get; }
    public byte TMA { get; }
    public byte TAC { get; }

    public bool TimerEnabled => (TAC & 0x04) != 0;
    public int TimerFrequency => TAC & 0x03;

    public TimerState(byte div, byte tima, byte tma, byte tac)
    {
        DIV = div;
        TIMA = tima;
        TMA = tma;
        TAC = tac;
    }
}

/// <summary>
/// Represents the debug state of interrupts.
/// </summary>
public readonly struct InterruptState
{
    public byte IE { get; }
    public byte IF { get; }

    public bool VBlankEnabled => (IE & 0x01) != 0;
    public bool LcdStatEnabled => (IE & 0x02) != 0;
    public bool TimerEnabled => (IE & 0x04) != 0;
    public bool SerialEnabled => (IE & 0x08) != 0;
    public bool JoypadEnabled => (IE & 0x10) != 0;

    public bool VBlankRequested => (IF & 0x01) != 0;
    public bool LcdStatRequested => (IF & 0x02) != 0;
    public bool TimerRequested => (IF & 0x04) != 0;
    public bool SerialRequested => (IF & 0x08) != 0;
    public bool JoypadRequested => (IF & 0x10) != 0;

    public InterruptState(byte ie, byte @if)
    {
        IE = ie;
        IF = @if;
    }
}

/// <summary>
/// Aggregate debug state capturing all emulator components at a point in time.
/// </summary>
public readonly struct DebugState
{
    public CpuState Cpu { get; }
    public PpuState Ppu { get; }
    public TimerState Timer { get; }
    public InterruptState Interrupts { get; }
    public ushort CurrentPc => Cpu.PC;
    public ulong TotalCycles { get; }
    public DateTime CapturedAt { get; }

    public DebugState(CpuState cpu, PpuState ppu, TimerState timer, InterruptState interrupts, ulong totalCycles)
    {
        Cpu = cpu;
        Ppu = ppu;
        Timer = timer;
        Interrupts = interrupts;
        TotalCycles = totalCycles;
        CapturedAt = DateTime.UtcNow;
    }
}