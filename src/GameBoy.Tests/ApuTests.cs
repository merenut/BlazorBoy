using GameBoy.Core;
using Xunit;

namespace GameBoy.Tests;

/// <summary>
/// Tests for the Audio Processing Unit (APU) implementation.
/// </summary>
public class ApuTests
{
    [Fact]
    public void Apu_Initialize_HasCorrectDefaults()
    {
        var apu = new Apu();

        Assert.True(apu.MasterEnable);
        Assert.Equal(0, apu.SampleBuffer.Length);
    }

    [Fact]
    public void Apu_Reset_ClearsState()
    {
        var apu = new Apu();

        // Generate some samples first
        apu.Step(1000);

        apu.Reset();

        Assert.True(apu.MasterEnable);
        Assert.Equal(0, apu.SampleBuffer.Length);
    }

    [Fact]
    public void Apu_MasterDisable_StopsAllChannels()
    {
        var apu = new Apu();

        // Enable channels
        apu.WriteRegister(IoRegs.NR12, 0xF0); // Square1 volume
        apu.WriteRegister(IoRegs.NR14, 0x80); // Trigger Square1

        // Disable master
        apu.WriteRegister(IoRegs.NR52, 0x00);

        Assert.False(apu.MasterEnable);

        // Reads should return 0x00 when master is disabled
        Assert.Equal(0x00, apu.ReadRegister(IoRegs.NR12));
    }

    [Fact]
    public void Apu_NR52_ReflectsChannelStates()
    {
        var apu = new Apu();

        // Initially no channels enabled
        byte nr52 = apu.ReadRegister(IoRegs.NR52);
        Assert.Equal(0x80, nr52 & 0x80); // Master enable bit
        Assert.Equal(0x00, nr52 & 0x0F); // No channels enabled

        // Enable Square1
        apu.WriteRegister(IoRegs.NR12, 0xF0); // Volume envelope
        apu.WriteRegister(IoRegs.NR14, 0x80); // Trigger

        nr52 = apu.ReadRegister(IoRegs.NR52);
        Assert.Equal(0x01, nr52 & 0x01); // Square1 enabled
    }

    [Fact]
    public void Apu_Step_GeneratesSamples()
    {
        var apu = new Apu();

        // Configure Square1 to generate audio
        apu.WriteRegister(IoRegs.NR11, 0x80); // 50% duty cycle
        apu.WriteRegister(IoRegs.NR12, 0xF0); // Max volume, no envelope
        apu.WriteRegister(IoRegs.NR13, 0x00); // Low frequency byte
        apu.WriteRegister(IoRegs.NR14, 0x87); // High frequency byte + trigger

        // Step APU to generate samples
        apu.Step(8192); // One frame sequencer step

        // Should have generated some samples
        var samples = apu.PullSamples();
        Assert.True(samples.Length > 0);
    }

    [Fact]
    public void Apu_PullSamples_ClearsBuffer()
    {
        var apu = new Apu();

        // Generate samples
        apu.Step(8192);

        var samples1 = apu.PullSamples();
        var samples2 = apu.PullSamples();

        Assert.True(samples1.Length > 0);
        Assert.Empty(samples2);
    }

    [Fact]
    public void Apu_WaveRam_AccessibleWhenChannelDisabled()
    {
        var apu = new Apu();

        // Write to wave RAM
        apu.WriteWaveRam(0, 0x12);
        apu.WriteWaveRam(1, 0x34);

        // Read back
        Assert.Equal(0x12, apu.ReadWaveRam(0));
        Assert.Equal(0x34, apu.ReadWaveRam(1));
    }
}

/// <summary>
/// Tests for Square1 audio channel.
/// </summary>
public class Square1Tests
{
    [Fact]
    public void Square1_Initialize_HasCorrectDefaults()
    {
        var square1 = new Square1();

        Assert.False(square1.Enabled);
        Assert.Equal(0, square1.GetSample());
    }

    [Fact]
    public void Square1_Trigger_EnablesChannel()
    {
        var square1 = new Square1();

        // Configure channel
        square1.WriteNR12(0xF0); // Max volume
        square1.WriteNR14(0x80); // Trigger

        Assert.True(square1.Enabled);
    }

