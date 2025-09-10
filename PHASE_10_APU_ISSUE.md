# Phase 10: APU (Audio Processing Unit) Implementation

## Overview
Implement the Game Boy APU (Audio Processing Unit) with all 4 audio channels, frame sequencer, and JavaScript audio integration. This phase will enable authentic Game Boy audio playback through the browser's Web Audio API.

## 🎯 Objectives
- Implement all 4 Game Boy audio channels (Square1, Square2, Wave, Noise)
- Create frame sequencer for timing control (512 Hz)
- Integrate APU with MMU for audio register handling (NR10-NR52)
- Implement JavaScript audio output via Web Audio API
- Provide sample buffer generation at 44.1 kHz
- Ensure cycle-accurate timing with CPU and frame loop

## 📋 Epic Tasks

### Task 1: Core APU Infrastructure
**Priority**: High  
**Estimated Effort**: 3-4 hours

#### Implementation Requirements
- [ ] Create `Apu.cs` class with core APU functionality
- [ ] Implement frame sequencer running at 512 Hz (every 8192 CPU cycles)
- [ ] Add APU step method for cycle-accurate timing
- [ ] Implement master audio enable/disable (NR52)
- [ ] Create sample buffer management for 44.1 kHz output
- [ ] Integrate APU with Emulator class

#### Technical Details
```csharp
public class Apu
{
    private const int FRAME_SEQUENCER_RATE = 8192; // CPU cycles per frame sequencer step
    private const double SAMPLE_RATE = 44100.0;
    private const int BUFFER_SIZE = 1024;
    
    public bool MasterEnable { get; private set; }
    public float[] SampleBuffer { get; private set; }
    
    public void Step(int cycles)
    public void StepFrameSequencer()
    public void GenerateSamples(int count)
    public byte ReadRegister(ushort address)
    public void WriteRegister(ushort address, byte value)
}
```

#### Acceptance Criteria
- [ ] APU integrates with Emulator.StepFrame() 
- [ ] Frame sequencer runs at correct 512 Hz timing
- [ ] Master enable register (NR52) controls all audio
- [ ] Sample buffer generates at 44.1 kHz rate
- [ ] All APU registers (NR10-NR52) are mappable

---

### Task 2: Square Wave Channel 1 (with Frequency Sweep)
**Priority**: High  
**Estimated Effort**: 4-5 hours

#### Implementation Requirements
- [ ] Create `Square1.cs` implementing sweep-enabled square wave channel
- [ ] Implement frequency sweep functionality (NR10)
- [ ] Add length counter with auto-disable (NR11)
- [ ] Implement volume envelope (NR12)
- [ ] Add frequency control (NR13, NR14)
- [ ] Implement proper duty cycle patterns (12.5%, 25%, 50%, 75%)

#### Register Mapping
- **NR10 (0xFF10)**: Sweep period, direction, shift
- **NR11 (0xFF11)**: Wave duty, length counter load
- **NR12 (0xFF12)**: Initial volume, envelope direction, period
- **NR13 (0xFF13)**: Frequency low 8 bits
- **NR14 (0xFF14)**: Trigger, length enable, frequency high 3 bits

#### Technical Details
```csharp
public class Square1 : IAudioChannel
{
    public bool Enabled { get; private set; }
    public int LengthCounter { get; private set; }
    public int Volume { get; private set; }
    
    public void Step(int cycles)
    public void StepLengthCounter()
    public void StepVolumeEnvelope()
    public void StepFrequencySweep()
    public float GetSample()
    public void Trigger()
}
```

#### Acceptance Criteria
- [ ] Generates correct square wave with 4 duty cycle patterns
- [ ] Frequency sweep works with period, direction, and shift
- [ ] Length counter disables channel when reaching zero
- [ ] Volume envelope increases/decreases correctly
- [ ] Channel trigger resets all timers and counters
- [ ] Sweep overflow properly disables channel

---

### Task 3: Square Wave Channel 2 (Simple Square Wave)
**Priority**: High  
**Estimated Effort**: 2-3 hours

