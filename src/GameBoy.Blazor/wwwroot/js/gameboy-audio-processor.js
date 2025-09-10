// GameBoy Audio Processor - AudioWorklet implementation for low-latency audio
class GameBoyAudioProcessor extends AudioWorkletProcessor {
  constructor() {
    super();
    
    this.sampleBuffer = [];
    this.volume = 0.5;
    
    // Listen for messages from main thread
    this.port.onmessage = (event) => {
      const { type, samples, volume } = event.data;
      
      if (type === 'audioData' && samples) {
        // Add new samples to buffer
        for (let i = 0; i < samples.length; i++) {
          this.sampleBuffer.push(samples[i]);
        }
      } else if (type === 'setVolume' && typeof volume === 'number') {
        this.volume = Math.max(0, Math.min(1, volume));
      }
    };
  }

  process(inputs, outputs, parameters) {
    const output = outputs[0];
    
    if (!output || output.length < 2) {
      return true; // Continue processing
    }
    
    const leftChannel = output[0];
    const rightChannel = output[1];
    const frameSize = leftChannel.length;
    
    // Fill output with samples from buffer
    for (let i = 0; i < frameSize; i++) {
      let leftSample = 0;
      let rightSample = 0;
      
      // Get stereo samples from buffer (left, right, left, right...)
      if (this.sampleBuffer.length >= 2) {
        leftSample = this.sampleBuffer.shift() * this.volume;
        rightSample = this.sampleBuffer.shift() * this.volume;
      }
      
      // Clamp samples to valid range
      leftChannel[i] = Math.max(-1, Math.min(1, leftSample));
      rightChannel[i] = Math.max(-1, Math.min(1, rightSample));
    }
    
    // Prevent buffer from growing too large (avoid latency)
    if (this.sampleBuffer.length > 4096) {
      this.sampleBuffer = this.sampleBuffer.slice(-2048);
    }
    
    return true; // Continue processing
  }
}

registerProcessor('gameboy-audio-processor', GameBoyAudioProcessor);