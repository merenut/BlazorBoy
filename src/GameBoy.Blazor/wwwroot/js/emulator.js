window.gbInterop = (function(){
  let rafId = 0;
  let dotnetObj = null;
  let audioContext = null;
  let audioWorkletNode = null;
  let audioEnabled = false;
  let audioInitialized = false;
  
  // Persistent buffer for optimized frame drawing
  let frameBufferCache = null;
  let frameBufferSize = 0;

  function startRenderLoop(dotnet) {
    dotnetObj = dotnet;
    function tick(ts) {
      if (!dotnetObj) return;
      dotnetObj.invokeMethodAsync('OnAnimationFrame', ts).then(continueLoop => {
        if (continueLoop) {
          rafId = requestAnimationFrame(tick);
        }
      }).catch(() => {
        // Stop on error
        stopRenderLoop();
      });
    }
    rafId = requestAnimationFrame(tick);
  }

  function stopRenderLoop(){
    if (rafId) cancelAnimationFrame(rafId);
    rafId = 0;
    dotnetObj = null;
  }

  function getCanvasContext(canvasId) {
    const canvas = document.getElementById(canvasId);
    return canvas ? canvas.getContext('2d') : null;
  }

  function drawFrame(canvasId, width, height, buffer) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    
    // Create or reuse ImageData and Uint8Array buffer for performance
    const pixelCount = width * height;
    const bufferSize = pixelCount * 4; // RGBA
    
    if (!frameBufferCache || frameBufferSize !== bufferSize) {
      frameBufferCache = ctx.createImageData(width, height);
      frameBufferSize = bufferSize;
    }
    
    const data = frameBufferCache.data; // Uint8ClampedArray
    
    // Optimized conversion from Int32 ARGB to Uint8 RGBA
    for (let i = 0, j = 0; i < buffer.length; i++, j += 4) {
      const argb = buffer[i] >>> 0;
      data[j] = (argb >>> 16) & 0xFF;     // R
      data[j + 1] = (argb >>> 8) & 0xFF;  // G
      data[j + 2] = argb & 0xFF;          // B
      data[j + 3] = (argb >>> 24) & 0xFF; // A
    }
    
    ctx.putImageData(frameBufferCache, 0, 0);
  }

  function drawFrameRgba(canvasId, width, height, rgbaBuffer) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    
    // Create or reuse ImageData for performance
    const pixelCount = width * height;
    const bufferSize = pixelCount * 4; // RGBA
    
    if (!frameBufferCache || frameBufferSize !== bufferSize) {
      frameBufferCache = ctx.createImageData(width, height);
      frameBufferSize = bufferSize;
    }
    
    // Direct copy from RGBA byte array - much faster than conversion
    frameBufferCache.data.set(rgbaBuffer);
    
    ctx.putImageData(frameBufferCache, 0, 0);
  }

  async function initAudio() {
    if (audioInitialized) return true;
    
    try {
      audioContext = new (window.AudioContext || window.webkitAudioContext)({
        sampleRate: 44100,
        latencyHint: 'interactive'
      });

      // Resume audio context if it's suspended (browser autoplay policy)
      if (audioContext.state === 'suspended') {
        await audioContext.resume();
      }

      // Load AudioWorklet processor
      await audioContext.audioWorklet.addModule('/js/gameboy-audio-processor.js');
      
      // Create AudioWorklet node
      audioWorkletNode = new AudioWorkletNode(audioContext, 'gameboy-audio-processor', {
        numberOfInputs: 0,
        numberOfOutputs: 1,
        outputChannelCount: [2] // Stereo output
      });

      // Connect to audio output
      audioWorkletNode.connect(audioContext.destination);

      audioInitialized = true;
      return true;
    } catch (error) {
      console.error('Failed to initialize audio:', error);
      return false;
    }
  }

  async function enableAudio() {
    if (!audioInitialized) {
      const success = await initAudio();
      if (!success) return false;
    }

    if (audioContext && audioContext.state === 'suspended') {
      await audioContext.resume();
    }

    audioEnabled = true;
    return true;
  }

  function disableAudio() {
    audioEnabled = false;
    if (audioContext && audioContext.state === 'running') {
      audioContext.suspend();
    }
  }

  function updateAudioBuffer(samples) {
    if (!audioEnabled || !audioWorkletNode) return;
    
    // Send audio samples to AudioWorklet
    audioWorkletNode.port.postMessage({
      type: 'audioData',
      samples: samples
    });
  }

  function setAudioVolume(volume) {
    if (audioWorkletNode) {
      audioWorkletNode.port.postMessage({
        type: 'setVolume',
        volume: Math.max(0, Math.min(1, volume))
      });
    }
  }

  return {
    startRenderLoop,
    stopRenderLoop,
    getCanvasContext,
    drawFrame,
    drawFrameRgba,
    initAudio,
    enableAudio,
    disableAudio,
    updateAudioBuffer,
    setAudioVolume
  };
})();

// Theme management functions
window.detectDarkMode = function() {
  // Check saved preference first
  const saved = localStorage.getItem('theme');
  if (saved) {
    return saved === 'dark';
  }
  
  // Otherwise check system preference
  return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
};

window.setTheme = function(theme) {
  document.documentElement.setAttribute('data-theme', theme);
  localStorage.setItem('theme', theme);
};

// Initialize theme on load
document.addEventListener('DOMContentLoaded', function() {
  const isDark = window.detectDarkMode();
  window.setTheme(isDark ? 'dark' : 'light');
});
