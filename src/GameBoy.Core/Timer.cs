namespace GameBoy.Core;

/// <summary>
/// Represents the Game Boy divider/timer unit with cycle-accurate timing.
/// Maintains a 16-bit internal counter and manages DIV, TIMA, TMA, and TAC registers.
/// </summary>
public sealed class Timer
{
    private readonly InterruptController _interruptController;

    // 16-bit internal counter that increments at CPU clock speed (4.194304 MHz)
    private ushort _internalCounter = 0;

    // Timer registers - these coordinate with MMU
    private byte _tima = 0x00;  // Timer counter (0xFF05)
    private byte _tma = 0x00;   // Timer modulo (0xFF06) 
    private byte _tac = 0xF8;   // Timer control (0xFF07)

    // Cycle counters for TIMA frequency tracking
    private int _timaCounter = 0;

    /// <summary>
    /// Initializes a new instance of the Timer.
    /// </summary>
    /// <param name="interruptController">The interrupt controller to request interrupts through.</param>
    public Timer(InterruptController interruptController)
    {
        _interruptController = interruptController ?? throw new ArgumentNullException(nameof(interruptController));
    }

    /// <summary>
    /// Gets the DIV register value (upper 8 bits of internal counter).
    /// DIV increments at 16384 Hz (every 256 CPU cycles).
    /// </summary>
    public byte DIV => (byte)(_internalCounter >> 8);

    /// <summary>
    /// Gets the current TIMA register value.
    /// </summary>
    public byte TIMA => _tima;

    /// <summary>
    /// Gets the current TMA register value.
    /// </summary>
    public byte TMA => _tma;

    /// <summary>
    /// Gets the current TAC register value.
    /// </summary>
    public byte TAC => _tac;

    /// <summary>
    /// Resets the internal counter to 0 (called when DIV is written).
    /// </summary>
    public void ResetDivider()
    {
        _internalCounter = 0;
    }

    /// <summary>
    /// Sets the TIMA register value.
    /// </summary>
    public void SetTIMA(byte value)
    {
        _tima = value;
    }

    /// <summary>
    /// Sets the TMA register value.
    /// </summary>
    public void SetTMA(byte value)
    {
        _tma = value;
    }

    /// <summary>
    /// Sets the TAC register value and resets TIMA counter.
    /// </summary>
    public void SetTAC(byte value)
    {
        _tac = value;
        _timaCounter = 0; // Reset TIMA counter when frequency changes
    }

    /// <summary>
    /// Resets timer to post-BIOS defaults.
    /// </summary>
    public void Reset()
    {
        _internalCounter = 0;
        _tima = 0x00;
        _tma = 0x00;
        _tac = 0xF8;
        _timaCounter = 0;
    }

    /// <summary>
    /// Steps the timer logic by the given cycles with cycle-accurate timing.
    /// </summary>
    public void Step(int cycles)
    {
        // Update internal counter (drives DIV)
        _internalCounter = (ushort)((_internalCounter + cycles) & 0xFFFF);

        // Only update TIMA if timer is enabled (TAC bit 2)
        if ((_tac & 0x04) != 0)
        {
            _timaCounter += cycles;

            int timaFrequency = GetTIMAFrequency();

            while (_timaCounter >= timaFrequency)
            {
                _timaCounter -= timaFrequency;

                // Increment TIMA
                _tima++;

                // Check for overflow
                if (_tima == 0)
                {
                    // TIMA overflowed - reload from TMA and request Timer interrupt
                    _tima = _tma;
                    _interruptController.Request(InterruptType.Timer);
                }
            }
        }
    }

    /// <summary>
    /// Gets the number of CPU cycles per TIMA increment based on TAC register.
    /// </summary>
    private int GetTIMAFrequency()
    {
        return (_tac & 0x03) switch
        {
            0x00 => 1024,  // 4096 Hz
            0x01 => 16,    // 262144 Hz  
            0x02 => 64,    // 65536 Hz
            0x03 => 256,   // 16384 Hz
            _ => 1024      // Default case (should not happen)
        };
    }
}