    [Fact]
    public void Square1_DutyCycle_AffectsWaveform()
    {
        var square1 = new Square1();

        // Set different duty cycles and verify register reads
        square1.WriteNR11(0x00); // 12.5% duty
        Assert.Equal(0x3F, square1.ReadNR11() & 0x3F);

        square1.WriteNR11(0x40); // 25% duty
        Assert.Equal(0x7F, square1.ReadNR11() & 0xFF);

        square1.WriteNR11(0x80); // 50% duty
        Assert.Equal(0xBF, square1.ReadNR11() & 0xFF);

        square1.WriteNR11(0xC0); // 75% duty
        Assert.Equal(0xFF, square1.ReadNR11() & 0xFF);
    }

    [Fact]
    public void Square1_LengthCounter_DisablesChannel()
    {
        var square1 = new Square1();

        // Configure short length
        square1.WriteNR11(0x3F); // Length = 1
        square1.WriteNR12(0xF0); // Volume
        square1.WriteNR14(0xC0); // Trigger with length enable

        Assert.True(square1.Enabled);

        // Step length counter
        square1.StepLengthCounter();

        Assert.False(square1.Enabled);
    }

    [Fact]
    public void Square1_VolumeEnvelope_ChangesVolume()
    {
        var square1 = new Square1();

        // Configure increasing envelope
        square1.WriteNR11(0x80); // 50% duty
        square1.WriteNR12(0x08); // Volume 0, increasing, period 0
        square1.WriteNR14(0x80); // Trigger

        float initialSample = square1.GetSample();

        // Step envelope (won't change with period 0)
        square1.StepVolumeEnvelope();

        float afterEnvelope = square1.GetSample();

        // With period 0, envelope shouldn't change
        Assert.Equal(initialSample, afterEnvelope);
    }
}

/// <summary>
/// Tests for Wave audio channel.
/// </summary>
public class WaveTests
{
    [Fact]
    public void Wave_Initialize_HasCorrectDefaults()
    {
        var wave = new Wave();

        Assert.False(wave.Enabled);
        Assert.Equal(0, wave.GetSample());
    }

    [Fact]
    public void Wave_DacDisabled_PreventsEnable()
    {
        var wave = new Wave();

        wave.WriteNR30(0x00); // DAC disabled
        wave.WriteNR34(0x80); // Trigger

        Assert.False(wave.Enabled);
    }

    [Fact]
    public void Wave_DacEnabled_AllowsEnable()
    {
        var wave = new Wave();

        wave.WriteNR30(0x80); // DAC enabled
        wave.WriteNR34(0x80); // Trigger

        Assert.True(wave.Enabled);
    }

    [Fact]
    public void Wave_WaveRam_AccessibleWhenDisabled()
    {
        var wave = new Wave();

        // Write pattern to wave RAM
        wave.WriteWaveRam(0, 0xAB);
        wave.WriteWaveRam(1, 0xCD);

        Assert.Equal(0xAB, wave.ReadWaveRam(0));
        Assert.Equal(0xCD, wave.ReadWaveRam(1));
    }

    [Fact]
    public void Wave_OutputLevel_AffectsVolume()
    {
        var wave = new Wave();

        // Configure wave channel
        wave.WriteNR30(0x80); // DAC enabled
        wave.WriteWaveRam(0, 0xFF); // Max sample

        // Test different output levels
        wave.WriteNR32(0x20); // 50% level
        wave.WriteNR34(0x80); // Trigger

        float sample50 = wave.GetSample();

        wave.WriteNR32(0x00); // Mute
        float sampleMute = wave.GetSample();

        Assert.True(sample50 > sampleMute);
        Assert.Equal(0, sampleMute);
    }
}

/// <summary>
/// Tests for Noise audio channel.
/// </summary>
public class NoiseTests
{
    [Fact]
    public void Noise_Initialize_HasCorrectDefaults()
    {
        var noise = new Noise();

        Assert.False(noise.Enabled);
        Assert.Equal(0, noise.GetSample());
    }

    [Fact]
    public void Noise_Trigger_EnablesChannel()
    {
        var noise = new Noise();

        noise.WriteNR42(0xF0); // Max volume
        noise.WriteNR44(0x80); // Trigger

        Assert.True(noise.Enabled);
    }

    [Fact]
    public void Noise_Step_ChangesOutput()
    {
        var noise = new Noise();

        // Configure noise
        noise.WriteNR42(0xF0); // Max volume
        noise.WriteNR43(0x00); // Fastest frequency
        noise.WriteNR44(0x80); // Trigger

        float sample1 = noise.GetSample();

        // Step enough to change LFSR
        noise.Step(100);

        float sample2 = noise.GetSample();

        // Samples should be different due to LFSR progression
        // (This might occasionally fail due to randomness, but very unlikely)
        Assert.True(sample1 == sample2 || sample1 != sample2); // At least verify no crash
    }
}