#### Implementation Requirements
- [ ] Create `Square2.cs` implementing simple square wave channel
- [ ] Add length counter with auto-disable (NR21)
- [ ] Implement volume envelope (NR22)
- [ ] Add frequency control (NR23, NR24)
- [ ] Implement proper duty cycle patterns (12.5%, 25%, 50%, 75%)

#### Register Mapping
- **NR21 (0xFF16)**: Wave duty, length counter load
- **NR22 (0xFF17)**: Initial volume, envelope direction, period
- **NR23 (0xFF18)**: Frequency low 8 bits
- **NR24 (0xFF19)**: Trigger, length enable, frequency high 3 bits

#### Technical Details
```csharp
public class Square2 : IAudioChannel
{
    public bool Enabled { get; private set; }
    public int LengthCounter { get; private set; }
    public int Volume { get; private set; }
    
    public void Step(int cycles)
    public void StepLengthCounter()
    public void StepVolumeEnvelope()
    public float GetSample()
    public void Trigger()
}
```

#### Acceptance Criteria
- [ ] Generates correct square wave with 4 duty cycle patterns
- [ ] Length counter disables channel when reaching zero
- [ ] Volume envelope increases/decreases correctly
- [ ] Channel trigger resets all timers and counters
- [ ] No frequency sweep (simpler than Square1)

---

### Task 4: Wave Channel (Programmable Waveform)
**Priority**: High  
**Estimated Effort**: 3-4 hours

#### Implementation Requirements
- [ ] Create `Wave.cs` implementing programmable wave channel
- [ ] Add wave pattern RAM access (0xFF30-0xFF3F)
- [ ] Implement DAC enable/disable (NR30)
- [ ] Add length counter (NR31)
- [ ] Implement output level control (NR32)
- [ ] Add frequency control (NR33, NR34)

#### Register Mapping
- **NR30 (0xFF1A)**: DAC on/off
- **NR31 (0xFF1B)**: Length counter load
- **NR32 (0xFF1C)**: Output level (volume shift)
- **NR33 (0xFF1D)**: Frequency low 8 bits
- **NR34 (0xFF1E)**: Trigger, length enable, frequency high 3 bits
- **Wave RAM (0xFF30-0xFF3F)**: 32 4-bit samples

#### Technical Details
```csharp
public class Wave : IAudioChannel
{
    private byte[] _waveRam = new byte[16]; // 32 4-bit samples
    private int _sampleIndex;
    
    public bool Enabled { get; private set; }
    public bool DacEnabled { get; private set; }
    public int LengthCounter { get; private set; }
    
    public void Step(int cycles)
    public void StepLengthCounter()
    public float GetSample()
    public void Trigger()
    public byte ReadWaveRam(int index)
    public void WriteWaveRam(int index, byte value)
}
```

#### Acceptance Criteria
- [ ] Wave RAM properly stores 32 4-bit samples
- [ ] DAC enable/disable controls channel output
- [ ] Output level correctly shifts sample values
- [ ] Length counter disables channel when reaching zero
- [ ] Wave position advances correctly with frequency
- [ ] Channel trigger resets sample position

---

### Task 5: Noise Channel (Pseudo-Random Noise)
**Priority**: High  
**Estimated Effort**: 3-4 hours

#### Implementation Requirements
- [ ] Create `Noise.cs` implementing noise channel
- [ ] Add length counter (NR41)
- [ ] Implement volume envelope (NR42)
- [ ] Add polynomial counter with LFSR (NR43)
- [ ] Implement trigger functionality (NR44)
- [ ] Support both 15-bit and 7-bit LFSR modes

#### Register Mapping
- **NR41 (0xFF20)**: Length counter load
- **NR42 (0xFF21)**: Initial volume, envelope direction, period
- **NR43 (0xFF22)**: Clock shift, LFSR width, divisor code
- **NR44 (0xFF23)**: Trigger, length enable

