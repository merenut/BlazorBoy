using System;

namespace GameBoy.Core;

/// <summary>
/// MBC5 (Memory Bank Controller 5) implementation.
/// Supports large ROM banking (0-511 with 9-bit addressing) and RAM banking (0-15).
/// </summary>
public sealed class Mbc5 : Cartridge, IBatteryBacked
{
    private readonly CartridgeHeader _header;
    private readonly byte[] _externalRam;

    // MBC5 state
    private bool _ramEnabled;
    private int _romBank;               // ROM bank (0-511, 9-bit)
    private int _ramBank;               // RAM bank (0-15, 4-bit)

    public bool HasBattery => _header.HasBattery();
    public int ExternalRamSize => _header.RamSize;

    public Mbc5(byte[] rom, CartridgeHeader header) : base(rom)
    {
        _header = header ?? throw new ArgumentNullException(nameof(header));
        _externalRam = new byte[_header.RamSize];
    }

    public override byte ReadRom(ushort addr)
    {
        if (addr <= 0x3FFF)
        {
            // ROM Bank 0 (fixed)
            if (addr >= Rom.Length)
                return 0xFF;

            return Rom[addr];
        }
        else
        {
            // ROM Bank 0-511 (switchable)
            // Unlike MBC1/MBC3, MBC5 allows bank 0 to be selected
            int effectiveBank = _romBank;
            int romAddr = (effectiveBank * 0x4000) + (addr - 0x4000);

            if (romAddr >= Rom.Length)
                return 0xFF;

            return Rom[romAddr];
        }
    }

    public override void WriteRom(ushort addr, byte value)
    {
        switch (addr & 0xF000)
        {
            case 0x0000:
            case 0x1000:
                // RAM Enable (0x0000-0x1FFF)
                _ramEnabled = (value & 0x0F) == 0x0A;
                break;

            case 0x2000:
                // ROM Bank Number Low 8 bits (0x2000-0x2FFF)
                _romBank = (_romBank & 0x100) | value;
                break;

            case 0x3000:
                // ROM Bank Number High bit (0x3000-0x3FFF)
                // Only bit 0 is used (9th bit of ROM bank)
                _romBank = (_romBank & 0xFF) | ((value & 0x01) << 8);
                break;

            case 0x4000:
            case 0x5000:
                // RAM Bank Number (0x4000-0x5FFF)
                // 4-bit RAM bank (0-15)
                _ramBank = value & 0x0F;
                break;

                // MBC5 doesn't use 0x6000-0x7FFF range like MBC1/MBC3
        }
    }

    /// <summary>
    /// Reads a byte from external RAM (0xA000-0xBFFF).
    /// </summary>
    public override byte ReadExternalRam(ushort addr)
    {
        if (!_ramEnabled || _header.RamSize == 0)
            return 0xFF;

        int ramAddr = (_ramBank * 0x2000) + (addr - 0xA000);

        if (ramAddr >= _externalRam.Length)
            return 0xFF;

        return _externalRam[ramAddr];
    }

    /// <summary>
    /// Writes a byte to external RAM (0xA000-0xBFFF).
    /// </summary>
    public override void WriteExternalRam(ushort addr, byte value)
    {
        if (!_ramEnabled || _header.RamSize == 0)
            return;

        int ramAddr = (_ramBank * 0x2000) + (addr - 0xA000);

        if (ramAddr < _externalRam.Length)
            _externalRam[ramAddr] = value;
    }

    public byte[]? GetExternalRam()
    {
        if (_header.RamSize == 0 || !HasBattery)
            return null;

        // Only return RAM data if it has been modified (contains non-zero data)
        for (int i = 0; i < _externalRam.Length; i++)
        {
            if (_externalRam[i] != 0)
            {
                byte[] data = new byte[_externalRam.Length];
                Array.Copy(_externalRam, data, _externalRam.Length);
                return data;
            }
        }

        return null;
    }

    public void LoadExternalRam(byte[]? data)
    {
        if (data == null || _header.RamSize == 0)
            return;

        int copyLength = Math.Min(data.Length, _externalRam.Length);
        Array.Copy(data, _externalRam, copyLength);
    }
}