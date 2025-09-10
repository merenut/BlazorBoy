using System;

namespace GameBoy.Core;

/// <summary>
/// Game Boy Audio Processing Unit (APU) implementation.
/// Handles all 4 audio channels, frame sequencer, and sample generation.
/// </summary>
public sealed class Apu
{
    private const int FRAME_SEQUENCER_RATE = 8192; // CPU cycles per frame sequencer step (512 Hz)
    private const double SAMPLE_RATE = 44100.0;
    private const int BUFFER_SIZE = 1024;

    // Audio channels
    private readonly Square1 _square1;
    private readonly Square2 _square2;
    private readonly Wave _wave;
    private readonly Noise _noise;

    // Frame sequencer timing
    private int _frameSequencerTimer;
    private int _frameSequencerStep;

    // Sample generation
    private readonly float[] _sampleBuffer;
    private int _sampleBufferIndex;
    private double _sampleAccumulator;
    private double _cyclesPerSample;

    // Master control registers
    private bool _masterEnable = true; // NR52 bit 7
    private byte _leftVolumeAndVin = 0x77; // NR50 
    private byte _soundPanning = 0xF3; // NR51

    public Apu()
    {
        _square1 = new Square1();
        _square2 = new Square2();
        _wave = new Wave();
        _noise = new Noise();

        _sampleBuffer = new float[BUFFER_SIZE * 2]; // Stereo buffer
        _cyclesPerSample = 4194304.0 / SAMPLE_RATE; // Game Boy CPU frequency / sample rate

        Reset();
    }

    /// <summary>
    /// Gets whether master audio is enabled (NR52 bit 7).
    /// </summary>
    public bool MasterEnable => _masterEnable;

    /// <summary>
    /// Gets the current sample buffer for audio output.
    /// </summary>
    public ReadOnlySpan<float> SampleBuffer => _sampleBuffer.AsSpan(0, _sampleBufferIndex);

    /// <summary>
    /// Resets the APU to initial state.
    /// </summary>
    public void Reset()
    {
        _frameSequencerTimer = 0;
        _frameSequencerStep = 0;
        _sampleBufferIndex = 0;
        _sampleAccumulator = 0;

        // Reset to post-BIOS defaults
        _masterEnable = true;
        _leftVolumeAndVin = 0x77;
        _soundPanning = 0xF3;

        // Reset all channels
        _square1.Reset();
        _square2.Reset();
        _wave.Reset();
        _noise.Reset();
    }

    /// <summary>
    /// Steps the APU by the given number of CPU cycles.
    /// </summary>
    /// <param name="cycles">Number of CPU cycles to step</param>
    public void Step(int cycles)
    {
        if (!_masterEnable)
            return;

        // Step frame sequencer
        _frameSequencerTimer += cycles;
        while (_frameSequencerTimer >= FRAME_SEQUENCER_RATE)
        {
            _frameSequencerTimer -= FRAME_SEQUENCER_RATE;
            StepFrameSequencer();
        }

        // Step all audio channels
        _square1.Step(cycles);
        _square2.Step(cycles);
        _wave.Step(cycles);
        _noise.Step(cycles);

        // Generate audio samples
        GenerateSamples(cycles);
    }

    /// <summary>
    /// Steps the frame sequencer (runs at 512 Hz).
    /// </summary>
    private void StepFrameSequencer()
    {
        // Frame sequencer controls timing of various audio functions
        switch (_frameSequencerStep)
        {
            case 0: // Length counter
                _square1.StepLengthCounter();
                _square2.StepLengthCounter();
                _wave.StepLengthCounter();
                _noise.StepLengthCounter();
                break;
            case 1: // Nothing
                break;
            case 2: // Length counter and sweep
                _square1.StepLengthCounter();
                _square2.StepLengthCounter();
                _wave.StepLengthCounter();
                _noise.StepLengthCounter();
                _square1.StepFrequencySweep();
                break;
            case 3: // Nothing
                break;
            case 4: // Length counter
                _square1.StepLengthCounter();
                _square2.StepLengthCounter();
                _wave.StepLengthCounter();
                _noise.StepLengthCounter();
                break;
            case 5: // Nothing
                break;
            case 6: // Length counter and sweep
                _square1.StepLengthCounter();
                _square2.StepLengthCounter();
                _wave.StepLengthCounter();
                _noise.StepLengthCounter();
                _square1.StepFrequencySweep();
                break;
            case 7: // Volume envelope
                _square1.StepVolumeEnvelope();
                _square2.StepVolumeEnvelope();
                _noise.StepVolumeEnvelope();
                break;
        }

        _frameSequencerStep = (_frameSequencerStep + 1) % 8;
    }

