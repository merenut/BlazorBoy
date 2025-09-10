---
name: "Phase 10: APU (Audio Processing Unit) Implementation"
about: Implement Game Boy Audio Processing Unit with all 4 channels and JavaScript audio integration
title: "Phase 10: Implement Game Boy APU (Audio Processing Unit)"
labels: ["enhancement", "phase-10", "audio", "apu"]
assignees: []
---

# 🎵 Phase 10: APU (Audio Processing Unit) Implementation

## 📖 Problem Statement
The BlazorBoy Game Boy emulator currently lacks audio functionality. The Game Boy's APU (Audio Processing Unit) consists of 4 audio channels that need to be implemented for authentic game audio playback. This includes Square Wave channels, Wave channel, Noise channel, frame sequencer, and JavaScript audio integration.

## 🎯 Goals
Implement a complete, cycle-accurate Game Boy APU that:
- Supports all 4 audio channels (Square1, Square2, Wave, Noise)  
- Provides authentic Game Boy audio through browser Web Audio API
- Integrates seamlessly with existing emulator architecture
- Maintains real-time performance (44.1 kHz sample rate)
- Passes audio accuracy test ROMs

## 🏗️ Technical Requirements

### Core APU Components
- **Square Wave Channel 1**: With frequency sweep capability (NR10-NR14)
- **Square Wave Channel 2**: Simple square wave generator (NR21-NR24)  
- **Wave Channel**: Programmable waveform from Wave RAM (NR30-NR34, 0xFF30-0xFF3F)
- **Noise Channel**: Pseudo-random noise with LFSR (NR41-NR44)
- **Frame Sequencer**: 512 Hz timing control for length/envelope/sweep
- **Audio Mixing**: Master volume, panning, and channel mixing

### Integration Points
- **MMU Integration**: Audio register handling (NR10-NR52, Wave RAM)
- **Emulator Integration**: APU step timing with CPU cycles  
- **JavaScript Interop**: Web Audio API integration for browser audio output
- **Blazor UI**: Audio controls (enable/disable, volume)

## 📋 Acceptance Criteria

### Functional Requirements
- [ ] **APU Core**: Frame sequencer runs at 512 Hz (every 8192 CPU cycles)
- [ ] **Square1 Channel**: Generates square waves with frequency sweep, length counter, volume envelope
- [ ] **Square2 Channel**: Generates square waves with length counter, volume envelope (no sweep)
- [ ] **Wave Channel**: Plays programmable waveforms from Wave RAM with proper DAC control
- [ ] **Noise Channel**: Generates pseudo-random noise with 15-bit/7-bit LFSR modes
- [ ] **Audio Registers**: All NR10-NR52 registers behave like real Game Boy hardware
- [ ] **Wave RAM**: 0xFF30-0xFF3F accessible for wave pattern storage
- [ ] **Master Control**: NR52 enables/disables entire APU, NR50/NR51 control volume/panning
- [ ] **Audio Output**: 44.1 kHz stereo audio plays through browser without artifacts
- [ ] **UI Controls**: Audio enable/disable toggle, volume control

### Technical Requirements  
- [ ] **Performance**: No frame rate regression during audio playback
- [ ] **Integration**: APU integrates with existing Emulator.StepFrame() method
- [ ] **Memory**: Minimal allocations in audio generation hot paths
- [ ] **Browser Support**: Works in Chrome, Firefox, Safari with Web Audio API
- [ ] **Error Handling**: Graceful degradation when audio unavailable

### Quality Requirements
- [ ] **Testing**: Comprehensive unit tests for all audio channels
- [ ] **Code Quality**: Follows existing project patterns and style
- [ ] **Documentation**: All public APIs documented with XML comments
- [ ] **Backwards Compatibility**: All 460+ existing tests continue to pass
- [ ] **Audio Quality**: No noticeable artifacts, dropouts, or timing issues

## 🔧 Implementation Tasks

### 1. Core APU Infrastructure (High Priority)
**Files**: `src/GameBoy.Core/Apu.cs`

```csharp
public class Apu
{
    private const int FRAME_SEQUENCER_RATE = 8192; // CPU cycles per step
    private const double SAMPLE_RATE = 44100.0;
    
    public bool MasterEnable { get; private set; }
    public float[] SampleBuffer { get; private set; }
    
    public void Step(int cycles)
    public void StepFrameSequencer()
    public void GenerateSamples(int count)
    public byte ReadRegister(ushort address)
    public void WriteRegister(ushort address, byte value)
}
```

### 2. Square Wave Channel 1 (High Priority)
**Files**: `src/GameBoy.Core/Square1.cs`

**Features**: Frequency sweep, duty cycle, length counter, volume envelope
**Registers**: NR10 (sweep), NR11 (duty/length), NR12 (envelope), NR13/NR14 (frequency)

### 3. Square Wave Channel 2 (High Priority)  
**Files**: `src/GameBoy.Core/Square2.cs`

**Features**: Duty cycle, length counter, volume envelope (no sweep)
**Registers**: NR21 (duty/length), NR22 (envelope), NR23/NR24 (frequency)

### 4. Wave Channel (High Priority)
**Files**: `src/GameBoy.Core/Wave.cs`

**Features**: Programmable waveform, Wave RAM access, DAC control, length counter
**Registers**: NR30 (DAC), NR31 (length), NR32 (level), NR33/NR34 (frequency)
**Memory**: Wave RAM at 0xFF30-0xFF3F

