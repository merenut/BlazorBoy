using System;

namespace GameBoy.Core;

/// <summary>
/// Game Boy Square Wave Channel 1 with frequency sweep capability.
/// </summary>
public sealed class Square1 : IAudioChannel
{
    // Duty cycle patterns (8 steps each)
    private static readonly byte[][] DutyPatterns = {
        new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }, // 12.5%
        new byte[] { 1, 0, 0, 0, 0, 0, 0, 1 }, // 25%
        new byte[] { 1, 0, 0, 0, 0, 1, 1, 1 }, // 50%
        new byte[] { 0, 1, 1, 1, 1, 1, 1, 0 }  // 75% (inverted 25%)
    };

    // Channel state
    private bool _enabled;
    private bool _dacEnabled;

    // NR10 - Sweep register
    private byte _sweepPeriod;
    private bool _sweepDirection; // true = decrease, false = increase  
    private byte _sweepShift;
    private int _sweepTimer;
    private bool _sweepEnabled;
    private ushort _sweepShadowFreq;

    // NR11 - Duty and length
    private byte _dutyCycle;
    private byte _lengthLoad;
    private int _lengthCounter;
    private bool _lengthEnabled;

    // NR12 - Volume envelope
    private byte _initialVolume;
    private bool _envelopeDirection; // true = increase, false = decrease
    private byte _envelopePeriod;
    private int _currentVolume;
    private int _envelopeTimer;

    // NR13/NR14 - Frequency
    private ushort _frequency;
    private int _frequencyTimer;
    private int _dutyPosition;

    public bool Enabled => _enabled && _dacEnabled;

    public Square1()
    {
        Reset();
    }

    public void Reset()
    {
        _enabled = false;
        _dacEnabled = false;
        _sweepPeriod = 0;
        _sweepDirection = false;
        _sweepShift = 0;
        _sweepTimer = 0;
        _sweepEnabled = false;
        _sweepShadowFreq = 0;
        _dutyCycle = 0;
        _lengthLoad = 0;
        _lengthCounter = 0;
        _lengthEnabled = false;
        _initialVolume = 0;
        _envelopeDirection = false;
        _envelopePeriod = 0;
        _currentVolume = 0;
        _envelopeTimer = 0;
        _frequency = 0;
        _frequencyTimer = 0;
        _dutyPosition = 0;
    }

    public void Step(int cycles)
    {
        if (!_enabled)
            return;

        // Step frequency timer
        _frequencyTimer -= cycles;
        while (_frequencyTimer <= 0)
        {
            _frequencyTimer += (2048 - _frequency) * 4;
            _dutyPosition = (_dutyPosition + 1) % 8;
        }
    }

    public float GetSample()
    {
        if (!Enabled || _currentVolume == 0)
            return 0;

        byte dutyValue = DutyPatterns[_dutyCycle][_dutyPosition];
        return dutyValue * (_currentVolume / 15.0f);
    }

    public void Trigger()
    {
        _enabled = true;

        // Length counter
        if (_lengthCounter == 0)
            _lengthCounter = 64;

        // Frequency timer
        _frequencyTimer = (2048 - _frequency) * 4;

        // Volume envelope
        _currentVolume = _initialVolume;
        _envelopeTimer = _envelopePeriod;

        // Sweep
        _sweepShadowFreq = _frequency;
        _sweepTimer = _sweepPeriod > 0 ? _sweepPeriod : 8;
        _sweepEnabled = _sweepPeriod > 0 || _sweepShift > 0;

        // Perform initial sweep calculation
        if (_sweepShift > 0)
        {
            ushort newFreq = CalculateSweepFrequency();
            if (newFreq > 2047)
                _enabled = false;
        }

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

    public void StepFrequencySweep()
    {
        if (!_sweepEnabled)
            return;

        if (_sweepTimer > 0)
        {
            _sweepTimer--;
            if (_sweepTimer == 0)
            {
                _sweepTimer = _sweepPeriod > 0 ? _sweepPeriod : 8;

                if (_sweepPeriod > 0)
                {
                    ushort newFreq = CalculateSweepFrequency();
                    if (newFreq <= 2047 && _sweepShift > 0)
                    {
                        _sweepShadowFreq = newFreq;
                        _frequency = newFreq;

                        // Check overflow again
                        newFreq = CalculateSweepFrequency();
                        if (newFreq > 2047)
                            _enabled = false;
                    }
                    else if (newFreq > 2047)
                    {
                        _enabled = false;
                    }
                }
            }
        }
    }

    private ushort CalculateSweepFrequency()
    {
        ushort delta = (ushort)(_sweepShadowFreq >> _sweepShift);
        if (_sweepDirection)
            return (ushort)(_sweepShadowFreq - delta);
        else
            return (ushort)(_sweepShadowFreq + delta);
    }

    // Register access methods
    public byte ReadNR10() => (byte)(0x80 | (_sweepPeriod << 4) | (_sweepDirection ? 0x08 : 0x00) | _sweepShift);
    public byte ReadNR11() => (byte)(0x3F | (_dutyCycle << 6));
    public byte ReadNR12() => (byte)((_initialVolume << 4) | (_envelopeDirection ? 0x08 : 0x00) | _envelopePeriod);
    public byte ReadNR13() => 0xFF; // Write-only
    public byte ReadNR14() => (byte)(0xBF | (_lengthEnabled ? 0x40 : 0x00));

    public void WriteNR10(byte value)
    {
        _sweepPeriod = (byte)((value >> 4) & 0x07);
        _sweepDirection = (value & 0x08) != 0;
        _sweepShift = (byte)(value & 0x07);
    }

    public void WriteNR11(byte value)
    {
        _dutyCycle = (byte)((value >> 6) & 0x03);
        _lengthLoad = (byte)(value & 0x3F);
        _lengthCounter = 64 - _lengthLoad;
    }

    public void WriteNR12(byte value)
    {
        _initialVolume = (byte)((value >> 4) & 0x0F);
        _envelopeDirection = (value & 0x08) != 0;
        _envelopePeriod = (byte)(value & 0x07);

        _dacEnabled = (_initialVolume > 0) || _envelopeDirection;
        if (!_dacEnabled)
            _enabled = false;
    }

    public void WriteNR13(byte value)
    {
        _frequency = (ushort)((_frequency & 0x0700) | value);
    }

    public void WriteNR14(byte value)
    {
        _frequency = (ushort)((_frequency & 0x00FF) | ((value & 0x07) << 8));
        _lengthEnabled = (value & 0x40) != 0;

        if ((value & 0x80) != 0)
            Trigger();
    }
}