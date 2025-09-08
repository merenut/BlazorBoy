namespace GameBoy.Core;

/// <summary>
/// Interface for cartridges that support battery-backed external RAM.
/// Provides methods for loading and saving external RAM data.
/// </summary>
public interface IBatteryBacked
{
    /// <summary>
    /// Gets the external RAM data for saving to persistent storage.
    /// </summary>
    /// <returns>The external RAM data, or null if no RAM or no data to save.</returns>
    byte[]? GetExternalRam();

    /// <summary>
    /// Loads external RAM data from persistent storage.
    /// </summary>
    /// <param name="data">The external RAM data to load, or null if no save data exists.</param>
    void LoadExternalRam(byte[]? data);

    /// <summary>
    /// Gets whether the cartridge has battery-backed RAM.
    /// </summary>
    bool HasBattery { get; }

    /// <summary>
    /// Gets the size of the external RAM in bytes.
    /// </summary>
    int ExternalRamSize { get; }
}