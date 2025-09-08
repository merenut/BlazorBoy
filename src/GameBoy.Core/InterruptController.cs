namespace GameBoy.Core;

/// <summary>
/// Represents the five core Game Boy interrupt types in priority order.
/// Use with InterruptController.Request() to trigger interrupts from hardware components.
/// </summary>
public enum InterruptType : byte
{
    /// <summary>
    /// VBlank interrupt (Bit 0, Vector 0x0040, Highest Priority).
    /// Triggered by PPU at the start of vertical blanking period.
    /// </summary>
    VBlank = 0,

    /// <summary>
    /// LCD STAT interrupt (Bit 1, Vector 0x0048).
    /// Triggered by PPU on LCD status changes (mode transitions, LYC=LY, etc).
    /// </summary>
    LCDStat = 1,

    /// <summary>
    /// Timer interrupt (Bit 2, Vector 0x0050).
    /// Triggered by Timer when TIMA register overflows.
    /// </summary>
    Timer = 2,

    /// <summary>
    /// Serial interrupt (Bit 3, Vector 0x0058).
    /// Triggered by Serial port when data transfer completes.
    /// </summary>
    Serial = 3,

    /// <summary>
    /// Joypad interrupt (Bit 4, Vector 0x0060, Lowest Priority).
    /// Triggered by Joypad when a button is pressed.
    /// </summary>
    Joypad = 4
}

/// <summary>
/// Handles interrupt enable/flags and dispatch for the emulator.
/// Encapsulates IF (0xFF0F) and IE (0xFFFF) registers with proper masking.
/// </summary>
public sealed class InterruptController
{
    private byte _if = 0x01; // Interrupt Flag register (only lower 5 bits used)
    private byte _ie = 0x00; // Interrupt Enable register (full 8-bit)

    /// <summary>
    /// Gets the current IF register value with upper 3 bits set to 1.
    /// </summary>
    public byte IF => (byte)(_if | 0xE0);

    /// <summary>
    /// Gets the current IE register value.
    /// </summary>
    public byte IE => _ie;

    /// <summary>
    /// Sets the IF register value, masking to lower 5 bits only.
    /// </summary>
    public void SetIF(byte value)
    {
        _if = (byte)(value & 0x1F);
    }

    /// <summary>
    /// Sets the IE register value (full 8-bit).
    /// </summary>
    public void SetIE(byte value)
    {
        _ie = value;
    }

    /// <summary>
    /// Requests an interrupt by setting the corresponding bit in the IF register.
    /// 
    /// Usage examples for each component:
    /// - PPU: Request VBlank interrupt at end of frame: interruptController.Request(InterruptType.VBlank)
    /// - PPU: Request LCD STAT interrupt on mode changes: interruptController.Request(InterruptType.LCDStat)
    /// - Timer: Request Timer interrupt on TIMA overflow: interruptController.Request(InterruptType.Timer)
    /// - Serial: Request Serial interrupt on transfer complete: interruptController.Request(InterruptType.Serial)
    /// - Joypad: Request Joypad interrupt on button press: interruptController.Request(InterruptType.Joypad)
    /// </summary>
    /// <param name="interruptType">The type of interrupt to request.</param>
    public void Request(InterruptType interruptType)
    {
        byte bit = (byte)(1 << (int)interruptType);
        _if |= bit;
    }

    /// <summary>
    /// Checks for pending interrupts and returns the highest priority one.
    /// An interrupt is pending if both IF and IE bits are set for that interrupt.
    /// </summary>
    /// <param name="interruptType">The highest priority pending interrupt, if any.</param>
    /// <returns>True if a pending interrupt was found, false otherwise.</returns>
    public bool TryGetPending(out InterruptType interruptType)
    {
        byte pending = (byte)(_if & _ie);

        // Check interrupts in priority order (VBlank highest, Joypad lowest)
        if ((pending & 0x01) != 0) { interruptType = InterruptType.VBlank; return true; }
        if ((pending & 0x02) != 0) { interruptType = InterruptType.LCDStat; return true; }
        if ((pending & 0x04) != 0) { interruptType = InterruptType.Timer; return true; }
        if ((pending & 0x08) != 0) { interruptType = InterruptType.Serial; return true; }
        if ((pending & 0x10) != 0) { interruptType = InterruptType.Joypad; return true; }

        interruptType = default;
        return false;
    }

    /// <summary>
    /// Checks if any interrupt flags are set in IF register.
    /// Used for HALT wake-up logic - HALT wakes on any IF flag regardless of IE.
    /// </summary>
    /// <returns>True if any interrupt flags are set, false otherwise.</returns>
    public bool HasAnyInterruptFlags()
    {
        return (_if & 0x1F) != 0; // Check lower 5 bits for any interrupt flags
    }

    /// <summary>
    /// Services an interrupt by clearing its IF bit and returning the interrupt vector address.
    /// Note: Actual CPU state manipulation (pushing PC, setting PC to vector) should be done by the CPU.
    /// </summary>
    /// <param name="interruptType">The interrupt type to service.</param>
    /// <returns>The interrupt vector address for the CPU to jump to.</returns>
    public ushort Service(InterruptType interruptType)
    {
        // Clear the IF bit for this interrupt
        byte bit = (byte)(1 << (int)interruptType);
        _if &= (byte)~bit;

        // Return the interrupt vector address
        return interruptType switch
        {
            InterruptType.VBlank => 0x0040,
            InterruptType.LCDStat => 0x0048,
            InterruptType.Timer => 0x0050,
            InterruptType.Serial => 0x0058,
            InterruptType.Joypad => 0x0060,
            _ => throw new ArgumentException($"Unknown interrupt type: {interruptType}")
        };
    }

    /// <summary>
    /// Initializes the interrupt controller to post-BIOS defaults.
    /// </summary>
    public void InitializePostBiosDefaults()
    {
        _if = 0x01; // VBlank flag set initially (post-BIOS state)
        _ie = 0x00; // No interrupts enabled initially
    }
}
