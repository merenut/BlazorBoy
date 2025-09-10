using System.Threading.Tasks;

namespace GameBoy.Blazor.Services;

/// <summary>
/// Interface for persistence operations in the browser environment.
/// Abstracts localStorage and file operations for Game Boy save data.
/// </summary>
public interface IPersistenceService
{
    /// <summary>
    /// Saves battery-backed RAM data to browser storage.
    /// </summary>
    /// <param name="romKey">Unique ROM identifier</param>
    /// <param name="batteryData">Battery RAM data to save</param>
    /// <returns>True if successful</returns>
    Task<bool> SaveBatteryRamAsync(string romKey, byte[]? batteryData);

    /// <summary>
    /// Loads battery-backed RAM data from browser storage.
    /// </summary>
    /// <param name="romKey">Unique ROM identifier</param>
    /// <returns>Battery RAM data or null if not found</returns>
    Task<byte[]?> LoadBatteryRamAsync(string romKey);

    /// <summary>
    /// Saves save state data to browser storage.
    /// </summary>
    /// <param name="slotKey">Save slot identifier</param>
    /// <param name="saveStateData">Save state data to save</param>
    /// <returns>True if successful</returns>
    Task<bool> SaveSaveStateAsync(string slotKey, byte[] saveStateData);

    /// <summary>
    /// Loads save state data from browser storage.
    /// </summary>
    /// <param name="slotKey">Save slot identifier</param>
    /// <returns>Save state data or null if not found</returns>
    Task<byte[]?> LoadSaveStateAsync(string slotKey);

    /// <summary>
    /// Gets information about browser storage usage.
    /// </summary>
    /// <returns>Storage information</returns>
    Task<StorageInfo> GetStorageInfoAsync();

    /// <summary>
    /// Checks if persistence is available in the browser.
    /// </summary>
    /// <returns>True if localStorage is available</returns>
    Task<bool> IsAvailableAsync();
}

/// <summary>
/// Information about browser storage usage.
/// </summary>
public class StorageInfo
{
    public bool Available { get; set; }
    public int Used { get; set; }
    public int Total { get; set; }
    public int Percentage { get; set; }
}