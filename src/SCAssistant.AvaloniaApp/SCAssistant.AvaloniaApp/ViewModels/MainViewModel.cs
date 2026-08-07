using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCAssistant.AvaloniaApp.Models;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.ViewModels;

/// <summary>
/// 主界面 ViewModel — 底部5标签页导航 + 顶部地址栏 + 设置面板。
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly IBrowserProvider _browser;
    private readonly ISettingsService _settings;

    private static readonly string[] DefaultTabUrls = { "https://www.scbbs.top/", "https://www.sckey.net/", "https://scwz.top/", "" };
    private static readonly string[] TabNames = { "SC中文社区", "SC联机号", "SC导航网", "工具", "设置" };

    [ObservableProperty]
    private string _currentUrl = string.Empty;

    [ObservableProperty]
    private int _selectedTabIndex = -1;

    [ObservableProperty]
    private bool _canGoBack;

    [ObservableProperty]
    private bool _canGoForward;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSettingsOpen;

    /// <summary>底部标签页数据源。</summary>
    public ObservableCollection<TabItem> Tabs { get; } = new();

    public MainViewModel(IBrowserProvider browser, ISettingsService settings)
    {
        _browser = browser;
        _settings = settings;

        _browser.AddressChanged += (_, url) =>
        {
            CurrentUrl = url;
            LogHelper.Debug($"[MainVM] 地址变更: {url}");
        };
        _browser.TitleChanged += (_, title) =>
        {
            LogHelper.Debug($"[MainVM] 标题变更: {title}");
        };
        _browser.LoadingStateChanged += (_, loading) =>
        {
            IsLoading = loading;
            LogHelper.Debug($"[MainVM] 加载状态: {(loading ? "加载中" : "完成")}");
        };
        _browser.NavigationHistoryChanged += (_, _) =>
        {
            CanGoBack = _browser.CanGoBack;
            CanGoForward = _browser.CanGoForward;
        };
        _browser.DownloadRequested += (_, url) =>
        {
            LogHelper.Info($"[MainVM] 下载请求: {url}");
        };

        LogHelper.Info("[MainVM] 初始化 — 加载标签页配置...");
        InitializeTabs();
    }

    private async void InitializeTabs()
    {
        AppSettings appSettings;
        try
        {
            appSettings = await _settings.GetSettingsAsync();
        }
        catch (Exception ex)
        {
            LogHelper.Error("[MainVM] 加载设置失败，使用默认配置", ex);
            appSettings = new AppSettings();
        }

        var urls = (appSettings.TabUrls != null && appSettings.TabUrls.Length >= 4)
            ? appSettings.TabUrls
            : DefaultTabUrls;

        for (var i = 0; i < 5; i++)
        {
            var url = i < 4 && i < urls.Length ? urls[i] : string.Empty;
            Tabs.Add(new TabItem { Name = TabNames[i], Url = url });
        }

        // 默认选中第一个标签页
        SelectedTabIndex = 0;
        LogHelper.Info($"[MainVM] 标签页初始化完成，共 {Tabs.Count} 个标签");
    }

    /// <summary>标签页切换。</summary>
    partial void OnSelectedTabIndexChanged(int value)
    {
        if (value < 0 || value >= Tabs.Count) return;

        var tabName = Tabs[value].Name;
        LogHelper.Info($"[MainVM] 切换标签: [{value}] {tabName}");

        // 第5个标签是设置，打开设置面板
        if (value == 4)
        {
            IsSettingsOpen = true;
            LogHelper.Info("[MainVM] 打开设置面板");
            return;
        }

        IsSettingsOpen = false;
        var url = Tabs[value].Url;
        if (!string.IsNullOrWhiteSpace(url))
        {
            CurrentUrl = url;
            _browser.Navigate(url);
        }
        else
        {
            LogHelper.Warn($"[MainVM] 标签 '{tabName}' URL 为空，跳过导航");
        }
    }

    /// <summary>关闭设置面板。</summary>
    [RelayCommand]
    private void CloseSettings()
    {
        LogHelper.Info("[MainVM] 关闭设置面板，返回首页");
        IsSettingsOpen = false;
        SelectedTabIndex = 0;
    }

    /// <summary>设置保存后刷新标签页 URL。</summary>
    public void RefreshTabUrls()
    {
        LogHelper.Info("[MainVM] 设置已保存，刷新标签页 URL");
        for (var i = 0; i < Tabs.Count && i < 4; i++)
        {
            if (Tabs[i].Url != _savedTabUrls[i])
            {
                LogHelper.Debug($"[MainVM] 标签[{i}] '{Tabs[i].Name}': {Tabs[i].Url} → {_savedTabUrls[i]}");
                Tabs[i].Url = _savedTabUrls[i];
            }
        }
    }

    private string[] _savedTabUrls = DefaultTabUrls;

    /// <summary>地址栏回车 / Go 按钮。</summary>
    [RelayCommand]
    private void NavigateToUrl()
    {
        var url = CurrentUrl?.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            LogHelper.Warn("[MainVM] 导航取消: URL 为空");
            return;
        }
        LogHelper.Info($"[MainVM] 地址栏导航: {url}");
        _browser.Navigate(url);
    }

    [RelayCommand]
    private void GoBack()
    {
        LogHelper.Debug("[MainVM] 后退");
        _browser.GoBack();
    }

    [RelayCommand]
    private void GoForward()
    {
        LogHelper.Debug("[MainVM] 前进");
        _browser.GoForward();
    }

    [RelayCommand]
    private void GoToTab0() => SelectedTabIndex = 0;

    [RelayCommand]
    private void GoToTab1() => SelectedTabIndex = 1;

    [RelayCommand]
    private void GoToTab2() => SelectedTabIndex = 2;

    [RelayCommand]
    private void GoToTab3() => SelectedTabIndex = 3;

    [RelayCommand]
    private void GoToTab4() => SelectedTabIndex = 4;
}
