namespace GameBoy.Core;

/// <summary>
/// Handles interrupt enable/flags and dispatch for the emulator.
/// </summary>
public sealed class InterruptController
{
    public bool VBlankRequested { get; set; }
    public bool LcdStatRequested { get; set; }
    public bool TimerRequested { get; set; }
    public bool SerialRequested { get; set; }
    public bool JoypadRequested { get; set; }
}
