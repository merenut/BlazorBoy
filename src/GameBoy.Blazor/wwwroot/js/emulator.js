window.gbInterop = (function(){
  let rafId = 0;
  let dotnetObj = null;

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

  return {
    startRenderLoop,
    stopRenderLoop,
    getCanvasContext,
    drawFrame
  };
})();
