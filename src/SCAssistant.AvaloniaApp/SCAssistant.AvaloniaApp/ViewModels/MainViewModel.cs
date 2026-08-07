using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCAssistant.AvaloniaApp.Models;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.ViewModels;

/// <summary>
/// 主界面 ViewModel — 底部4标签页导航 + 设置面板叠加层。
/// 地址栏逻辑已移至独立的 AddressBarViewModel，本类不再管理地址栏状态。
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly IBrowserProvider _browser;
    private readonly ISettingsService _settings;

    /// <summary>默认标签页 URL（当用户未自定义时使用）。</summary>
    private static readonly string[] DefaultTabUrls = { "https://www.scbbs.top/", "https://www.sckey.net/", "https://scwz.top/", "" };

    /// <summary>底部 4 个标签页的中文名称。</summary>
    private static readonly string[] TabNames = { "SC中文社区", "SC联机号", "SC导航网", "工具" };

    /// <summary>当前选中的标签页索引（-1 表示未选中）。</summary>
    [ObservableProperty]
    private int _selectedTabIndex = -1;

    /// <summary>设置面板是否打开。</summary>
    [ObservableProperty]
    private bool _isSettingsOpen;

    /// <summary>底部标签页数据源。</summary>
    public ObservableCollection<TabItem> Tabs { get; } = new();

    /// <summary>
    /// 构造函数：注入浏览器服务和设置服务，初始化标签页。
    /// </summary>
    public MainViewModel(IBrowserProvider browser, ISettingsService settings)
    {
        _browser = browser;
        _settings = settings;

        _browser.DownloadRequested += (_, url) =>
        {
            LogHelper.Info($"[MainVM] 下载请求: {url}");
        };

        // 订阅设置保存事件
        SettingsViewModel.SettingsSaved += (_, _) => RefreshTabUrlsAsync();

        LogHelper.Info("[MainVM] 初始化 — 加载标签页配置...");
        InitializeTabs();
    }

    /// <summary>
    /// 从设置加载标签页 URL，若未配置则使用默认值。
    /// 加载完成后默认选中第一个标签页。
    /// 关键：默认标签页创建与选中在 await 之前同步执行，
    /// 确保 DataContext 绑定时 UrlText 已经就绪，避免地址栏短暂空白。
    /// </summary>
    private async void InitializeTabs()
    {
        // ── 同步阶段：立即创建默认标签页并导航，不等 I/O ──
        for (var i = 0; i < 4; i++)
        {
            var url = i < DefaultTabUrls.Length ? DefaultTabUrls[i] : string.Empty;
            Tabs.Add(new TabItem { Name = TabNames[i], Url = url });
        }

        // 默认选中第一个标签页 → 触发同步导航 → AddressChanged 立即通知地址栏
        SelectedTabIndex = 0;

        // ── 异步阶段：加载用户自定义设置并更新标签页 ──
        AppSettings appSettings;
        try
        {
            appSettings = await _settings.GetSettingsAsync();
        }
        catch (Exception ex)
        {
            LogHelper.Error("[MainVM] 加载设置失败，使用默认配置", ex);
            LogHelper.Info($"[MainVM] 标签页初始化完成，共 {Tabs.Count} 个标签 (默认)");
            return;
        }

        var urls = (appSettings.TabUrls != null && appSettings.TabUrls.Length >= 4)
            ? appSettings.TabUrls
            : DefaultTabUrls;

        for (var i = 0; i < Tabs.Count && i < urls.Length; i++)
        {
            if (Tabs[i].Url != urls[i])
            {
                LogHelper.Debug($"[MainVM] 标签[{i}] '{Tabs[i].Name}': {Tabs[i].Url} → {urls[i]}");
                Tabs[i].Url = urls[i];
            }
        }

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

    [RelayCommand]
    private void GoToTab0() => SelectedTabIndex = 0;

    [RelayCommand]
    private void GoToTab1() => SelectedTabIndex = 1;

    [RelayCommand]
    private void GoToTab2() => SelectedTabIndex = 2;

    [RelayCommand]
    private void GoToTab3() => SelectedTabIndex = 3;
}
