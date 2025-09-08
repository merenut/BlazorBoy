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

    // I/O Register addresses - using IoRegs constants to avoid duplication
    public const ushort JOYP = IoRegs.P1_JOYP;
    public const ushort DIV = IoRegs.DIV;
    public const ushort TIMA = IoRegs.TIMA;
    public const ushort TMA = IoRegs.TMA;
    public const ushort TAC = IoRegs.TAC;
    public const ushort IF = IoRegs.IF;
    public const ushort LCDC = IoRegs.LCDC;
    public const ushort STAT = IoRegs.STAT;
    public const ushort SCY = IoRegs.SCY;
    public const ushort SCX = IoRegs.SCX;
    public const ushort LY = IoRegs.LY;
    public const ushort LYC = IoRegs.LYC;
    public const ushort DMA = IoRegs.DMA;
    public const ushort BGP = IoRegs.BGP;
    public const ushort OBP0 = IoRegs.OBP0;
    public const ushort OBP1 = IoRegs.OBP1;
    public const ushort WY = IoRegs.WY;
    public const ushort WX = IoRegs.WX;

    private readonly byte[] _mem = new byte[AddressSpaceSize];

    // I/O Register storage - post-BIOS defaults
    private byte _joyp = 0xCF;
    private byte _div = 0x00;
    private byte _tima = 0x00;
    private byte _tma = 0x00;
    private byte _tac = 0xF8;
    private byte _if = 0x01; // Only lower 5 bits stored; upper 3 bits added during read
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
        // Reset I/O register private fields to default values
        // These registers have backing fields and are accessed via ReadIoRegister/WriteIoRegister
        _joyp = 0xCF;
        _div = 0x00;
        _tima = 0x00;
        _tma = 0x00;
        _tac = 0xF8;
        _if = 0x01; // Only lower 5 bits stored; upper 3 bits added during read
        _lcdc = 0x91;
        _stat = 0x85;
        _scy = 0x00;
        _scx = 0x00;
        _ly = 0x00;
        _lyc = 0x00;
        _dma = 0xFF;
        _bgp = 0xFC;
        _obp0 = 0x00;
        _obp1 = 0x00;
        _wy = 0x00;
        _wx = 0x00;

        // IE register (outside I/O range) - accessed via direct memory
        _mem[0xFFFF] = 0x00; // IE - Interrupt enable

        // Note: Serial transfer and sound registers are not implemented in ReadIoRegister
        // and therefore read as 0xFF. Their defaults are not set here to avoid confusion.
        // When these registers are properly implemented, their defaults should be:
        // Serial: SB=0x00, SC=0x7E  
        // Sound: NR10=0x80, NR11=0xBF, NR12=0xF3, NR14=0xBF, NR21=0x3F, NR22=0x00,
        //        NR24=0xBF, NR30=0x7F, NR31=0xFF, NR32=0x9F, NR34=0xBF, NR41=0xFF,
        //        NR42=0x00, NR43=0x00, NR44=0xBF, NR50=0x77, NR51=0xF3, NR52=0xF1
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

        // External RAM (0xA000-0xBFFF) - route through cartridge
        if (addr >= ExtRamStart && addr <= ExtRamEnd)
        {
            if (Cartridge is null) return 0xFF;
            return Cartridge.ReadExternalRam(addr);
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

        // I/O region (0xFF00-0xFF7F) - use proper I/O register handling
        if (addr >= IoStart && addr <= IoEnd)
        {
            return ReadIoRegister(addr);
        }

        // All other regions (VRAM, Work RAM, OAM, HRAM, IE)
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

        // External RAM (0xA000-0xBFFF) - route through cartridge
        if (addr >= ExtRamStart && addr <= ExtRamEnd)
        {
            Cartridge?.WriteExternalRam(addr, value);
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

        // I/O region (0xFF00-0xFF7F) - use proper I/O register handling
        if (addr >= IoStart && addr <= IoEnd)
        {
            WriteIoRegister(addr, value);
            return;
        }

        // All other regions (VRAM, Work RAM, OAM, HRAM, IE)
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
