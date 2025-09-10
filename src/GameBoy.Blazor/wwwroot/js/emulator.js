window.gbInterop = (function(){
  let rafId = 0;
  let dotnetObj = null;
  let audioContext = null;
  let audioWorkletNode = null;
  let audioEnabled = false;
  let audioInitialized = false;

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
    const imageData = ctx.createImageData(width, height);
    const data = imageData.data; // Uint8ClampedArray
    // buffer is int32 array with ARGB (A in high byte)
    for (let i = 0, j = 0; i < buffer.length; i++, j += 4) {
      const argb = buffer[i] >>> 0;
      const a = (argb >>> 24) & 0xFF;
      const r = (argb >>> 16) & 0xFF;
      const g = (argb >>> 8) & 0xFF;
      const b = argb & 0xFF;
      data[j] = r;
      data[j + 1] = g;
      data[j + 2] = b;
      data[j + 3] = a;
    }
    ctx.putImageData(imageData, 0, 0);
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
    initAudio,
    enableAudio,
    disableAudio,
    updateAudioBuffer,
    setAudioVolume
  };
})();