    /// <summary>
    /// Generates audio samples for the given number of cycles.
    /// </summary>
    /// <param name="cycles">Number of CPU cycles</param>
    private void GenerateSamples(int cycles)
    {
        _sampleAccumulator += cycles;

        while (_sampleAccumulator >= _cyclesPerSample)
        {
            _sampleAccumulator -= _cyclesPerSample;

            if (_sampleBufferIndex >= _sampleBuffer.Length - 1)
                continue; // Buffer full, skip sample

            // Mix all channels
            float sample1 = _square1.GetSample();
            float sample2 = _square2.GetSample();
            float sample3 = _wave.GetSample();
            float sample4 = _noise.GetSample();

            // Apply panning (simplified stereo mixing)
            float leftMix = 0;
            float rightMix = 0;

            if ((_soundPanning & 0x01) != 0) rightMix += sample1; // Square1 right
            if ((_soundPanning & 0x02) != 0) rightMix += sample2; // Square2 right
            if ((_soundPanning & 0x04) != 0) rightMix += sample3; // Wave right
            if ((_soundPanning & 0x08) != 0) rightMix += sample4; // Noise right
            if ((_soundPanning & 0x10) != 0) leftMix += sample1;  // Square1 left
            if ((_soundPanning & 0x20) != 0) leftMix += sample2;  // Square2 left
            if ((_soundPanning & 0x40) != 0) leftMix += sample3;  // Wave left
            if ((_soundPanning & 0x80) != 0) leftMix += sample4;  // Noise left

            // Apply master volume
            float leftVolume = ((_leftVolumeAndVin >> 4) & 0x07) / 7.0f;
            float rightVolume = (_leftVolumeAndVin & 0x07) / 7.0f;

            leftMix *= leftVolume * 0.25f; // Scale down for mixing
            rightMix *= rightVolume * 0.25f;

            // Store stereo samples
            _sampleBuffer[_sampleBufferIndex++] = Math.Clamp(leftMix, -1.0f, 1.0f);
            _sampleBuffer[_sampleBufferIndex++] = Math.Clamp(rightMix, -1.0f, 1.0f);
        }
    }

    /// <summary>
    /// Pulls generated samples and resets the buffer.
    /// </summary>
    /// <returns>Array of stereo samples (left, right, left, right, ...)</returns>
    public float[] PullSamples()
    {
        var samples = new float[_sampleBufferIndex];
        Array.Copy(_sampleBuffer, samples, _sampleBufferIndex);
        _sampleBufferIndex = 0;
        return samples;
    }

    /// <summary>
    /// Reads an audio register value.
    /// </summary>
    /// <param name="address">Register address</param>
    /// <returns>Register value</returns>
    public byte ReadRegister(ushort address)
    {
        return address switch
        {
            IoRegs.NR10 => _masterEnable ? _square1.ReadNR10() : (byte)0x00,
            IoRegs.NR11 => _masterEnable ? _square1.ReadNR11() : (byte)0x00,
            IoRegs.NR12 => _masterEnable ? _square1.ReadNR12() : (byte)0x00,
            IoRegs.NR13 => _masterEnable ? _square1.ReadNR13() : (byte)0x00,
            IoRegs.NR14 => _masterEnable ? _square1.ReadNR14() : (byte)0x00,
            IoRegs.NR21 => _masterEnable ? _square2.ReadNR21() : (byte)0x00,
            IoRegs.NR22 => _masterEnable ? _square2.ReadNR22() : (byte)0x00,
            IoRegs.NR23 => _masterEnable ? _square2.ReadNR23() : (byte)0x00,
            IoRegs.NR24 => _masterEnable ? _square2.ReadNR24() : (byte)0x00,
            IoRegs.NR30 => _masterEnable ? _wave.ReadNR30() : (byte)0x00,
            IoRegs.NR31 => _masterEnable ? _wave.ReadNR31() : (byte)0x00,
            IoRegs.NR32 => _masterEnable ? _wave.ReadNR32() : (byte)0x00,
            IoRegs.NR33 => _masterEnable ? _wave.ReadNR33() : (byte)0x00,
            IoRegs.NR34 => _masterEnable ? _wave.ReadNR34() : (byte)0x00,
            IoRegs.NR41 => _masterEnable ? _noise.ReadNR41() : (byte)0x00,
            IoRegs.NR42 => _masterEnable ? _noise.ReadNR42() : (byte)0x00,
            IoRegs.NR43 => _masterEnable ? _noise.ReadNR43() : (byte)0x00,
            IoRegs.NR44 => _masterEnable ? _noise.ReadNR44() : (byte)0x00,
            IoRegs.NR50 => _masterEnable ? _leftVolumeAndVin : (byte)0x00,
            IoRegs.NR51 => _masterEnable ? _soundPanning : (byte)0x00,
            IoRegs.NR52 => (byte)((_masterEnable ? 0x80 : 0x00) |
                                  (_masterEnable && _square1.Enabled ? 0x01 : 0x00) |
                                  (_masterEnable && _square2.Enabled ? 0x02 : 0x00) |
                                  (_masterEnable && _wave.Enabled ? 0x04 : 0x00) |
                                  (_masterEnable && _noise.Enabled ? 0x08 : 0x00) | 0x70),
            _ => 0xFF
        };
    }

