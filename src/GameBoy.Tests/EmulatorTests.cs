using GameBoy.Core;

namespace GameBoy.Tests;

public class EmulatorTests
{
    [Fact]
    public void StepFrame_AccumulatesCycles_UntilFrameBudgetReached()
    {
        var emulator = new Emulator();
        
        // Each CPU step returns 4 cycles in the placeholder implementation
        // 70224 / 4 = 17556 steps needed for one frame
        
        // Step multiple times - should not complete frame until 70224 cycles
        for (int i = 0; i < 17555; i++)  // One less than needed
        {
            bool frameReady = emulator.StepFrame();
            Assert.False(frameReady, $"Frame should not be ready at step {i + 1}");
        }
        
        // The last step should complete the frame
        bool finalFrameReady = emulator.StepFrame();
        Assert.True(finalFrameReady, "Frame should be ready after 70224 cycles");
    }
    
    [Fact]
    public void StepFrame_ReturnsTrue_OnlyOncePerFrame()
    {
        var emulator = new Emulator();
        
        // Step until frame is complete
        bool frameCompleted = false;
        for (int i = 0; i < 17556; i++)  // 70224 / 4 = 17556 steps
        {
            bool frameReady = emulator.StepFrame();
            if (frameReady)
            {
                frameCompleted = true;
                break;
            }
        }
        
        Assert.True(frameCompleted, "Frame should have completed");
        
        // Next step should start a new frame and not return true immediately
        bool nextStepReady = emulator.StepFrame();
        Assert.False(nextStepReady, "Next step after frame completion should not be ready");
    }
    
    [Fact]
    public void StepFrame_IsDeterministic_WithPlaceholderCpuPpu()
    {
        var emulator1 = new Emulator();
        var emulator2 = new Emulator();
        
        // Both emulators should behave identically
        for (int i = 0; i < 20000; i++)  // More than one frame
        {
            bool frame1Ready = emulator1.StepFrame();
            bool frame2Ready = emulator2.StepFrame();
            
            Assert.Equal(frame1Ready, frame2Ready);
        }
    }
}