namespace GameBoy.Core;

/// <summary>
/// Represents the Game Boy serial port for link cable communication.
/// This is a placeholder implementation that triggers Serial interrupts.
/// </summary>
public sealed class Serial
{
    private readonly InterruptController _interruptController;
    private int _transferCycleAccumulator = 0;
    private bool _transferInProgress = false;
    private const int CyclesPerTransfer = 512; // Placeholder: complete transfer after 512 cycles

    /// <summary>
    /// Initializes a new instance of the Serial port.
    /// </summary>
    /// <param name="interruptController">The interrupt controller to request interrupts through.</param>
    public Serial(InterruptController interruptController)
    {
        _interruptController = interruptController ?? throw new ArgumentNullException(nameof(interruptController));
    }

    /// <summary>
    /// Starts a serial transfer (placeholder implementation).
    /// In a real implementation, this would be triggered by writing to SC register.
    /// </summary>
    public void StartTransfer()
    {
        if (!_transferInProgress)
        {
            _transferInProgress = true;
            _transferCycleAccumulator = 0;
        }
    }

    /// <summary>
    /// Steps the serial transfer logic by the given cycles.
    /// </summary>
    public void Step(int cycles)
    {
        if (_transferInProgress)
        {
            _transferCycleAccumulator += cycles;

            if (_transferCycleAccumulator >= CyclesPerTransfer)
            {
                // Transfer complete - request Serial interrupt
                _interruptController.Request(InterruptType.Serial);
                _transferInProgress = false;
                _transferCycleAccumulator = 0;
            }
        }
    }
}