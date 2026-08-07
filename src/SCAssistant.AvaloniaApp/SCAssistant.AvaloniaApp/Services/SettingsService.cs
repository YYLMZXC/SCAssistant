using Newtonsoft.Json;
using SCAssistant.AvaloniaApp.Models;

namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 设置服务实现 - 使用JSON文件持久化
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private AppSettings _settings = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SettingsService()
    {
        _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SCAssistant",
            "settings.json");

        LoadSettings();
    }

    public async Task<AppSettings> GetSettingsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return new AppSettings
            {
                DownloadDirectory = _settings.DownloadDirectory,
                MaxConcurrentDownloads = _settings.MaxConcurrentDownloads,
                EnableDownloadHistory = _settings.EnableDownloadHistory,
                DefaultSearchEngine = _settings.DefaultSearchEngine,
                EnableAdBlock = _settings.EnableAdBlock,
                HomePageUrl = _settings.HomePageUrl,
                Theme = _settings.Theme
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        await _lock.WaitAsync();
        try
        {
            _settings = settings;
            await SaveToFileAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ResetToDefaultsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _settings = new AppSettings();
            await SaveToFileAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    private void LoadSettings()
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                _settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            _settings = new AppSettings();
        }
    }

    private async Task SaveToFileAsync()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
            await File.WriteAllTextAsync(_settingsPath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }
}
