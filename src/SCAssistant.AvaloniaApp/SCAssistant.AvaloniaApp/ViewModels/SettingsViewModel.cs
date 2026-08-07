using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCAssistant.AvaloniaApp.Models;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.ViewModels;

/// <summary>
/// 设置面板 ViewModel。
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    // ─── 标签页 URL ───
    [ObservableProperty]
    private string _tabUrl0 = string.Empty;

    [ObservableProperty]
    private string _tabUrl1 = string.Empty;

    [ObservableProperty]
    private string _tabUrl2 = string.Empty;

    [ObservableProperty]
    private string _tabUrl3 = string.Empty;

    // ─── 通用设置 ───
    [ObservableProperty]
    private string _homePageUrl = string.Empty;

    [ObservableProperty]
    private string _downloadDirectory = string.Empty;

    [ObservableProperty]
    private int _maxConcurrentDownloads = 3;

    [ObservableProperty]
    private bool _enableDownloadHistory = true;

    [ObservableProperty]
    private bool _enableAdBlock;

    [ObservableProperty]
    private int _themeIndex;

    [ObservableProperty]
    private ObservableCollection<string> _themeOptions = new()
    {
        "跟随系统", "浅色", "深色"
    };

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task LoadAsync()
    {
        LogHelper.Info("[SettingsVM] 加载设置...");
        try
        {
            var s = await _settingsService.GetSettingsAsync();
            HomePageUrl = s.HomePageUrl;
            DownloadDirectory = s.DownloadDirectory;
            MaxConcurrentDownloads = s.MaxConcurrentDownloads;
            EnableDownloadHistory = s.EnableDownloadHistory;
            EnableAdBlock = s.EnableAdBlock;
            ThemeIndex = s.ThemeIndex;

            // 标签页 URL（4个）
            if (s.TabUrls != null)
            {
                if (s.TabUrls.Length > 0) TabUrl0 = s.TabUrls[0];
                if (s.TabUrls.Length > 1) TabUrl1 = s.TabUrls[1];
                if (s.TabUrls.Length > 2) TabUrl2 = s.TabUrls[2];
                if (s.TabUrls.Length > 3) TabUrl3 = s.TabUrls[3];
            }

            LogHelper.Info($"[SettingsVM] 已加载: 首页={s.HomePageUrl}, 最大下载={s.MaxConcurrentDownloads}, 主题={s.ThemeIndex}");
        }
        catch (Exception ex)
        {
            LogHelper.Error("[SettingsVM] 加载设置失败", ex);
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        var settings = new AppSettings
        {
            HomePageUrl = HomePageUrl,
            DownloadDirectory = DownloadDirectory,
            MaxConcurrentDownloads = MaxConcurrentDownloads,
            EnableDownloadHistory = EnableDownloadHistory,
            EnableAdBlock = EnableAdBlock,
            ThemeIndex = ThemeIndex,
            TabUrls = new[] { TabUrl0, TabUrl1, TabUrl2, TabUrl3 }
        };

        LogHelper.Info($"[SettingsVM] 保存设置: 标签URLs=[{TabUrl0}, {TabUrl1}, {TabUrl2}, {TabUrl3}], 首页={HomePageUrl}");
        await _settingsService.SaveSettingsAsync(settings);
        LogHelper.Info("[SettingsVM] 设置保存完成");
    }
}
