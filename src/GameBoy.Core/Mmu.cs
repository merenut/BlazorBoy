using System;

namespace GameBoy.Core;

/// <summary>
/// Represents the Game Boy Memory Management Unit and memory map.
/// </summary>
public sealed class Mmu
{
    public const int AddressSpaceSize = 0x10000;

    public const ushort Rom0Start = 0x0000;
    public const ushort Rom0End = 0x3FFF;
    public const ushort RomXStart = 0x4000;
    public const ushort RomXEnd = 0x7FFF;
    public const ushort VramStart = 0x8000;
    public const ushort VramEnd = 0x9FFF;
    public const ushort ExtRamStart = 0xA000;
    public const ushort ExtRamEnd = 0xBFFF;
    public const ushort WorkRamStart = 0xC000;
    public const ushort WorkRamEnd = 0xDFFF;
    public const ushort EchoRamStart = 0xE000;
    public const ushort EchoRamEnd = 0xFDFF;
    public const ushort OamStart = 0xFE00;
    public const ushort OamEnd = 0xFE9F;
    public const ushort IoStart = IoRegs.P1_JOYP; // 0xFF00
    public const ushort IoEnd = 0xFF7F;
    public const ushort HramStart = 0xFF80;
    public const ushort HramEnd = 0xFFFE;
    public const ushort InterruptEnable = IoRegs.IE; // 0xFFFF

    private readonly byte[] _mem = new byte[AddressSpaceSize];

    public Cartridge? Cartridge { get; set; }

    /// <summary>
    /// Initializes a new instance of the MMU and sets post-BIOS I/O register defaults.
    /// </summary>
    public Mmu()
    {
        InitializePostBiosDefaults();
    }

    /// <summary>
    /// Sets I/O register values to their post-BIOS defaults for DMG compatibility.
    /// </summary>
    public void InitializePostBiosDefaults()
    {
        // Joypad register (P1/JOYP)
        _mem[0xFF00] = 0xCF;
        
        // Serial transfer registers
        _mem[0xFF01] = 0x00; // SB - Serial transfer data
        _mem[0xFF02] = 0x7E; // SC - Serial transfer control
        
        // Timer registers
        _mem[0xFF04] = 0x00; // DIV - Divider register
        _mem[0xFF05] = 0x00; // TIMA - Timer counter
        _mem[0xFF06] = 0x00; // TMA - Timer modulo
        _mem[0xFF07] = 0xF8; // TAC - Timer control
        
        // Interrupt registers
        _mem[0xFF0F] = 0xE1; // IF - Interrupt flag
        _mem[0xFFFF] = 0x00; // IE - Interrupt enable
        
        // Sound registers (key defaults)
        _mem[0xFF10] = 0x80; // NR10
        _mem[0xFF11] = 0xBF; // NR11
        _mem[0xFF12] = 0xF3; // NR12
        _mem[0xFF14] = 0xBF; // NR14
        _mem[0xFF16] = 0x3F; // NR21
        _mem[0xFF17] = 0x00; // NR22
        _mem[0xFF19] = 0xBF; // NR24
        _mem[0xFF1A] = 0x7F; // NR30
        _mem[0xFF1B] = 0xFF; // NR31
        _mem[0xFF1C] = 0x9F; // NR32
        _mem[0xFF1E] = 0xBF; // NR34
        _mem[0xFF20] = 0xFF; // NR41
        _mem[0xFF21] = 0x00; // NR42
        _mem[0xFF22] = 0x00; // NR43
        _mem[0xFF23] = 0xBF; // NR44
        _mem[0xFF24] = 0x77; // NR50
        _mem[0xFF25] = 0xF3; // NR51
        _mem[0xFF26] = 0xF1; // NR52
        
        // LCD registers
        _mem[0xFF40] = 0x91; // LCDC - LCD control
        _mem[0xFF41] = 0x85; // STAT - LCD status
        _mem[0xFF42] = 0x00; // SCY - Scroll Y
        _mem[0xFF43] = 0x00; // SCX - Scroll X
        _mem[0xFF44] = 0x00; // LY - LCD Y coordinate
        _mem[0xFF45] = 0x00; // LYC - LY compare
        _mem[0xFF46] = 0xFF; // DMA - DMA transfer
        _mem[0xFF47] = 0xFC; // BGP - Background palette
        _mem[0xFF48] = 0x00; // OBP0 - Object palette 0
        _mem[0xFF49] = 0x00; // OBP1 - Object palette 1
        _mem[0xFF4A] = 0x00; // WY - Window Y position
        _mem[0xFF4B] = 0x00; // WX - Window X position
    }

    /// <summary>
    /// Resets the MMU to post-BIOS state.
    /// </summary>
    public void Reset()
    {
        // Clear all memory except ROM areas
        Array.Clear(_mem, VramStart, AddressSpaceSize - VramStart);
        InitializePostBiosDefaults();
    }

    /// <summary>
    /// Reads a byte from the Game Boy memory map.
    /// </summary>
    public byte ReadByte(ushort addr)
    {
        if (addr < 0x8000)
        {
            if (Cartridge is null) return 0xFF;
            return Cartridge.ReadRom(addr);
        }
        return _mem[addr];
    }

    /// <summary>
    /// Writes a byte to the Game Boy memory map.
    /// </summary>
    public void WriteByte(ushort addr, byte value)
    {
        if (addr < 0x8000)
        {
            Cartridge?.WriteRom(addr, value);
            return;
        }
        _mem[addr] = value;
    }

    /// <summary>
    /// Convenience method to load a ROM into cartridge.
    /// </summary>
    public void LoadRom(byte[] rom)
    {
        Cartridge = Cartridge.Detect(rom);
    }
}
