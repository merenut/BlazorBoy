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
    public const ushort UnusableStart = 0xFEA0;
    public const ushort UnusableEnd = 0xFEFF;
    public const ushort IoEnd = 0xFF7F;
    public const ushort HramStart = 0xFF80;
    public const ushort HramEnd = 0xFFFE;
    public const ushort InterruptEnable = IoRegs.IE; // 0xFFFF

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
        // Cartridge ROM (0x0000-0x7FFF)
        if (addr <= RomXEnd)
        {
            if (Cartridge is null) return 0xFF;
            return Cartridge.ReadRom(addr);
        }

        // Echo RAM (0xE000-0xFDFF) mirrors Work RAM (0xC000-0xDDFF)
        if (addr >= EchoRamStart && addr <= EchoRamEnd)
        {
            ushort mirrorAddr = (ushort)(addr - 0x2000); // Map 0xE000-0xFDFF to 0xC000-0xDDFF
            return _mem[mirrorAddr];
        }

        // Unusable region (0xFEA0-0xFEFF) always returns 0xFF
        if (addr >= UnusableStart && addr <= UnusableEnd)
        {
            return 0xFF;
        }

        // I/O region (0xFF00-0xFF7F) - unmapped addresses return 0xFF
        if (addr >= IoStart && addr <= IoEnd)
        {
            // For now, all I/O addresses return 0xFF since they're not implemented
            return 0xFF;
        }

        // All other regions (VRAM, External RAM, Work RAM, OAM, HRAM, IE)
        return _mem[addr];
    }

    /// <summary>
    /// Writes a byte to the Game Boy memory map.
    /// </summary>
    public void WriteByte(ushort addr, byte value)
    {
        // Cartridge ROM (0x0000-0x7FFF)
        if (addr <= RomXEnd)
        {
            Cartridge?.WriteRom(addr, value);
            return;
        }

        // Echo RAM (0xE000-0xFDFF) mirrors Work RAM (0xC000-0xDDFF)
        if (addr >= EchoRamStart && addr <= EchoRamEnd)
        {
            ushort mirrorAddr = (ushort)(addr - 0x2000); // Map 0xE000-0xFDFF to 0xC000-0xDDFF
            _mem[mirrorAddr] = value;
            return;
        }

        // Unusable region (0xFEA0-0xFEFF) - writes are ignored
        if (addr >= UnusableStart && addr <= UnusableEnd)
        {
            return; // Ignore writes
        }

        // I/O region (0xFF00-0xFF7F) - for now, just latch writes (store in memory)
        if (addr >= IoStart && addr <= IoEnd)
        {
            _mem[addr] = value;
            return;
        }

        // All other regions (VRAM, External RAM, Work RAM, OAM, HRAM, IE)
        _mem[addr] = value;
    }

    /// <summary>
    /// Reads a 16-bit word from the Game Boy memory map in little-endian order.
    /// </summary>
    public ushort ReadWord(ushort addr)
    {
        byte lowByte = ReadByte(addr);
        byte highByte = ReadByte((ushort)(addr + 1));
        return (ushort)(lowByte | (highByte << 8));
    }

    /// <summary>
    /// Writes a 16-bit word to the Game Boy memory map in little-endian order.
    /// </summary>
    public void WriteWord(ushort addr, ushort value)
    {
        byte lowByte = (byte)(value & 0xFF);
        byte highByte = (byte)(value >> 8);
        WriteByte(addr, lowByte);
        WriteByte((ushort)(addr + 1), highByte);
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
