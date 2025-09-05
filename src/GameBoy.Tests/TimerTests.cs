using GameBoy.Core;

namespace GameBoy.Tests;

public class TimerTests
{
    [Fact]
    public void Step_DoesNotThrow()
    {
    var timer = new GameBoy.Core.Timer();
        timer.Step(4);
    }
}