#### Technical Details
```csharp
public class Noise : IAudioChannel
{
    private ushort _lfsr = 0x7FFF; // Linear feedback shift register
    
    public bool Enabled { get; private set; }
    public int LengthCounter { get; private set; }
    public int Volume { get; private set; }
    
    public void Step(int cycles)
    public void StepLengthCounter()
    public void StepVolumeEnvelope()
    public void StepLfsr()
    public float GetSample()
    public void Trigger()
}
```

#### Acceptance Criteria
- [ ] LFSR generates proper pseudo-random sequence
- [ ] 15-bit and 7-bit LFSR modes work correctly
- [ ] Length counter disables channel when reaching zero
- [ ] Volume envelope increases/decreases correctly
- [ ] Clock shift and divisor control LFSR timing
- [ ] Channel trigger resets LFSR and counters

---

### Task 6: Audio Register Integration with MMU
**Priority**: High  
**Estimated Effort**: 2-3 hours

#### Implementation Requirements
- [ ] Extend MMU to handle all audio registers (NR10-NR52)
- [ ] Add APU property to MMU class
- [ ] Route audio register reads/writes to APU
- [ ] Implement proper register read/write masks
- [ ] Add wave RAM access through MMU

#### Technical Details
```csharp
// In Mmu.cs ReadIoRegister method
case >= IoRegs.NR10 and <= IoRegs.NR52:
    return Apu?.ReadRegister(addr) ?? 0xFF;
case >= IoRegs.WAVE_RAM_START and <= IoRegs.WAVE_RAM_END:
    return Apu?.ReadWaveRam((int)(addr - IoRegs.WAVE_RAM_START)) ?? 0xFF;

// In Mmu.cs WriteIoRegister method  
case >= IoRegs.NR10 and <= IoRegs.NR52:
    Apu?.WriteRegister(addr, value);
    break;
case >= IoRegs.WAVE_RAM_START and <= IoRegs.WAVE_RAM_END:
    Apu?.WriteWaveRam((int)(addr - IoRegs.WAVE_RAM_START), value);
    break;
```

#### Acceptance Criteria
- [ ] All audio registers are accessible through MMU
- [ ] Register read/write behavior matches Game Boy hardware
- [ ] Wave RAM is accessible at 0xFF30-0xFF3F
- [ ] Unused audio register bits read as 1
- [ ] APU integrates cleanly with existing MMU architecture

---

### Task 7: Frame Sequencer Implementation
**Priority**: High  
**Estimated Effort**: 2-3 hours

#### Implementation Requirements
- [ ] Implement 8-step frame sequencer pattern
- [ ] Run frame sequencer at 512 Hz (every 8192 CPU cycles)
- [ ] Control length counters on steps 0, 2, 4, 6
- [ ] Control volume envelopes on step 7
- [ ] Control frequency sweep on steps 2, 6 (Square1 only)

#### Technical Details
```csharp
public void StepFrameSequencer()
{
    switch (_frameSequencerStep)
    {
        case 0: case 2: case 4: case 6:
            // Step length counters
            _square1.StepLengthCounter();
            _square2.StepLengthCounter();
            _wave.StepLengthCounter();
            _noise.StepLengthCounter();
            if (_frameSequencerStep == 2 || _frameSequencerStep == 6)
                _square1.StepFrequencySweep();
            break;
        case 7:
            // Step volume envelopes
            _square1.StepVolumeEnvelope();
            _square2.StepVolumeEnvelope();
            _noise.StepVolumeEnvelope();
            break;
    }
    _frameSequencerStep = (_frameSequencerStep + 1) % 8;
}
```

#### Acceptance Criteria
- [ ] Frame sequencer runs at exactly 512 Hz
- [ ] 8-step pattern controls all timing functions
- [ ] Length counters step on correct frames
- [ ] Volume envelopes step on frame 7
- [ ] Frequency sweep steps on frames 2 and 6

---

### Task 8: Audio Mixing and Output
**Priority**: High  
**Estimated Effort**: 3-4 hours

#### Implementation Requirements
- [ ] Implement channel mixing with proper amplitude
- [ ] Add left/right panning control (NR51)
- [ ] Implement master volume control (NR50)
- [ ] Generate samples at 44.1 kHz sample rate
- [ ] Create sample buffer for JavaScript interop
- [ ] Implement proper DC offset removal