### 5. Noise Channel (High Priority)
**Files**: `src/GameBoy.Core/Noise.cs`

**Features**: LFSR noise generation, length counter, volume envelope
**Registers**: NR41 (length), NR42 (envelope), NR43 (LFSR control), NR44 (trigger)

### 6. MMU Integration (High Priority)
**Files**: `src/GameBoy.Core/Mmu.cs`

```csharp
// In ReadIoRegister method
case >= IoRegs.NR10 and <= IoRegs.NR52:
    return Apu?.ReadRegister(addr) ?? 0xFF;
case >= IoRegs.WAVE_RAM_START and <= IoRegs.WAVE_RAM_END:
    return Apu?.ReadWaveRam((int)(addr - IoRegs.WAVE_RAM_START)) ?? 0xFF;

// In WriteIoRegister method
case >= IoRegs.NR10 and <= IoRegs.NR52:
    Apu?.WriteRegister(addr, value);
    break;
```

### 7. Emulator Integration (High Priority)
**Files**: `src/GameBoy.Core/Emulator.cs`

```csharp
private readonly Apu _apu;
public Apu Apu => _apu;

public bool StepFrame()
{
    // ... existing code ...
    _apu.Step(cycles);
    // ... existing code ...
}
```

### 8. JavaScript Audio Integration (Medium Priority)
**Files**: `src/GameBoy.Blazor/wwwroot/js/emulator.js`

**Features**: Web Audio API integration, AudioWorklet for low-latency audio, sample buffer transfer

```javascript
class GameBoyAudioProcessor extends AudioWorkletProcessor {
    process(inputs, outputs, parameters) {
        // Get samples from Blazor and output to speakers
    }
}

function initAudio() {
    // Setup AudioContext and AudioWorklet
}

function updateAudioBuffer(sampleData) {
    // Receive samples from Blazor C# code
}
```

### 9. Blazor UI Integration (Medium Priority)  
**Files**: `src/GameBoy.Blazor/Pages/Index.razor`

**Features**: Audio enable/disable toggle, volume control, audio status indicator

### 10. Testing and Validation (Medium Priority)
**Files**: `src/GameBoy.Tests/ApuTests.cs`, `src/GameBoy.Tests/AudioChannelTests.cs`

**Coverage**: Unit tests for each channel, integration tests, audio register tests, frame sequencer tests

## 🧪 Testing Strategy

### Unit Tests
```csharp
[Test] public void Square1_FrequencySweep_WorksCorrectly()
[Test] public void Square2_DutyCycle_GeneratesCorrectWaveform()  
[Test] public void Wave_WaveRam_AccessWorksCorrectly()
[Test] public void Noise_Lfsr_GeneratesProperSequence()
[Test] public void FrameSequencer_RunsAtCorrectTiming()
[Test] public void AudioRegisters_ReadWriteCorrectly()
```

### Integration Tests  
- APU integration with emulator timing
- Audio register access through MMU
- Multi-channel mixing accuracy
- JavaScript interop functionality

### Manual Testing
- Load Game Boy ROMs with known audio (Tetris, Pokémon, etc.)
- Verify audio authenticity compared to real hardware
- Test browser compatibility and performance

## 📚 Reference Resources

### Game Boy Audio Documentation
- [Game Boy Sound Hardware](https://gbdev.gg8.se/wiki/articles/Gameboy_sound_hardware)
- [APU Registers](https://gbdev.io/pandocs/Audio.html)  
- [Frame Sequencer](https://gbdev.gg8.se/wiki/articles/APU_Frame_Sequencer)

### Test ROMs
- [dmg-sound](https://github.com/blargg/dmg-sound) - Audio accuracy validation
- [cgb-sound](https://github.com/blargg/cgb-sound) - Extended audio tests

### Web Audio API
- [Web Audio API](https://developer.mozilla.org/en-US/docs/Web/API/Web_Audio_API)
- [AudioWorklet](https://developer.mozilla.org/en-US/docs/Web/API/AudioWorklet)

## 🏁 Implementation Notes for AI Agent

### Code Style Requirements
- Follow existing C# coding conventions in the project
- Use XML documentation comments for all public APIs
- Implement proper error handling and validation
- Use existing project patterns for component integration

### Performance Considerations
- APU must generate 44,100 samples per second in real-time
- Minimize memory allocations in hot paths (sample generation)
- Use efficient data structures for audio buffers
- Consider sample caching where appropriate

### Integration Patterns
- Follow existing component integration patterns (MMU ↔ Timer, PPU, etc.)
- Use dependency injection where applicable
- Maintain clean separation between emulator core and UI
- Use existing JavaScript interop patterns

### Error Handling
- Gracefully handle Web Audio API unavailability
- Provide fallback for browsers without AudioWorklet support
- Handle audio context suspension due to autoplay policies
- Add proper validation for audio register writes

### Backwards Compatibility
- Ensure all existing tests continue to pass
- Maintain existing emulator API compatibility
- Add APU as optional component that can be disabled
- Don't break existing ROM loading or emulation functionality

---

**Priority**: High  
**Estimated Effort**: 20-25 hours  
**Dependencies**: Requires MMU, Emulator, and JavaScript interop  
**Milestone**: Phase 10 Complete