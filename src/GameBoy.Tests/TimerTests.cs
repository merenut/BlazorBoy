using GameBoy.Core;

namespace GameBoy.Tests;

public class TimerTests
{
    [Fact]
    public void Step_DoesNotThrow()
    {
        var interruptController = new InterruptController();
        var timer = new GameBoy.Core.Timer(interruptController);
        timer.Step(4);
    }
}
