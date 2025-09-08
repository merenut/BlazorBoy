namespace GameBoy.Core;

/// <summary>
/// Represents the Game Boy joypad input state.
/// </summary>
public sealed class Joypad
{
    private readonly InterruptController _interruptController;

    // Current state
    private bool _right, _left, _up, _down;
    private bool _a, _b, _select, _start;

    /// <summary>
    /// Initializes a new instance of the Joypad.
    /// </summary>
    /// <param name="interruptController">The interrupt controller to request interrupts through.</param>
    public Joypad(InterruptController interruptController)
    {
        _interruptController = interruptController ?? throw new ArgumentNullException(nameof(interruptController));
    }

    public bool Right
    {
        get => _right;
        set
        {
            if (!_right && value) // Button pressed (false -> true transition)
                _interruptController.Request(InterruptType.Joypad);
            _right = value;
        }
    }

    public bool Left
    {
        get => _left;
        set
        {
            if (!_left && value) // Button pressed (false -> true transition)
                _interruptController.Request(InterruptType.Joypad);
            _left = value;
        }
    }

    public bool Up
    {
        get => _up;
        set
        {
            if (!_up && value) // Button pressed (false -> true transition)
                _interruptController.Request(InterruptType.Joypad);
            _up = value;
        }
    }

    public bool Down
    {
        get => _down;
        set
        {
            if (!_down && value) // Button pressed (false -> true transition)
                _interruptController.Request(InterruptType.Joypad);
            _down = value;
        }
    }

    public bool A
    {
        get => _a;
        set
        {
            if (!_a && value) // Button pressed (false -> true transition)
                _interruptController.Request(InterruptType.Joypad);
            _a = value;
        }
    }

    public bool B
    {
        get => _b;
        set
        {
            if (!_b && value) // Button pressed (false -> true transition)
                _interruptController.Request(InterruptType.Joypad);
            _b = value;
        }
    }

    public bool Select
    {
        get => _select;
        set
        {
            if (!_select && value) // Button pressed (false -> true transition)
                _interruptController.Request(InterruptType.Joypad);
            _select = value;
        }
    }

    public bool Start
    {
        get => _start;
        set
        {
            if (!_start && value) // Button pressed (false -> true transition)
                _interruptController.Request(InterruptType.Joypad);
            _start = value;
        }
    }
}
