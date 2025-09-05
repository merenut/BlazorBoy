using System;

namespace GameBoy.Core;

/// <summary>
/// Base cartridge abstraction and factory.
/// </summary>
public abstract class Cartridge
{
    public byte[] Rom { get; }

    protected Cartridge(byte[] rom)
    {
        Rom = rom;
    }

    /// <summary>
    /// Reads a byte from the ROM address space (0x0000-0x7FFF).
    /// </summary>
    public abstract byte ReadRom(ushort addr);

    /// <summary>
    /// Writes a byte to ROM address space (usually bank switching control).
    /// </summary>
    public abstract void WriteRom(ushort addr, byte value);

    /// <summary>
    /// Detects and creates the correct cartridge controller (MBC).
    /// </summary>
    public static Cartridge Detect(byte[] rom)
    {
        // Very simple: use MBC0 for now.
        return new Mbc0(rom);
    }
}
