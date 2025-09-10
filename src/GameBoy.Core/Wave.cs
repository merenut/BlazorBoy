using System;

namespace GameBoy.Core;

/// <summary>
/// Game Boy Wave Channel (programmable waveform).
/// </summary>
public sealed class Wave : IAudioChannel
{
    // Wave RAM (32 4-bit samples stored in 16 bytes)
    private readonly byte[] _waveRam = new byte[16];

    // Channel state
    private bool _enabled;
    private bool _dacEnabled;

    // NR31 - Length
    private byte _lengthLoad;
    private int _lengthCounter;
    private bool _lengthEnabled;

    // NR32 - Output level
    private byte _outputLevel;

    // NR33/NR34 - Frequency
    private ushort _frequency;
    private int _frequencyTimer;
    private int _wavePosition;

    public bool Enabled => _enabled && _dacEnabled;

    public Wave()
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
        _outputLevel = 0;
        _frequency = 0;
        _frequencyTimer = 0;
        _wavePosition = 0;
        Array.Clear(_waveRam);
    }

    public void Step(int cycles)
    {
        if (!_enabled)
            return;

        // Step frequency timer
        _frequencyTimer -= cycles;
        while (_frequencyTimer <= 0)
        {
            _frequencyTimer += (2048 - _frequency) * 2;
            _wavePosition = (_wavePosition + 1) % 32;
        }
    }

    public float GetSample()
    {
        if (!Enabled || _outputLevel == 0)
            return 0;

        // Get 4-bit sample from wave RAM
        int byteIndex = _wavePosition / 2;
        bool isHighNibble = (_wavePosition % 2) == 0;

        byte sample;
        if (isHighNibble)
            sample = (byte)((_waveRam[byteIndex] >> 4) & 0x0F);
        else
            sample = (byte)(_waveRam[byteIndex] & 0x0F);

        // Apply output level (volume shift)
        int shift = _outputLevel switch
        {
            0 => 4, // Mute
            1 => 0, // 100%
            2 => 1, // 50%
            3 => 2, // 25%
            _ => 4  // Mute
        };

        if (shift >= 4)
            return 0;

        float normalizedSample = (sample >> shift) / 15.0f;
        return normalizedSample;
    }

    public void Trigger()
    {
        _enabled = true;

        // Length counter
        if (_lengthCounter == 0)
            _lengthCounter = 256;

        // Frequency timer
        _frequencyTimer = (2048 - _frequency) * 2;

        // Reset wave position
        _wavePosition = 0;

        // Check DAC
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
        // Wave channel has no volume envelope
    }

    // Wave RAM access
    public byte ReadWaveRam(int index)
    {
        if (index < 0 || index >= 16)
            return 0xFF;

        // If wave channel is playing, can only read currently playing sample
        if (_enabled)
        {
            int currentByte = _wavePosition / 2;
            if (index == currentByte)
                return _waveRam[index];
            else
                return 0xFF;
        }

        return _waveRam[index];
    }

    public void WriteWaveRam(int index, byte value)
    {
        if (index < 0 || index >= 16)
            return;

        // If wave channel is playing, can only write to currently playing sample
        if (_enabled)
        {
            int currentByte = _wavePosition / 2;
            if (index == currentByte)
                _waveRam[index] = value;
        }
        else
        {
            _waveRam[index] = value;
        }
    }

    // Register access methods
    public byte ReadNR30() => (byte)(0x7F | (_dacEnabled ? 0x80 : 0x00));
    public byte ReadNR31() => 0xFF; // Write-only
    public byte ReadNR32() => (byte)(0x9F | (_outputLevel << 5));
    public byte ReadNR33() => 0xFF; // Write-only
    public byte ReadNR34() => (byte)(0xBF | (_lengthEnabled ? 0x40 : 0x00));

    public void WriteNR30(byte value)
    {
        _dacEnabled = (value & 0x80) != 0;
        if (!_dacEnabled)
            _enabled = false;
    }

    public void WriteNR31(byte value)
    {
        _lengthLoad = value;
        _lengthCounter = 256 - _lengthLoad;
    }

    public void WriteNR32(byte value)
    {
        _outputLevel = (byte)((value >> 5) & 0x03);
    }

    public void WriteNR33(byte value)
    {
        _frequency = (ushort)((_frequency & 0x0700) | value);
    }

    public void WriteNR34(byte value)
    {
        _frequency = (ushort)((_frequency & 0x00FF) | ((value & 0x07) << 8));
        _lengthEnabled = (value & 0x40) != 0;

        if ((value & 0x80) != 0)
            Trigger();
    }
}