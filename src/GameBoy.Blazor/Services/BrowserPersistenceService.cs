using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace GameBoy.Blazor.Services;

/// <summary>
/// Browser-based implementation of persistence service using localStorage via JavaScript interop.
/// </summary>
public class BrowserPersistenceService : IPersistenceService
{
    private readonly IJSRuntime _jsRuntime;

    public BrowserPersistenceService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<bool> SaveBatteryRamAsync(string romKey, byte[]? batteryData)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("gbPersistence.saveBatteryRam", romKey, batteryData);
        }
        catch
        {
            return false;
        }
    }

    public async Task<byte[]?> LoadBatteryRamAsync(string romKey)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<byte[]?>("gbPersistence.loadBatteryRam", romKey);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> SaveSaveStateAsync(string slotKey, byte[] saveStateData)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("gbPersistence.saveSaveState", slotKey, saveStateData);
        }
        catch
        {
            return false;
        }
    }

    public async Task<byte[]?> LoadSaveStateAsync(string slotKey)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<byte[]?>("gbPersistence.loadSaveState", slotKey);
        }
        catch
        {
            return null;
        }
    }

    public async Task<StorageInfo> GetStorageInfoAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<StorageInfo>("gbPersistence.getStorageInfo");
        }
        catch
        {
            return new StorageInfo { Available = false };
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("gbPersistence.isAvailable");
        }
        catch
        {
            return false;
        }
    }
}