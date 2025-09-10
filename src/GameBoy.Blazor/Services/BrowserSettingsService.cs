using Microsoft.JSInterop;
using System.Text.Json;

namespace GameBoy.Blazor.Services;

/// <summary>
/// Implementation of ISettingsService using browser localStorage.
/// </summary>
public class BrowserSettingsService : ISettingsService
{
    private readonly IJSRuntime _jsRuntime;
    private const string KeyPrefix = "blazorboy_";

    // Default input mappings
    private static readonly Dictionary<string, string> DefaultInputMappings = new()
    {
        { "Up", "ArrowUp" },
        { "Down", "ArrowDown" },
        { "Left", "ArrowLeft" },
        { "Right", "ArrowRight" },
        { "A", "z" },
        { "B", "x" },
        { "Start", "Enter" },
        { "Select", "Shift" }
    };

    public BrowserSettingsService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<double> GetSpeedMultiplierAsync()
    {
        var value = await GetSettingAsync<double?>("speed_multiplier");
        return value ?? 1.0;
    }

    public async Task SetSpeedMultiplierAsync(double speed)
    {
        await SetSettingAsync("speed_multiplier", speed);
    }

    public async Task<bool> GetAudioMutedAsync()
    {
        var value = await GetSettingAsync<bool?>("audio_muted");
        return value ?? false;
    }

    public async Task SetAudioMutedAsync(bool muted)
    {
        await SetSettingAsync("audio_muted", muted);
    }

    public async Task<double> GetAudioVolumeAsync()
    {
        var value = await GetSettingAsync<double?>("audio_volume");
        return value ?? 0.5;
    }

    public async Task SetAudioVolumeAsync(double volume)
    {
        await SetSettingAsync("audio_volume", volume);
    }

    public async Task<string?> GetLastRomNameAsync()
    {
        return await GetSettingAsync<string?>("last_rom_name");
    }

    public async Task SetLastRomNameAsync(string? romName)
    {
        await SetSettingAsync("last_rom_name", romName);
    }

    public async Task<Dictionary<string, string>> GetInputMappingsAsync()
    {
        var value = await GetSettingAsync<Dictionary<string, string>?>("input_mappings");
        return value ?? new Dictionary<string, string>(DefaultInputMappings);
    }

    public async Task SetInputMappingsAsync(Dictionary<string, string> mappings)
    {
        await SetSettingAsync("input_mappings", mappings);
    }

    public async Task<bool> GetTouchOverlayEnabledAsync()
    {
        var value = await GetSettingAsync<bool?>("touch_overlay_enabled");
        return value ?? false; // Default to disabled, will auto-enable on mobile
    }

    public async Task SetTouchOverlayEnabledAsync(bool enabled)
    {
        await SetSettingAsync("touch_overlay_enabled", enabled);
    }

    public async Task ClearAllSettingsAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("eval", $@"
                for (let i = localStorage.length - 1; i >= 0; i--) {{
                    const key = localStorage.key(i);
                    if (key && key.startsWith('{KeyPrefix}')) {{
                        localStorage.removeItem(key);
                    }}
                }}
            ");
        }
        catch (JSException)
        {
            // localStorage might not be available
        }
    }

    private async Task<T> GetSettingAsync<T>(string key)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", KeyPrefix + key);
            if (string.IsNullOrEmpty(json))
                return default!;

            return JsonSerializer.Deserialize<T>(json) ?? default!;
        }
        catch (JSException)
        {
            return default!;
        }
    }

    private async Task SetSettingAsync<T>(string key, T value)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", KeyPrefix + key, json);
        }
        catch (JSException)
        {
            // localStorage might not be available - ignore silently
        }
    }
}