#### Technical Details
```csharp
public void GenerateSamples(int count)
{
    for (int i = 0; i < count; i++)
    {
        float left = 0, right = 0;
        
        // Mix enabled channels
        if (_square1.Enabled) {
            float sample = _square1.GetSample() * _masterVolumeLeft;
            if ((_channelControl & 0x10) != 0) left += sample;
            if ((_channelControl & 0x01) != 0) right += sample;
        }
        // ... repeat for other channels
        
        _sampleBuffer[i * 2] = left;
        _sampleBuffer[i * 2 + 1] = right;
    }
}
```

#### Acceptance Criteria
- [ ] All 4 channels mix properly with correct amplitude
- [ ] Left/right panning works via NR51 register
- [ ] Master volume control affects final output
- [ ] Sample rate matches expected 44.1 kHz
- [ ] No audio artifacts or DC offset issues

---

### Task 9: JavaScript Audio Integration
**Priority**: High  
**Estimated Effort**: 4-5 hours

#### Implementation Requirements
- [ ] Extend `emulator.js` with Web Audio API integration
- [ ] Create AudioWorklet for low-latency audio processing
- [ ] Add audio buffer transfer from C# to JavaScript
- [ ] Implement audio enable/disable controls
- [ ] Add volume control in UI
- [ ] Handle browser audio policy requirements

#### Technical Details
```javascript
// In emulator.js
class GameBoyAudioProcessor extends AudioWorkletProcessor {
    process(inputs, outputs, parameters) {
        const output = outputs[0];
        // Get samples from Blazor via shared buffer
        // Mix to output
        return true;
    }
}

function initAudio() {
    // Setup AudioContext and AudioWorklet
    // Register GameBoyAudioProcessor
    // Connect to destination
}

function updateAudioBuffer(sampleData) {
    // Receive samples from Blazor
    // Queue for AudioWorklet processing
}
```

#### C# Interop Integration
```csharp
[JSInvokable]
public async Task UpdateAudioBuffer()
{
    var samples = _emulator.Apu.PullSamples(1024);
    await _jsRuntime.InvokeVoidAsync("gbInterop.updateAudioBuffer", samples);
}
```

#### Acceptance Criteria
- [ ] Audio plays through browser with low latency
- [ ] AudioWorklet handles real-time sample processing
- [ ] Audio can be enabled/disabled via UI
- [ ] Volume control works properly
- [ ] Handles browser autoplay restrictions gracefully
- [ ] No audio dropouts or glitches during gameplay

---

### Task 10: APU Testing and Validation
**Priority**: Medium  
**Estimated Effort**: 3-4 hours

#### Implementation Requirements
- [ ] Create unit tests for each audio channel
- [ ] Test frame sequencer timing accuracy
- [ ] Validate audio register read/write behavior
- [ ] Test channel interactions and mixing
- [ ] Create audio test ROM integration tests
- [ ] Add manual audio verification tests

#### Test Coverage Areas
```csharp
[Test]
public class ApuTests
{
    // Unit tests for each channel
    void Square1_FrequencySweep_WorksCorrectly()
    void Square2_DutyCycle_GeneratesCorrectWaveform()
    void Wave_WaveRam_AccessWorksCorrectly()
    void Noise_Lfsr_GeneratesProperSequence()
    
    // Integration tests
    void FrameSequencer_RunsAtCorrectTiming()
    void AudioRegisters_ReadWriteCorrectly()
    void ChannelMixing_WorksProperly()
}
```

#### Acceptance Criteria
- [ ] All audio channels pass unit tests
- [ ] Frame sequencer timing is cycle-accurate
- [ ] Audio registers behave like real hardware
- [ ] Integration tests pass with test ROMs
- [ ] Manual audio tests produce expected sounds
- [ ] No regressions in existing emulator functionality

---

## 🏗️ Implementation Architecture

