using System;

namespace GameBoy.Core;

/// <summary>
/// Game Boy Noise Channel (LFSR pseudo-random noise).
/// </summary>
public sealed class Noise : IAudioChannel
{
    // Channel state
    private bool _enabled;
    private bool _dacEnabled;

    // NR41 - Length
    private byte _lengthLoad;
    private int _lengthCounter;
    private bool _lengthEnabled;

    // NR42 - Volume envelope
    private byte _initialVolume;
    private bool _envelopeDirection; // true = increase, false = decrease
    private byte _envelopePeriod;
    private int _currentVolume;
    private int _envelopeTimer;

    // NR43 - Polynomial counter
    private byte _clockShift;
    private bool _widthMode; // true = 7-bit, false = 15-bit
    private byte _divisorCode;
    private int _frequencyTimer;

    // LFSR state
    private ushort _lfsr = 0x7FFF;

    public bool Enabled => _enabled && _dacEnabled;

    public Noise()
    {
        Reset();
    }

    public void Reset()
    {
        _enabled = false;
        _dacEnabled = false;
        _lengthLoad = 0;
        _lengthCounter = 0;
        _lengthEnabled = false;
        _initialVolume = 0;
        _envelopeDirection = false;
        _envelopePeriod = 0;
        _currentVolume = 0;
        _envelopeTimer = 0;
        _clockShift = 0;
        _widthMode = false;
        _divisorCode = 0;
        _frequencyTimer = 0;
        _lfsr = 0x7FFF;
    }

    public void Step(int cycles)
    {
        if (!_enabled)
            return;

        // Step frequency timer
        _frequencyTimer -= cycles;
        while (_frequencyTimer <= 0)
        {
            _frequencyTimer += GetPeriod();
            StepLfsr();
        }
    }

    private int GetPeriod()
    {
        int divisor = _divisorCode switch
        {
            0 => 8,
            1 => 16,
            2 => 32,
            3 => 48,
            4 => 64,
            5 => 80,
            6 => 96,
            7 => 112,
            _ => 8
        };

        return divisor << _clockShift;
    }

    private void StepLfsr()
    {
        // XOR bits 0 and 1
        int xorResult = (_lfsr & 1) ^ ((_lfsr >> 1) & 1);

        // Shift right
        _lfsr >>= 1;

        // Set bit 14
        _lfsr |= (ushort)(xorResult << 14);

        // In 7-bit mode, also set bit 6
        if (_widthMode)
        {
            _lfsr = (ushort)((_lfsr & ~(1 << 6)) | (xorResult << 6));
        }
    }

    public float GetSample()
    {
        if (!Enabled || _currentVolume == 0)
            return 0;

        // Output is inverted bit 0 of LFSR
        float output = (~_lfsr & 1) * (_currentVolume / 15.0f);
        return output;
    }

    public void Trigger()
    {
        _enabled = true;

        // Length counter
        if (_lengthCounter == 0)
            _lengthCounter = 64;

        // Frequency timer
        _frequencyTimer = GetPeriod();

        // Volume envelope
        _currentVolume = _initialVolume;
        _envelopeTimer = _envelopePeriod;

        // LFSR
        _lfsr = 0x7FFF;

        // Check DAC
        _dacEnabled = (_initialVolume > 0) || _envelopeDirection;
        if (!_dacEnabled)
            _enabled = false;
    }

    public void StepLengthCounter()
    {
        if (_lengthEnabled && _lengthCounter > 0)
        {
            _lengthCounter--;
            if (_lengthCounter == 0)
                _enabled = false;
        }
    }

    public void StepVolumeEnvelope()
    {
        if (_envelopePeriod == 0)
            return;

        if (_envelopeTimer > 0)
        {
            _envelopeTimer--;
            if (_envelopeTimer == 0)
            {
                _envelopeTimer = _envelopePeriod;

                if (_envelopeDirection && _currentVolume < 15)
                    _currentVolume++;
                else if (!_envelopeDirection && _currentVolume > 0)
                    _currentVolume--;
            }
        }
    }

    // Register access methods
    public byte ReadNR41() => 0xFF; // Write-only
    public byte ReadNR42() => (byte)((_initialVolume << 4) | (_envelopeDirection ? 0x08 : 0x00) | _envelopePeriod);
    public byte ReadNR43() => (byte)((_clockShift << 4) | (_widthMode ? 0x08 : 0x00) | _divisorCode);
    public byte ReadNR44() => (byte)(0xBF | (_lengthEnabled ? 0x40 : 0x00));

    public void WriteNR41(byte value)
    {
        _lengthLoad = (byte)(value & 0x3F);
        _lengthCounter = 64 - _lengthLoad;
    }

    public void WriteNR42(byte value)
    {
        _initialVolume = (byte)((value >> 4) & 0x0F);
        _envelopeDirection = (value & 0x08) != 0;
        _envelopePeriod = (byte)(value & 0x07);

        _dacEnabled = (_initialVolume > 0) || _envelopeDirection;
        if (!_dacEnabled)
            _enabled = false;
    }

    public void WriteNR43(byte value)
    {
        _clockShift = (byte)((value >> 4) & 0x0F);
        _widthMode = (value & 0x08) != 0;
        _divisorCode = (byte)(value & 0x07);
    }

    public void WriteNR44(byte value)
    {
        _lengthEnabled = (value & 0x40) != 0;

        if ((value & 0x80) != 0)
            Trigger();
    }
}