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
    /// Reads a byte from external RAM address space (0xA000-0xBFFF).
    /// </summary>
    public virtual byte ReadExternalRam(ushort addr)
    {
        // Default implementation for cartridges without external RAM
        return 0xFF;
    }

    /// <summary>
    /// Writes a byte to external RAM address space (0xA000-0xBFFF).
    /// </summary>
    public virtual void WriteExternalRam(ushort addr, byte value)
    {
        // Default implementation for cartridges without external RAM
        // Writes are ignored
    }

    /// <summary>
    /// Detects and creates the correct cartridge controller (MBC).
    /// </summary>
    public static Cartridge Detect(byte[] rom)
    {
        if (rom == null || rom.Length < 0x0150)
            throw new ArgumentException("ROM too small to contain valid header", nameof(rom));

        var header = CartridgeHeader.Parse(rom);
        var mbcType = header.GetMbcType();

        return mbcType switch
        {
            MbcType.None => new Mbc0(rom),
            MbcType.Mbc1 => new Mbc1(rom, header),
            MbcType.Mbc3 => new Mbc3(rom, header),
            MbcType.Mbc5 => new Mbc5(rom, header),
            MbcType.Mbc2 => throw new NotSupportedException($"MBC2 is not yet supported (cartridge type: 0x{header.CartridgeType:X2})"),
            MbcType.Unknown => throw new NotSupportedException($"Unknown or unsupported cartridge type: 0x{header.CartridgeType:X2}"),
            _ => throw new NotSupportedException($"Unsupported MBC type: {mbcType}")
        };
    }
}
