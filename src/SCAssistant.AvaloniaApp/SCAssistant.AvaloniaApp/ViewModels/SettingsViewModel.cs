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
    private readonly ISettingsService _settingsService;
    private readonly IDownloadHistoryService _historyService;

    [ObservableProperty]
    private string _homePageUrl = string.Empty;

    [ObservableProperty]
    private string _defaultSearchEngine = string.Empty;

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

    public SettingsViewModel(
        ISettingsService settingsService,
        IDownloadHistoryService historyService)
    {
        _settingsService = settingsService;
        _historyService = historyService;
    }

    public async Task LoadAsync()
    {
        var settings = await _settingsService.GetSettingsAsync();
        HomePageUrl = settings.HomePageUrl;
        DefaultSearchEngine = settings.DefaultSearchEngine;
        DownloadDirectory = settings.DownloadDirectory;
        MaxConcurrentDownloads = settings.MaxConcurrentDownloads;
        EnableDownloadHistory = settings.EnableDownloadHistory;
        EnableAdBlock = settings.EnableAdBlock;
        ThemeIndex = settings.ThemeIndex;

        LogHelper.Info("[SettingsVM] 设置已加载");
    }

    [RelayCommand]
    private async Task Save()
    {
        var settings = new AppSettings
        {
            HomePageUrl = HomePageUrl,
            DefaultSearchEngine = DefaultSearchEngine,
            DownloadDirectory = DownloadDirectory,
            MaxConcurrentDownloads = MaxConcurrentDownloads,
            EnableDownloadHistory = EnableDownloadHistory,
            EnableAdBlock = EnableAdBlock,
            ThemeIndex = ThemeIndex
        };

        await _settingsService.SaveSettingsAsync(settings);
        LogHelper.Info("[SettingsVM] 设置已保存");
    }

    [RelayCommand]
    private async Task ClearHistory()
    {
        await _historyService.ClearAllAsync();
        LogHelper.Info("[SettingsVM] 下载历史已清空");
    }
}
