using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCAssistant.AvaloniaApp.Models;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.ViewModels;

/// <summary>
/// 主页面 ViewModel — 整合浏览器控制、网址栏、下载请求、设置面板。
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IDownloadHistoryService _historyService;
    private readonly IDownloadService _downloadService;
    private readonly IBrowserProvider _browserProvider;

    #region Browser State

    [ObservableProperty]
    private string _addressBarUrl = string.Empty;

    [ObservableProperty]
    private string _pageTitle = "SC 助手";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _canGoBack;

    [ObservableProperty]
    private bool _canGoForward;

    [ObservableProperty]
    private string _currentUrl = string.Empty;

    #endregion

    #region Settings

    /// <summary>应用设置（持久化模型）。</summary>
    [ObservableProperty]
    private AppSettings _settings = new();

    /// <summary>主题选项列表。</summary>
    [ObservableProperty]
    private ObservableCollection<string> _themeOptions = new()
    {
        "跟随系统", "浅色", "深色"
    };

    /// <summary>主题索引。</summary>
    [ObservableProperty]
    private int _themeIndex;

    /// <summary>设置面板是否打开。</summary>
    [ObservableProperty]
    private bool _isSettingsOpen;

    #endregion

    #region Downloads

    /// <summary>下载记录集合。</summary>
    [ObservableProperty]
    private ObservableCollection<DownloadRecord> _downloadRecords = new();

    /// <summary>用户输入的下载 URL。</summary>
    [ObservableProperty]
    private string _downloadUrl = string.Empty;

    /// <summary>下载面板是否打开。</summary>
    [ObservableProperty]
    private bool _isDownloadPanelOpen;

    #endregion

    #region Service Properties

    public IBrowserProvider BrowserProvider => _browserProvider;

    #endregion

    public MainViewModel(
        ISettingsService settingsService,
        IDownloadHistoryService historyService,
        IDownloadService downloadService,
        IBrowserProvider browserProvider)
    {
        _settingsService = settingsService;
        _historyService = historyService;
        _downloadService = downloadService;
        _browserProvider = browserProvider;

        Title = "SC 助手";

        // 订阅浏览器事件
        SubscribeToBrowserEvents();
    }

    /// <summary>
    /// 初始化 — 加载设置和下载历史。
    /// </summary>
    public async Task InitializeAsync()
    {
        // 加载设置
        var settings = await _settingsService.GetSettingsAsync();
        Settings = settings;
        ThemeIndex = settings.ThemeIndex;

        CurrentUrl = Settings.HomePageUrl;
        AddressBarUrl = Settings.HomePageUrl;

        // 加载下载历史
        await LoadDownloadRecordsAsync();

        LogHelper.Info("[MainVM] 初始化完成");
    }

    private async Task LoadDownloadRecordsAsync()
    {
        var records = await _historyService.GetRecordsAsync();
        DownloadRecords = new ObservableCollection<DownloadRecord>(records);
    }

    private void SubscribeToBrowserEvents()
    {
        _browserProvider.AddressChanged += (_, url) =>
        {
            if (!string.IsNullOrEmpty(url))
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    CurrentUrl = url;
                    AddressBarUrl = url;
                });
            }
        };

        _browserProvider.TitleChanged += (_, title) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                PageTitle = title ?? "SC 助手";
            });
        };

        _browserProvider.LoadingStateChanged += (_, isLoading) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => { IsLoading = isLoading; });
        };

        _browserProvider.NavigationHistoryChanged += (_, _) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                CanGoBack = _browserProvider.CanGoBack;
                CanGoForward = _browserProvider.CanGoForward;
            });
        };

        _browserProvider.DownloadRequested += (_, url) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (!string.IsNullOrEmpty(url))
                {
                    DownloadUrl = url;
                    IsDownloadPanelOpen = true;
                    LogHelper.Info($"[MainVM] 下载请求: {url}");
                }
            });
        };

        LogHelper.Info("[MainVM] 浏览器事件已订阅");
    }

    #region Navigation Commands

    [RelayCommand]
    private void NavigateToUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        LogHelper.Info($"[MainVM] 导航到: {url}");

        // 智能 URL 补全
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // 判断是搜索词还是网址
            if (url.Contains(' ') || !Uri.TryCreate($"https://{url}", UriKind.Absolute, out _))
            {
                url = Settings.DefaultSearchEngine + Uri.EscapeDataString(url);
            }
            else
            {
                url = "https://" + url;
            }
        }

        CurrentUrl = url;
        AddressBarUrl = url;
        _browserProvider.Navigate(url);
    }

    [RelayCommand]
    private void GoBack()
    {
        LogHelper.Info("[MainVM] 后退");
        _browserProvider.GoBack();
    }

    [RelayCommand]
    private void GoForward()
    {
        LogHelper.Info("[MainVM] 前进");
        _browserProvider.GoForward();
    }

    [RelayCommand]
    private void Refresh()
    {
        LogHelper.Info("[MainVM] 刷新");
        _browserProvider.Reload();
    }

    [RelayCommand]
    private void GoHome()
    {
        LogHelper.Info($"[MainVM] 返回主页: {Settings.HomePageUrl}");
        NavigateToUrl(Settings.HomePageUrl);
    }

    /// <summary>
    /// 打开当前页面在系统浏览器中。
    /// </summary>
    [RelayCommand]
    private void OpenInSystemBrowser(string? url)
    {
        var targetUrl = url ?? CurrentUrl;
        if (string.IsNullOrWhiteSpace(targetUrl)) return;

        LogHelper.Info($"[MainVM] 系统浏览器打开: {targetUrl}");
        SystemBrowserProvider.OpenUrl(targetUrl);
    }

    #endregion

    #region Settings Commands

    [RelayCommand]
    private void ToggleSettings()
    {
        IsSettingsOpen = !IsSettingsOpen;
        LogHelper.Info($"[MainVM] 设置面板: {(IsSettingsOpen ? "打开" : "关闭")}");
    }

    [RelayCommand]
    private async Task SaveSettings()
    {
        Settings.ThemeIndex = ThemeIndex;
        await _settingsService.SaveSettingsAsync(Settings);
        IsSettingsOpen = false;
        LogHelper.Info("[MainVM] 设置已保存");

        // 应用主题
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        // 主题变更逻辑
        var theme = Settings.Theme switch
        {
            "Light" => Avalonia.Styling.ThemeVariant.Light,
            "Dark" => Avalonia.Styling.ThemeVariant.Dark,
            _ => Avalonia.Styling.ThemeVariant.Default
        };

        if (Avalonia.Application.Current is not null)
        {
            Avalonia.Application.Current.RequestedThemeVariant = theme;
        }

        LogHelper.Info($"[MainVM] 主题已应用: {Settings.Theme}");
    }

    #endregion

    #region Download Commands

    [RelayCommand]
    private void ToggleDownloadPanel()
    {
        IsDownloadPanelOpen = !IsDownloadPanelOpen;
        LogHelper.Info($"[MainVM] 下载面板: {(IsDownloadPanelOpen ? "打开" : "关闭")}");
    }

    [RelayCommand]
    private async Task StartDownload()
    {
        var url = DownloadUrl?.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            url = CurrentUrl;
        }
        if (string.IsNullOrWhiteSpace(url)) return;

        var fileName = ExtractFileName(url);
        LogHelper.Info($"[MainVM] 开始下载: {fileName}");

        var record = new DownloadRecord
        {
            Id = Guid.NewGuid().ToString("N")[..12],
            Url = url,
            FileName = fileName,
            State = DownloadState.Pending,
            DownloadTime = DateTime.Now
        };

        await _historyService.AddRecordAsync(record);
        DownloadRecords.Add(record);

        try
        {
            var savePath = System.IO.Path.Combine(
                string.IsNullOrWhiteSpace(Settings.DownloadDirectory)
                    ? System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
                    : Settings.DownloadDirectory,
                fileName);

            await _downloadService.StartDownloadAsync(url, savePath);

            record.State = DownloadState.Completed;
            record.CompletedTime = DateTime.Now;
            record.LocalPath = savePath;
            await _historyService.UpdateRecordAsync(record);
            LogHelper.Info($"[MainVM] 下载完成: {fileName}");
        }
        catch (Exception ex)
        {
            record.State = DownloadState.Failed;
            record.ErrorMessage = ex.Message;
            LogHelper.Error($"[MainVM] 下载失败: {fileName}", ex);
        }
    }

    [RelayCommand]
    private async Task ClearDownloadHistory()
    {
        await _historyService.ClearAllAsync();
        DownloadRecords.Clear();
        LogHelper.Info("[MainVM] 下载历史已清空");
    }

    private static string ExtractFileName(string url)
    {
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            var name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrWhiteSpace(name) && name != "/")
                return Uri.UnescapeDataString(name);
        }
        catch { }

        return $"download_{DateTime.Now:yyyyMMddHHmmss}";
    }

    #endregion
}
