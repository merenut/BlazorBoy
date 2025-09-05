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
    public const ushort UnusableStart = 0xFEA0;
    public const ushort UnusableEnd = 0xFEFF;
    public const ushort IoStart = 0xFF00;
    public const ushort IoEnd = 0xFF7F;
    public const ushort HramStart = 0xFF80;
    public const ushort HramEnd = 0xFFFE;
    public const ushort InterruptEnable = 0xFFFF;

    private readonly byte[] _mem = new byte[AddressSpaceSize];

    public Cartridge? Cartridge { get; set; }

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
}
