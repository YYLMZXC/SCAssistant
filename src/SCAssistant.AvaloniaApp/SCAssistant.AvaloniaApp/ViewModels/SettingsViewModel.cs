using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCAssistant.AvaloniaApp.Models;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.ViewModels;

/// <summary>
/// 设置面板视图模型
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private AppSettings _settings = new();

    [ObservableProperty]
    private string _downloadDirectory = string.Empty;

    [ObservableProperty]
    private int _maxConcurrentDownloads = 3;

    [ObservableProperty]
    private bool _enableDownloadHistory = true;

    [ObservableProperty]
    private string _defaultSearchEngine = "https://www.google.com/search?q=";

    [ObservableProperty]
    private bool _enableAdBlock;

    [ObservableProperty]
    private string _homePageUrl = "https://www.google.com";

    [ObservableProperty]
    private int _selectedThemeIndex;

    [ObservableProperty]
    private ObservableCollection<string> _themeOptions = new()
    {
        "跟随系统", "浅色", "深色"
    };

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        Title = "设置";
    }

    public AppSettings Settings => _settings;

    public async Task InitializeAsync()
    {
        _settings = await _settingsService.GetSettingsAsync();

        DownloadDirectory = _settings.DownloadDirectory;
        MaxConcurrentDownloads = _settings.MaxConcurrentDownloads;
        EnableDownloadHistory = _settings.EnableDownloadHistory;
        DefaultSearchEngine = _settings.DefaultSearchEngine;
        EnableAdBlock = _settings.EnableAdBlock;
        HomePageUrl = _settings.HomePageUrl;

        SelectedThemeIndex = _settings.Theme switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0
        };
    }

    [RelayCommand]
    private async Task SelectDownloadDirectory()
    {
        // 在Avalonia中使用FolderPicker或系统文件夹对话框
        // 这里保留为平台相关的文件夹选择
        try
        {
            var folder = await GetFolderFromUserAsync();
            if (!string.IsNullOrEmpty(folder))
            {
                DownloadDirectory = folder;
            }
        }
        catch { }
    }

    [RelayCommand]
    private async Task Save()
    {
        _settings.DownloadDirectory = DownloadDirectory;
        _settings.MaxConcurrentDownloads = MaxConcurrentDownloads;
        _settings.EnableDownloadHistory = EnableDownloadHistory;
        _settings.DefaultSearchEngine = DefaultSearchEngine;
        _settings.EnableAdBlock = EnableAdBlock;
        _settings.HomePageUrl = HomePageUrl;
        _settings.Theme = SelectedThemeIndex switch
        {
            1 => "Light",
            2 => "Dark",
            _ => "System"
        };

        await _settingsService.SaveSettingsAsync(_settings);
    }

    [RelayCommand]
    private void Cancel()
    {
        // 由父级ViewModel处理关闭
    }

    private static async Task<string> GetFolderFromUserAsync()
    {
        // 使用Avalonia的StorageProvider来选择文件夹
        // 这个需要在View层通过TopLevel.GetTopLevel来获取
        var tcs = new TaskCompletionSource<string>();
        // Platform-specific folder picker would be invoked here
        await Task.CompletedTask;
        return string.Empty;
    }
}
