using System.Text.Json;
using Microsoft.JSInterop;

namespace ProSushiMsg.Client.Services;

/// <summary>
/// Обёртка вокруг localStorage для сохранения данных в браузере (PWA).
/// </summary>
public class LocalStorageService
{
    private readonly IJSRuntime _jsRuntime;

    public LocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        Console.WriteLine("? LocalStorageService создан");
    }

    /// <summary>
    /// Сохраняет значение в localStorage.
    /// </summary>
    public async Task SetAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
    }

    /// <summary>
    /// Получает значение из localStorage.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
            return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Получает строку из localStorage.
    /// </summary>
    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Удаляет значение из localStorage.
    /// </summary>
    public async Task RemoveAsync(string key)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
    }

    /// <summary>
    /// Очищает всё localStorage.
    /// </summary>
    public async Task ClearAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.clear");
    }
}
