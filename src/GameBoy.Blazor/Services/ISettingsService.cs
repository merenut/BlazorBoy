namespace GameBoy.Blazor.Services;

/// <summary>
/// Interface for managing UI settings persistence in localStorage.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets or sets the emulator speed multiplier.
    /// </summary>
    Task<double> GetSpeedMultiplierAsync();
    Task SetSpeedMultiplierAsync(double speed);

    /// <summary>
    /// Gets or sets the audio mute state.
    /// </summary>
    Task<bool> GetAudioMutedAsync();
    Task SetAudioMutedAsync(bool muted);

    /// <summary>
    /// Gets or sets the audio volume (0.0 to 1.0).
    /// </summary>
    Task<double> GetAudioVolumeAsync();
    Task SetAudioVolumeAsync(double volume);

    /// <summary>
    /// Gets or sets the last loaded ROM filename.
    /// </summary>
    Task<string?> GetLastRomNameAsync();
    Task SetLastRomNameAsync(string? romName);

    /// <summary>
    /// Gets or sets input key mappings.
    /// </summary>
    Task<Dictionary<string, string>> GetInputMappingsAsync();
    Task SetInputMappingsAsync(Dictionary<string, string> mappings);

    /// <summary>
    /// Gets or sets touch overlay visibility preference.
    /// </summary>
    Task<bool> GetTouchOverlayEnabledAsync();
    Task SetTouchOverlayEnabledAsync(bool enabled);

    /// <summary>
    /// Clears all stored settings.
    /// </summary>
    Task ClearAllSettingsAsync();
}