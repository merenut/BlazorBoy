namespace GameBoy.Core;

/// <summary>
/// Interface for Game Boy audio channels.
/// </summary>
public interface IAudioChannel
{
    /// <summary>
    /// Gets whether the channel is currently enabled and producing audio.
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    /// Steps the channel by the given number of CPU cycles.
    /// </summary>
    /// <param name="cycles">Number of CPU cycles to step</param>
    void Step(int cycles);

    /// <summary>
    /// Gets the current audio sample from this channel (range: -1.0 to 1.0).
    /// </summary>
    /// <returns>Current audio sample</returns>
    float GetSample();

    /// <summary>
    /// Triggers the channel to restart playing with current register settings.
    /// </summary>
    void Trigger();

    /// <summary>
    /// Steps the length counter (called by frame sequencer at 256 Hz).
    /// </summary>
    void StepLengthCounter();

    /// <summary>
    /// Steps the volume envelope (called by frame sequencer at 64 Hz).
    /// </summary>
    void StepVolumeEnvelope();
}