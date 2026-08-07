using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCAssistant.AvaloniaApp.Models;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.ViewModels;

/// <summary>
/// 主界面 ViewModel — 底部4标签页导航 + 浏览器顶部地址栏 + 设置面板叠加层。
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly IBrowserProvider _browser;
    private readonly ISettingsService _settings;

    /// <summary>默认标签页 URL（当用户未自定义时使用）。</summary>
    private static readonly string[] DefaultTabUrls = { "https://www.scbbs.top/", "https://www.sckey.net/", "https://scwz.top/", "" };

    /// <summary>底部 4 个标签页的中文名称。</summary>
    private static readonly string[] TabNames = { "SC中文社区", "SC联机号", "SC导航网", "工具" };

    /// <summary>当前地址栏 URL。</summary>
    [ObservableProperty]
    private string _currentUrl = string.Empty;

    /// <summary>当前选中的标签页索引（-1 表示未选中）。</summary>
    [ObservableProperty]
    private int _selectedTabIndex = -1;

    /// <summary>浏览器是否可以后退。</summary>
    [ObservableProperty]
    private bool _canGoBack;

    /// <summary>浏览器是否可以前进。</summary>
    [ObservableProperty]
    private bool _canGoForward;

    /// <summary>浏览器是否正在加载页面。</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>平台 WebView 是否已初始化就绪。</summary>
    [ObservableProperty]
    private bool _isBrowserReady;

    /// <summary>设置面板是否打开。</summary>
    [ObservableProperty]
    private bool _isSettingsOpen;

    /// <summary>底部标签页数据源。</summary>
    public ObservableCollection<TabItem> Tabs { get; } = new();

    /// <summary>
    /// 构造函数：注入浏览器服务和设置服务，订阅浏览器事件并初始化标签页。
    /// </summary>
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
        _browser.ReadyChanged += (_, _) =>
        {
            IsBrowserReady = _browser.IsReady;
            LogHelper.Info($"[MainVM] 浏览器就绪状态: {IsBrowserReady}");

            // 浏览器就绪后同步一次导航按钮状态
            CanGoBack = _browser.CanGoBack;
            CanGoForward = _browser.CanGoForward;
        };

        // 订阅设置保存事件
        SettingsViewModel.SettingsSaved += (_, _) => RefreshTabUrlsAsync();

        LogHelper.Info("[MainVM] 初始化 — 加载标签页配置...");
        InitializeTabs();
    }

    /// <summary>
    /// 从设置加载标签页 URL，若未配置则使用默认值。
    /// 加载完成后默认选中第一个标签页。
    /// </summary>
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

        for (var i = 0; i < 4; i++)
        {
            var url = i < urls.Length ? urls[i] : string.Empty;
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

        // 浏览器标签页 (0-2): 导航到对应 URL
        if (value <= 2)
        {
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
            return;
        }

        // 工具标签页 (3): 显示主页，不需要浏览器导航
        if (value == 3)
        {
            IsSettingsOpen = false;
            LogHelper.Info("[MainVM] 切换到工具主页");
            return;
        }
    }

    /// <summary>切换设置面板的显示/隐藏。</summary>
    [RelayCommand]
    private void ToggleSettings()
    {
        IsSettingsOpen = !IsSettingsOpen;
        LogHelper.Info($"[MainVM] 设置面板: {(IsSettingsOpen ? "打开" : "关闭")}");
    }

    /// <summary>设置保存后刷新标签页 URL。</summary>
    public async void RefreshTabUrlsAsync()
    {
        LogHelper.Info("[MainVM] 设置已保存，刷新标签页 URL");
        try
        {
            var settings = await _settings.GetSettingsAsync();
            var urls = (settings.TabUrls != null && settings.TabUrls.Length >= 4)
                ? settings.TabUrls
                : DefaultTabUrls;

            for (var i = 0; i < Tabs.Count && i < 4; i++)
            {
                var newUrl = i < urls.Length ? urls[i] : string.Empty;
                if (Tabs[i].Url != newUrl)
                {
                    LogHelper.Debug($"[MainVM] 标签[{i}] '{Tabs[i].Name}': {Tabs[i].Url} → {newUrl}");
                    Tabs[i].Url = newUrl;
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error("[MainVM] 刷新标签页 URL 失败", ex);
        }
    }

    /// <summary>地址栏回车 / Go 按钮。</summary>
    [RelayCommand]
    private void NavigateToUrl(string? url = null)
    {
        var target = url?.Trim() ?? CurrentUrl?.Trim();
        if (string.IsNullOrWhiteSpace(target))
        {
            LogHelper.Warn("[MainVM] 导航取消: URL 为空");
            return;
        }

        // 自动补全协议
        if (!target.StartsWith("http://") && !target.StartsWith("https://") && !target.StartsWith("file://"))
        {
            target = "https://" + target;
            CurrentUrl = target;
        }

        LogHelper.Info($"[MainVM] 地址栏导航: {target}");
        _browser.Navigate(target);
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
}
