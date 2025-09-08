using System;

namespace GameBoy.Core;

/// <summary>
/// MBC3 (Memory Bank Controller 3) implementation.
/// Supports ROM banking (1-127), RAM banking (0-3), and Real Time Clock (stub).
/// </summary>
public sealed class Mbc3 : Cartridge, IBatteryBacked
{
    private readonly CartridgeHeader _header;
    private readonly byte[] _externalRam;
    private readonly byte[] _rtcRegisters = new byte[5]; // RTC registers (S, M, H, DL, DH)

    // MBC3 state
    private bool _ramAndTimerEnabled;
    private int _romBank = 1;           // ROM bank (1-127, bank 0 maps to 1)
    private int _ramBankOrRtc;          // RAM bank (0-3) or RTC register (0x08-0x0C)

    // RTC state (stub implementation)
    private bool _rtcLatched;
    private long _rtcLatchTime;

    public bool HasBattery => _header.HasBattery();
    public int ExternalRamSize => _header.RamSize;
    public bool HasRtc => _header.HasRtc();

    public Mbc3(byte[] rom, CartridgeHeader header) : base(rom)
    {
        _header = header ?? throw new ArgumentNullException(nameof(header));
        _externalRam = new byte[_header.RamSize];

        // Initialize RTC registers to reasonable defaults
        InitializeRtc();
    }

    private void InitializeRtc()
    {
        // Stub RTC implementation - just set to epoch time
        var now = DateTime.UtcNow;
        _rtcRegisters[0] = (byte)(now.Second % 60);      // Seconds (0-59)
        _rtcRegisters[1] = (byte)(now.Minute % 60);      // Minutes (0-59) 
        _rtcRegisters[2] = (byte)(now.Hour % 24);        // Hours (0-23)
        _rtcRegisters[3] = (byte)(now.DayOfYear & 0xFF); // Day Low (0-255)
        _rtcRegisters[4] = (byte)((now.DayOfYear >> 8) & 0x01); // Day High bit + flags
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
            // ROM Bank 1-127 (switchable)
            int effectiveBank = _romBank;

            // Ensure bank 0 maps to bank 1
            if (effectiveBank == 0)
                effectiveBank = 1;

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
                // RAM and Timer Enable (0x0000-0x1FFF)
                _ramAndTimerEnabled = (value & 0x0F) == 0x0A;
                break;

            case 0x2000:
            case 0x3000:
                // ROM Bank Number (0x2000-0x3FFF)
                _romBank = value & 0x7F; // 7-bit ROM bank (0-127)

                // Bank 0 maps to bank 1
                if (_romBank == 0)
                    _romBank = 1;
                break;

            case 0x4000:
            case 0x5000:
                // RAM Bank Number or RTC Register Select (0x4000-0x5FFF)
                _ramBankOrRtc = value;
                break;

            case 0x6000:
            case 0x7000:
                // Latch Clock Data (0x6000-0x7FFF)
                // Writing 0x00 then 0x01 latches current time to RTC registers
                if (HasRtc)
                {
                    if (!_rtcLatched && value == 0x01)
                    {
                        _rtcLatched = true;
                        _rtcLatchTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        UpdateRtcRegisters();
                    }
                    else if (value == 0x00)
                    {
                        _rtcLatched = false;
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Reads a byte from external RAM or RTC registers (0xA000-0xBFFF).
    /// </summary>
    public override byte ReadExternalRam(ushort addr)
    {
        if (!_ramAndTimerEnabled)
            return 0xFF;

        if (_ramBankOrRtc <= 0x03)
        {
            // RAM bank (0-3)
            if (_header.RamSize == 0)
                return 0xFF;

            int ramAddr = (_ramBankOrRtc * 0x2000) + (addr - 0xA000);

            if (ramAddr >= _externalRam.Length)
                return 0xFF;

            return _externalRam[ramAddr];
        }
        else if (HasRtc && _ramBankOrRtc >= 0x08 && _ramBankOrRtc <= 0x0C)
        {
            // RTC register (0x08-0x0C)
            int rtcIndex = _ramBankOrRtc - 0x08;
            return _rtcRegisters[rtcIndex];
        }

        return 0xFF;
    }

    /// <summary>
    /// Writes a byte to external RAM or RTC registers (0xA000-0xBFFF).
    /// </summary>
    public override void WriteExternalRam(ushort addr, byte value)
    {
        if (!_ramAndTimerEnabled)
            return;

        if (_ramBankOrRtc <= 0x03)
        {
            // RAM bank (0-3)
            if (_header.RamSize == 0)
                return;

            int ramAddr = (_ramBankOrRtc * 0x2000) + (addr - 0xA000);

            if (ramAddr < _externalRam.Length)
                _externalRam[ramAddr] = value;
        }
        else if (HasRtc && _ramBankOrRtc >= 0x08 && _ramBankOrRtc <= 0x0C)
        {
            // RTC register (0x08-0x0C)
            int rtcIndex = _ramBankOrRtc - 0x08;
            _rtcRegisters[rtcIndex] = value;
        }
    }

    private void UpdateRtcRegisters()
    {
        // Stub implementation - just increment from initial values
        // In a full implementation, this would calculate actual elapsed time
        var now = DateTime.UtcNow;
        _rtcRegisters[0] = (byte)(now.Second % 60);
        _rtcRegisters[1] = (byte)(now.Minute % 60);
        _rtcRegisters[2] = (byte)(now.Hour % 24);
        _rtcRegisters[3] = (byte)(now.DayOfYear & 0xFF);
        _rtcRegisters[4] = (byte)((now.DayOfYear >> 8) & 0x01);
    }

    public byte[]? GetExternalRam()
    {
        if (_header.RamSize == 0 || !HasBattery)
            return null;

        // For MBC3, we need to save both RAM and RTC data
        // Format: [RAM data][RTC data]
        int totalSize = _externalRam.Length + (HasRtc ? 5 : 0);

        // Check if there's any data to save
        bool hasData = false;
        for (int i = 0; i < _externalRam.Length; i++)
        {
            if (_externalRam[i] != 0)
            {
                hasData = true;
                break;
            }
        }

        if (HasRtc)
        {
            for (int i = 0; i < _rtcRegisters.Length; i++)
            {
                if (_rtcRegisters[i] != 0)
                {
                    hasData = true;
                    break;
                }
            }
        }

        if (!hasData)
            return null;

        byte[] data = new byte[totalSize];
        Array.Copy(_externalRam, 0, data, 0, _externalRam.Length);

        if (HasRtc)
        {
            Array.Copy(_rtcRegisters, 0, data, _externalRam.Length, 5);
        }

        return data;
    }

    public void LoadExternalRam(byte[]? data)
    {
        if (data == null || _header.RamSize == 0)
            return;

        // Load RAM data
        int ramCopyLength = Math.Min(data.Length, _externalRam.Length);
        Array.Copy(data, _externalRam, ramCopyLength);

        // Load RTC data if present
        if (HasRtc && data.Length > _externalRam.Length)
        {
            int rtcDataStart = _externalRam.Length;
            int rtcCopyLength = Math.Min(data.Length - rtcDataStart, 5);
            Array.Copy(data, rtcDataStart, _rtcRegisters, 0, rtcCopyLength);
        }
    }
}