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
    public const ushort IoStart = 0xFF00;
    public const ushort IoEnd = 0xFF7F;
    public const ushort HramStart = 0xFF80;
    public const ushort HramEnd = 0xFFFE;
    public const ushort InterruptEnable = 0xFFFF;

    // I/O Register addresses
    public const ushort JOYP = 0xFF00;
    public const ushort DIV = 0xFF04;
    public const ushort TIMA = 0xFF05;
    public const ushort TMA = 0xFF06;
    public const ushort TAC = 0xFF07;
    public const ushort IF = 0xFF0F;
    public const ushort LCDC = 0xFF40;
    public const ushort STAT = 0xFF41;
    public const ushort SCY = 0xFF42;
    public const ushort SCX = 0xFF43;
    public const ushort LY = 0xFF44;
    public const ushort LYC = 0xFF45;
    public const ushort DMA = 0xFF46;
    public const ushort BGP = 0xFF47;
    public const ushort OBP0 = 0xFF48;
    public const ushort OBP1 = 0xFF49;
    public const ushort WY = 0xFF4A;
    public const ushort WX = 0xFF4B;

    private readonly byte[] _mem = new byte[AddressSpaceSize];

    // I/O Register storage - post-BIOS defaults
    private byte _joyp = 0xCF;
    private byte _div = 0x00;
    private byte _tima = 0x00;
    private byte _tma = 0x00;
    private byte _tac = 0xF8;
    private byte _if = 0xE1;
    private byte _lcdc = 0x91;
    private byte _stat = 0x85;
    private byte _scy = 0x00;
    private byte _scx = 0x00;
    private byte _ly = 0x00;
    private byte _lyc = 0x00;
    private byte _dma = 0xFF;
    private byte _bgp = 0xFC;
    private byte _obp0 = 0x00;
    private byte _obp1 = 0x00;
    private byte _wy = 0x00;
    private byte _wx = 0x00;

    public Cartridge? Cartridge { get; set; }

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

        // Handle I/O registers
        if (addr >= IoStart && addr <= IoEnd)
        {
            return ReadIoRegister(addr);
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

        // Handle I/O registers
        if (addr >= IoStart && addr <= IoEnd)
        {
            WriteIoRegister(addr, value);
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

    /// <summary>
    /// Reads from I/O registers with proper stubbing behavior.
    /// </summary>
    private byte ReadIoRegister(ushort addr)
    {
        return addr switch
        {
            JOYP => (byte)(_joyp | 0x0F), // Lower 4 bits always read as 1s
            DIV => _div,
            TIMA => _tima,
            TMA => _tma,
            TAC => _tac,
            IF => (byte)(_if | 0xE0), // Upper 3 bits always read as 1s
            LCDC => _lcdc,
            STAT => _stat,
            SCY => _scy,
            SCX => _scx,
            LY => _ly,
            LYC => _lyc,
            DMA => _dma,
            BGP => _bgp,
            OBP0 => _obp0,
            OBP1 => _obp1,
            WY => _wy,
            WX => _wx,
            _ => 0xFF // All other I/O registers read as 0xFF
        };
    }

    /// <summary>
    /// Writes to I/O registers with proper stubbing behavior.
    /// </summary>
    private void WriteIoRegister(ushort addr, byte value)
    {
        switch (addr)
        {
            case JOYP:
                // Only bits 4-5 are writable
                _joyp = (byte)((_joyp & 0xCF) | (value & 0x30));
                break;
            case DIV:
                // Writing any value resets DIV to 0x00
                _div = 0x00;
                break;
            case TIMA:
                _tima = value;
                break;
            case TMA:
                _tma = value;
                break;
            case TAC:
                _tac = value;
                break;
            case IF:
                // Only lower 5 bits are writable
                _if = (byte)(value & 0x1F);
                break;
            case LCDC:
                _lcdc = value;
                break;
            case STAT:
                // Only bits 6, 5, 4, 3 are writable (0x78 mask)
                _stat = (byte)((_stat & 0x87) | (value & 0x78));
                break;
            case SCY:
                _scy = value;
                break;
            case SCX:
                _scx = value;
                break;
            case LY:
                // LY is read-only, writes are ignored
                break;
            case LYC:
                _lyc = value;
                break;
            case DMA:
                // DMA writes are latched, no transfer logic yet
                _dma = value;
                break;
            case BGP:
                _bgp = value;
                break;
            case OBP0:
                _obp0 = value;
                break;
            case OBP1:
                _obp1 = value;
                break;
            case WY:
                _wy = value;
                break;
            case WX:
                _wx = value;
                break;
            // All other I/O register writes are ignored
        }
    }
}
