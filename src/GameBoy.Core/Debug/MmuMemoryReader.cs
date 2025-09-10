namespace GameBoy.Core.Debug;

/// <summary>
/// Adapter to make MMU compatible with IMemoryReader interface.
/// </summary>
public sealed class MmuMemoryReader : IMemoryReader
{
    private readonly Mmu _mmu;

    public MmuMemoryReader(Mmu mmu)
    {
        _mmu = mmu ?? throw new System.ArgumentNullException(nameof(mmu));
    }

    public byte ReadByte(ushort address)
    {
        return _mmu.ReadByte(address);
    }
}