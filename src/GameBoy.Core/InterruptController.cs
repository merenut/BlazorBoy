namespace GameBoy.Core;

/// <summary>
/// Represents the five core Game Boy interrupt types in priority order.
/// </summary>
public enum InterruptType : byte
{
    VBlank = 0,    // Bit 0, Vector 0x0040, Highest Priority
    LCDStat = 1,   // Bit 1, Vector 0x0048
    Timer = 2,     // Bit 2, Vector 0x0050
    Serial = 3,    // Bit 3, Vector 0x0058
    Joypad = 4     // Bit 4, Vector 0x0060, Lowest Priority
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
        _if = 0x01; // Only VBlank bit set initially
        _ie = 0x00; // No interrupts enabled initially
    }
}
