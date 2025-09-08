using System;

namespace GameBoy.Core;

/// <summary>
/// MBC1 (Memory Bank Controller 1) implementation.
/// Supports ROM banking (1-127), RAM banking (0-3), and mode switching.
/// </summary>
public sealed class Mbc1 : Cartridge, IBatteryBacked
{
    private readonly CartridgeHeader _header;
    private readonly byte[] _externalRam;

    // MBC1 state
    private bool _ramEnabled;
    private int _romBank = 1;           // ROM bank (1-127, bank 0 maps to 1)
    private int _ramBank;               // RAM bank (0-3)
    private bool _advancedBankingMode;  // false = Mode 0 (16Mbit ROM/8KB RAM), true = Mode 1 (4Mbit ROM/32KB RAM)

    public bool HasBattery => _header.HasBattery();
    public int ExternalRamSize => _header.RamSize;

    public Mbc1(byte[] rom, CartridgeHeader header) : base(rom)
    {
        _header = header ?? throw new ArgumentNullException(nameof(header));
        _externalRam = new byte[_header.RamSize];
    }

    public override byte ReadRom(ushort addr)
    {
        if (addr <= 0x3FFF)
        {
            // ROM Bank 0 (fixed)
            // In advanced banking mode, this can be banked to 0x20, 0x40, 0x60
            int effectiveBank = _advancedBankingMode ? (_ramBank << 5) : 0;
            int romAddr = (effectiveBank * 0x4000) + addr;

            if (romAddr >= Rom.Length)
                return 0xFF;

            return Rom[romAddr];
        }
        else
        {
            // ROM Bank 1-127 (switchable)
            int effectiveBank = _romBank;
            if (_advancedBankingMode)
            {
                // In advanced mode, upper 2 bits come from RAM bank selection
                effectiveBank = (_ramBank << 5) | (_romBank & 0x1F);
            }

            // Ensure bank 0 maps to bank 1
            if ((effectiveBank & 0x1F) == 0)
                effectiveBank |= 1;

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
            case 0x3000:
                // ROM Bank Number (0x2000-0x3FFF)
                // Lower 5 bits of ROM bank number
                _romBank = (_romBank & 0x60) | (value & 0x1F);

                // Bank 0 maps to bank 1
                if ((_romBank & 0x1F) == 0)
                    _romBank |= 1;
                break;

            case 0x4000:
            case 0x5000:
                // RAM Bank Number / Upper ROM Bank bits (0x4000-0x5FFF)
                _ramBank = value & 0x03;

                if (!_advancedBankingMode)
                {
                    // In simple mode, these bits are upper ROM bank bits
                    _romBank = (_romBank & 0x1F) | ((_ramBank & 0x03) << 5);
                }
                break;

            case 0x6000:
            case 0x7000:
                // Banking Mode Select (0x6000-0x7FFF)
                _advancedBankingMode = (value & 0x01) != 0;
                break;
        }
    }

    /// <summary>
    /// Reads a byte from external RAM (0xA000-0xBFFF).
    /// </summary>
    public override byte ReadExternalRam(ushort addr)
    {
        if (!_ramEnabled || _header.RamSize == 0)
            return 0xFF;

        int ramAddr;
        if (_advancedBankingMode && _header.RamSize > 0x2000)
        {
            // Advanced mode: RAM banking enabled for 32KB RAM
            ramAddr = (_ramBank * 0x2000) + (addr - 0xA000);
        }
        else
        {
            // Simple mode: only first 8KB accessible
            ramAddr = addr - 0xA000;
        }

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

        int ramAddr;
        if (_advancedBankingMode && _header.RamSize > 0x2000)
        {
            // Advanced mode: RAM banking enabled for 32KB RAM
            ramAddr = (_ramBank * 0x2000) + (addr - 0xA000);
        }
        else
        {
            // Simple mode: only first 8KB accessible
            ramAddr = addr - 0xA000;
        }

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