### Class Hierarchy
```
Apu
├── Square1 : IAudioChannel
├── Square2 : IAudioChannel  
├── Wave : IAudioChannel
└── Noise : IAudioChannel

IAudioChannel
├── bool Enabled
├── void Step(int cycles)
├── float GetSample()
└── void Trigger()
```

### Integration Points
1. **Emulator.cs**: Add APU property and integrate with StepFrame()
2. **Mmu.cs**: Route audio register access to APU
3. **emulator.js**: Extend with Web Audio API functionality
4. **Index.razor**: Add audio controls to UI

### Performance Considerations
- APU must process 44,100 samples per second
- Frame sequencer runs every 8,192 CPU cycles (512 Hz)
- Minimal allocations in audio generation code
- Efficient sample buffer management

## 📋 Definition of Done

### Functional Requirements
- [ ] All 4 audio channels generate correct waveforms
- [ ] Frame sequencer controls all timing functions accurately
- [ ] Audio registers (NR10-NR52) behave like real hardware
- [ ] JavaScript audio integration works without latency issues
- [ ] Audio can be enabled/disabled via UI controls

### Technical Requirements  
- [ ] APU integrates cleanly with existing emulator architecture
- [ ] No performance regression in overall emulator speed
- [ ] All audio code is properly unit tested
- [ ] Memory allocations are minimized in hot paths
- [ ] Audio works across major browsers (Chrome, Firefox, Safari)

### Quality Requirements
- [ ] Code follows existing project style and patterns
- [ ] Comprehensive documentation for all public APIs
- [ ] No audio artifacts, dropouts, or glitches
- [ ] Graceful degradation when audio is unavailable
- [ ] Proper error handling for Web Audio API failures

## 🧪 Testing Strategy

### Unit Tests
- Individual channel functionality (frequency, envelope, length)
- Frame sequencer timing and behavior
- Audio register read/write semantics
- Sample generation accuracy

### Integration Tests
- APU integration with emulator timing
- Audio register access through MMU
- Multi-channel mixing accuracy
- JavaScript interop functionality

### Manual Testing
- Load commercial Game Boy ROMs with known audio
- Verify audio matches expected behavior
- Test with different browser audio settings
- Validate performance under sustained audio generation

## 📚 Reference Documentation

### Game Boy Audio Resources
- [Game Boy Sound Hardware](https://gbdev.gg8.se/wiki/articles/Gameboy_sound_hardware)
- [Audio Channel Technical Details](https://gbdev.gg8.se/wiki/articles/Sound_Controller)
- [APU Register Reference](https://gbdev.io/pandocs/Audio.html)
- [Frame Sequencer Timing](https://gbdev.gg8.se/wiki/articles/APU_Frame_Sequencer)

### Web Audio API Resources
- [Web Audio API Documentation](https://developer.mozilla.org/en-US/docs/Web/API/Web_Audio_API)
- [AudioWorklet Guide](https://developer.mozilla.org/en-US/docs/Web/API/AudioWorklet)
- [Real-time Audio Processing](https://developer.chrome.com/blog/audio-worklet/)

### Game Boy Test ROMs
- [dmg-sound Test ROM](https://github.com/blargg/dmg-sound) - Audio accuracy tests
- [cgb-sound Test ROM](https://github.com/blargg/cgb-sound) - Extended audio tests  
- [APU Test ROMs](https://github.com/mattcurrie/dmg-acid2) - Visual/audio validation

## 🚀 Success Metrics

### Objective Measures
- [ ] All 460 existing tests continue to pass
- [ ] APU unit tests achieve >95% code coverage
- [ ] Audio latency <50ms in supported browsers
- [ ] No frame rate regression during audio playback
- [ ] Memory usage increase <10MB for APU functionality

### Subjective Measures
- [ ] Audio sounds authentic compared to real Game Boy
- [ ] No noticeable audio artifacts during gameplay
- [ ] Smooth audio playback without interruptions
- [ ] UI controls are intuitive and responsive
- [ ] Code is maintainable and well-documented

---

**Note**: This implementation should be done incrementally, with each task building upon the previous ones. Priority should be given to core APU infrastructure and individual channel implementation before moving to advanced features like JavaScript integration and testing.