using System;
using System.Collections.Generic;

namespace GameBoy.Core.Persistence;

/// <summary>
/// Represents a complete emulator save state that can be serialized and restored.
/// Contains all necessary data to recreate the exact emulator state at a point in time.
/// </summary>
public class SaveState
{
    /// <summary>
    /// Save state format version for compatibility checking.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Timestamp when the save state was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Title of the cartridge this save state belongs to.
    /// </summary>
    public string CartridgeTitle { get; set; } = string.Empty;

    /// <summary>
    /// Hash of the ROM data for validation.
    /// </summary>
    public byte[] CartridgeHash { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// CPU register state.
    /// </summary>
    public CpuState Cpu { get; set; } = new();

    /// <summary>
    /// Memory state - Work RAM (0x8000 bytes).
    /// </summary>
    public byte[] WorkRam { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Video RAM state (0x2000 bytes).
    /// </summary>
    public byte[] VideoRam { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// OAM (Sprite) RAM state (0xA0 bytes).
    /// </summary>
    public byte[] OamRam { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// External RAM state (cartridge-dependent size).
    /// </summary>
    public byte[] ExternalRam { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// High RAM state (0x7F bytes).
    /// </summary>
    public byte[] HighRam { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Timer component state.
    /// </summary>
    public TimerState Timer { get; set; } = new();

    /// <summary>
    /// PPU component state.
    /// </summary>
    public PpuState Ppu { get; set; } = new();

    /// <summary>
    /// APU component state.
    /// </summary>
    public ApuState Apu { get; set; } = new();

    /// <summary>
    /// Memory Bank Controller state.
    /// </summary>
    public MbcState Mbc { get; set; } = new();

    /// <summary>
    /// I/O register states (0xFF00-0xFF7F range).
    /// </summary>
    public Dictionary<ushort, byte> IoRegisters { get; set; } = new();

    /// <summary>
    /// Interrupt controller state.
    /// </summary>
    public InterruptState Interrupts { get; set; } = new();

    /// <summary>
    /// Joypad state.
    /// </summary>
    public JoypadState Joypad { get; set; } = new();
}

/// <summary>
/// CPU register and flag state.
/// </summary>
public class CpuState
{
    public ushort PC { get; set; }
    public ushort SP { get; set; }
    public ushort AF { get; set; }
    public ushort BC { get; set; }
    public ushort DE { get; set; }
    public ushort HL { get; set; }
    public bool InterruptsEnabled { get; set; }
    public bool IsHalted { get; set; }
    public bool IsStopped { get; set; }
}

/// <summary>
/// Timer component state.
/// </summary>
public class TimerState
{
    public byte DIV { get; set; }
    public byte TIMA { get; set; }
    public byte TMA { get; set; }
    public byte TAC { get; set; }
    public int InternalCounter { get; set; }
    public bool PreviousBit { get; set; }
}

/// <summary>
/// PPU component state.
/// </summary>
public class PpuState
{
    public byte LCDC { get; set; }
    public byte STAT { get; set; }
    public byte SCY { get; set; }
    public byte SCX { get; set; }
    public byte LY { get; set; }
    public byte LYC { get; set; }
    public byte WY { get; set; }
    public byte WX { get; set; }
    public byte BGP { get; set; }
    public byte OBP0 { get; set; }
    public byte OBP1 { get; set; }
    public int Cycles { get; set; }
    public int Mode { get; set; }
    public bool WindowTriggered { get; set; }
}

/// <summary>
/// APU component state.
/// </summary>
public class ApuState
{
    public byte NR10 { get; set; }
    public byte NR11 { get; set; }
    public byte NR12 { get; set; }
    public byte NR13 { get; set; }
    public byte NR14 { get; set; }
    public byte NR21 { get; set; }
    public byte NR22 { get; set; }
    public byte NR23 { get; set; }
    public byte NR24 { get; set; }
    public byte NR30 { get; set; }
    public byte NR31 { get; set; }
    public byte NR32 { get; set; }
    public byte NR33 { get; set; }
    public byte NR34 { get; set; }
    public byte NR41 { get; set; }
    public byte NR42 { get; set; }
    public byte NR43 { get; set; }
    public byte NR44 { get; set; }
    public byte NR50 { get; set; }
    public byte NR51 { get; set; }
    public byte NR52 { get; set; }
    public byte[] WavePattern { get; set; } = new byte[16];
}

/// <summary>
/// Memory Bank Controller state.
/// </summary>
public class MbcState
{
    public string MbcType { get; set; } = string.Empty;
    public int RomBankNumber { get; set; }
    public int RamBankNumber { get; set; }
    public bool RamEnabled { get; set; }
    public byte BankingMode { get; set; }
    public Dictionary<string, object> AdditionalState { get; set; } = new();
}

/// <summary>
/// Interrupt controller state.
/// </summary>
public class InterruptState
{
    public byte IE { get; set; } // Interrupt Enable
    public byte IF { get; set; } // Interrupt Flag
}

/// <summary>
/// Joypad state.
/// </summary>
public class JoypadState
{
    public bool Up { get; set; }
    public bool Down { get; set; }
    public bool Left { get; set; }
    public bool Right { get; set; }
    public bool A { get; set; }
    public bool B { get; set; }
    public bool Start { get; set; }
    public bool Select { get; set; }
    public byte Register { get; set; }
}