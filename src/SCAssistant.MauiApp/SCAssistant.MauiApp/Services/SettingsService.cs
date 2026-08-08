using System.Text.Json;

namespace SCAssistant.Maui.Services;

/// <summary>
/// SettingsService — 基于 JSON 文件的设置持久化实现。
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _filePath;
    private Dictionary<string, object?> _store = new();

    public SettingsService()
    {
        _filePath = Path.Combine(FileSystem.AppDataDirectory, "settings.json");
    }

    public T? Get<T>(string key, T? defaultValue = default)
    {
        if (_store.TryGetValue(key, out var value) && value is JsonElement jsonElement)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
            }
            catch
            {
                return defaultValue;
            }
        }

        if (value is T typedValue)
            return typedValue;

        return defaultValue;
    }

    public void Set<T>(string key, T value)
    {
        _store[key] = value;
    }

    public bool ContainsKey(string key) => _store.ContainsKey(key);

    public void Remove(string key) => _store.Remove(key);

    public async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(_store, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_filePath, json);
    }

    public async Task LoadAsync()
    {
        if (File.Exists(_filePath))
        {
            var json = await File.ReadAllTextAsync(_filePath);
            _store = JsonSerializer.Deserialize<Dictionary<string, object?>>(json)
                     ?? new Dictionary<string, object?>();
        }
    }
}
