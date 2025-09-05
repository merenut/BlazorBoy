namespace GameBoy.Core;

/// <summary>
/// No MBC (ROM only) cartridge.
/// </summary>
public sealed class Mbc0 : Cartridge
{
    public Mbc0(byte[] rom) : base(rom)
    {
    }

    public override byte ReadRom(ushort addr)
    {
        return Rom[addr];
    }

    public override void WriteRom(ushort addr, byte value)
    {
        // No effect for ROM-only cartridges.
    }
}
