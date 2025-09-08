namespace GameBoy.Core;

/// <summary>
/// Represents the Game Boy joypad input state.
/// </summary>
public sealed class Joypad
{
    private readonly InterruptController _interruptController;

    // Previous state to detect changes
    private bool _prevRight, _prevLeft, _prevUp, _prevDown;
    private bool _prevA, _prevB, _prevSelect, _prevStart;

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
            if (!_prevRight && value) // Button pressed (false -> true transition)
                _interruptController.Request(InterruptType.Joypad);
            _prevRight = _right;
            _right = value;
        }
    }

    public bool Left
    {
        get => _left;
        set
        {
            if (!_prevLeft && value) // Button pressed (false -> true transition)
                _interruptController.Request(InterruptType.Joypad);
            _prevLeft = _left;
            _left = value;
        }
    }

    public bool Up
    {
        get => _up;
        set
        {
            if (!_prevUp && value) // Button pressed (false -> true transition)
                _interruptController.Request(InterruptType.Joypad);
            _prevUp = _up;
            _up = value;
        }
    }

    public bool Down
    {
        get => _down;
        set
        {
            if (!_prevDown && value) // Button pressed (false -> true transition)
                _interruptController.Request(InterruptType.Joypad);
            _prevDown = _down;
            _down = value;
        }
    }

    public bool A
    {
        get => _a;
        set
        {
            if (!_prevA && value) // Button pressed (false -> true transition)
                _interruptController.Request(InterruptType.Joypad);
            _prevA = _a;
            _a = value;
        }
    }

    public bool B
    {
        get => _b;
        set
        {
            if (!_prevB && value) // Button pressed (false -> true transition)
                _interruptController.Request(InterruptType.Joypad);
            _prevB = _b;
            _b = value;
        }
    }

    public bool Select
    {
        get => _select;
        set
        {
            if (!_prevSelect && value) // Button pressed (false -> true transition)
                _interruptController.Request(InterruptType.Joypad);
            _prevSelect = _select;
            _select = value;
        }
    }

    public bool Start
    {
        get => _start;
        set
        {
            if (!_prevStart && value) // Button pressed (false -> true transition)
                _interruptController.Request(InterruptType.Joypad);
            _prevStart = _start;
            _start = value;
        }
    }
}