    /// <summary>
    /// Writes an audio register value.
    /// </summary>
    /// <param name="address">Register address</param>
    /// <param name="value">Value to write</param>
    public void WriteRegister(ushort address, byte value)
    {
        switch (address)
        {
            case IoRegs.NR10: if (_masterEnable) _square1.WriteNR10(value); break;
            case IoRegs.NR11: if (_masterEnable) _square1.WriteNR11(value); break;
            case IoRegs.NR12: if (_masterEnable) _square1.WriteNR12(value); break;
            case IoRegs.NR13: if (_masterEnable) _square1.WriteNR13(value); break;
            case IoRegs.NR14: if (_masterEnable) _square1.WriteNR14(value); break;
            case IoRegs.NR21: if (_masterEnable) _square2.WriteNR21(value); break;
            case IoRegs.NR22: if (_masterEnable) _square2.WriteNR22(value); break;
            case IoRegs.NR23: if (_masterEnable) _square2.WriteNR23(value); break;
            case IoRegs.NR24: if (_masterEnable) _square2.WriteNR24(value); break;
            case IoRegs.NR30: if (_masterEnable) _wave.WriteNR30(value); break;
            case IoRegs.NR31: if (_masterEnable) _wave.WriteNR31(value); break;
            case IoRegs.NR32: if (_masterEnable) _wave.WriteNR32(value); break;
            case IoRegs.NR33: if (_masterEnable) _wave.WriteNR33(value); break;
            case IoRegs.NR34: if (_masterEnable) _wave.WriteNR34(value); break;
            case IoRegs.NR41: if (_masterEnable) _noise.WriteNR41(value); break;
            case IoRegs.NR42: if (_masterEnable) _noise.WriteNR42(value); break;
            case IoRegs.NR43: if (_masterEnable) _noise.WriteNR43(value); break;
            case IoRegs.NR44: if (_masterEnable) _noise.WriteNR44(value); break;
            case IoRegs.NR50: if (_masterEnable) _leftVolumeAndVin = value; break;
            case IoRegs.NR51: if (_masterEnable) _soundPanning = value; break;
            case IoRegs.NR52:
                bool newMasterEnable = (value & 0x80) != 0;
                if (_masterEnable && !newMasterEnable)
                {
                    // Turning off master enable clears all audio registers except NR52
                    _leftVolumeAndVin = 0;
                    _soundPanning = 0;
                    _square1.Reset();
                    _square2.Reset();
                    _wave.Reset();
                    _noise.Reset();
                    _masterEnable = false;
                }
                else if (!_masterEnable && newMasterEnable)
                {
                    _masterEnable = true;
                }
                break;
        }
    }

    /// <summary>
    /// Reads wave RAM value.
    /// </summary>
    /// <param name="index">Index into wave RAM (0-15)</param>
    /// <returns>Wave RAM value</returns>
    public byte ReadWaveRam(int index)
    {
        return _wave.ReadWaveRam(index);
    }

    /// <summary>
    /// Writes wave RAM value.
    /// </summary>
    /// <param name="index">Index into wave RAM (0-15)</param>
    /// <param name="value">Value to write</param>
    public void WriteWaveRam(int index, byte value)
    {
        _wave.WriteWaveRam(index, value);
    }
}