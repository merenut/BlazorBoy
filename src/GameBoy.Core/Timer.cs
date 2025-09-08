namespace GameBoy.Core;

/// <summary>
/// Represents the Game Boy divider/timers unit.
/// </summary>
public sealed class Timer
{
    private readonly InterruptController _interruptController;
    private int _cycleAccumulator = 0;
    private const int CyclesPerTimerInterrupt = 1024; // Placeholder: trigger interrupt every 1024 cycles

    /// <summary>
    /// Initializes a new instance of the Timer.
    /// </summary>
    /// <param name="interruptController">The interrupt controller to request interrupts through.</param>
    public Timer(InterruptController interruptController)
    {
        _interruptController = interruptController ?? throw new ArgumentNullException(nameof(interruptController));
    }

    /// <summary>
    /// Steps the timer logic by the given cycles.
    /// </summary>
    public void Step(int cycles)
    {
        // Placeholder implementation: accumulate cycles and request Timer interrupt periodically
        _cycleAccumulator += cycles;

        if (_cycleAccumulator >= CyclesPerTimerInterrupt)
        {
            _cycleAccumulator -= CyclesPerTimerInterrupt;
            _interruptController.Request(InterruptType.Timer);
        }
    }
}
