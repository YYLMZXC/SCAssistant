using CommunityToolkit.Mvvm.ComponentModel;
using SCAssistant.Maui.Services;

namespace SCAssistant.Maui.ViewModels;

/// <summary>
/// SettingsViewModel — 应用设置管理。
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;

    [ObservableProperty]
    private bool _darkMode;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private int _maxDownloadThreads = 3;

    [ObservableProperty]
    private string _downloadDirectory = string.Empty;

    public SettingsViewModel(ISettingsService settings)
    {
        _settings = settings;
        LoadSettings();
    }

    private void LoadSettings()
    {
        DarkMode = _settings.Get("DarkMode", false);
        ZoomLevel = _settings.Get("ZoomLevel", 1.0);
        MaxDownloadThreads = _settings.Get("MaxDownloadThreads", 3);
        DownloadDirectory = _settings.Get("DownloadDirectory", FileSystem.CacheDirectory);
    }

    /// <summary>
    /// 持久化保存设置。
    /// </summary>
    public async Task SaveAsync()
    {
        _settings.Set("DarkMode", DarkMode);
        _settings.Set("ZoomLevel", ZoomLevel);
        _settings.Set("MaxDownloadThreads", MaxDownloadThreads);
        _settings.Set("DownloadDirectory", DownloadDirectory);
        await _settings.SaveAsync();
        LogHelper.Info("[SettingsVM] 设置已保存");
    }
}
