using System;
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
    /// <summary>
    /// 设置保存完成事件 — MainViewModel 订阅此事件以刷新标签页。
    /// </summary>
    public static event EventHandler? SettingsSaved;

    private readonly ISettingsService _settingsService;

    // ─── 标签页 URL（共 4 个可自定义的浏览器标签页） ───

    /// <summary>标签页 0 URL（SC中文社区）。</summary>
    [ObservableProperty]
    private string _tabUrl0 = string.Empty;

    /// <summary>标签页 1 URL（SC联机号）。</summary>
    [ObservableProperty]
    private string _tabUrl1 = string.Empty;

    /// <summary>标签页 2 URL（SC导航网）。</summary>
    [ObservableProperty]
    private string _tabUrl2 = string.Empty;

    /// <summary>标签页 3 URL（工具，通常为空白）。</summary>
    [ObservableProperty]
    private string _tabUrl3 = string.Empty;

    // ─── 通用设置 ───

    /// <summary>应用主页 URL。</summary>
    [ObservableProperty]
    private string _homePageUrl = string.Empty;

    /// <summary>文件下载保存目录。</summary>
    [ObservableProperty]
    private string _downloadDirectory = string.Empty;

    /// <summary>最大并行下载数量。</summary>
    [ObservableProperty]
    private int _maxConcurrentDownloads = 3;

    /// <summary>是否启用下载历史记录。</summary>
    [ObservableProperty]
    private bool _enableDownloadHistory = true;

    /// <summary>是否启用广告拦截。</summary>
    [ObservableProperty]
    private bool _enableAdBlock;

    /// <summary>主题索引：0=跟随系统，1=浅色，2=深色。</summary>
    [ObservableProperty]
    private int _themeIndex;

    /// <summary>主题选项列表（供 UI 下拉选择）。</summary>
    [ObservableProperty]
    private ObservableCollection<string> _themeOptions = new()
    {
        "跟随系统", "浅色", "深色"
    };

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>从持久化存储异步加载设置并填充到各属性。</summary>
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

    /// <summary>将当前设置保存到持久化存储，并触发 SettingsSaved 事件通知 MainViewModel 刷新。</summary>
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

        // 通知订阅者（MainViewModel）设置已保存
        SettingsSaved?.Invoke(null, EventArgs.Empty);
    }
}
