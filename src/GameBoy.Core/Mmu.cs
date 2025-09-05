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

    private readonly byte[] _mem = new byte[AddressSpaceSize];

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
