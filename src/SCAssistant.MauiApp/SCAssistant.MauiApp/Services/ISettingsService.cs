namespace SCAssistant.Maui.Services;

/// <summary>
/// ISettingsService — 持久化应用设置的读写接口。
/// </summary>
public interface ISettingsService
{
    T? Get<T>(string key, T? defaultValue = default);
    void Set<T>(string key, T value);
    bool ContainsKey(string key);
    void Remove(string key);
    Task SaveAsync();
    Task LoadAsync();
}
