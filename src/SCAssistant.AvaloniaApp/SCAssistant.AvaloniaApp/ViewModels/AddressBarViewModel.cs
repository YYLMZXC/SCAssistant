using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.ViewModels;

/// <summary>
/// 地址栏独立 ViewModel — 与 MainViewModel 完全解耦，防止其他代码影响地址栏显示。
/// 自己管理 URL 文本编辑状态，仅在用户未编辑时同步浏览器 URL 变更。
/// </summary>
public partial class AddressBarViewModel : ViewModelBase
{
    private readonly IBrowserProvider? _browser;

    /// <summary>地址栏显示的 URL 文本（TwoWay 绑定 TextBox）。</summary>
    [ObservableProperty]
    private string _urlText = string.Empty;

    /// <summary>是否可以后退。</summary>
    [ObservableProperty]
    private bool _canGoBack;

    /// <summary>是否可以前进。</summary>
    [ObservableProperty]
    private bool _canGoForward;

    /// <summary>用户是否正在编辑地址栏（打字时锁定，不接收浏览器 URL 更新）。</summary>
    private bool _isEditing;

    /// <summary>ViewModel 是否已正确初始化（拥有有效 IBrowserProvider）。</summary>
    public bool IsInitialized { get; }

    /// <summary>
    /// 设计时无参构造函数（XAML 编译绑定 / Design.DataContext 需要）。
    /// 此构造创建的实例功能受限，运行时 DI 会使用带参构造。
    /// </summary>
    public AddressBarViewModel()
    {
        IsInitialized = false;
        LogHelper.Debug("[AddrBarVM] 无参构造（设计时模式），功能受限");
    }

    /// <summary>
    /// 运行时构造函数（由 DI 注入）。
    /// </summary>
    public AddressBarViewModel(IBrowserProvider browser)
    {
        _browser = browser;
        IsInitialized = true;

        LogHelper.Info($"[AddrBarVM] 构造完成 — 浏览器就绪={browser.IsReady}, 当前URL={browser.GetCurrentUrl()}");

        try
        {
            // 订阅浏览器事件同步地址栏状态
            _browser.AddressChanged += OnAddressChanged;
            _browser.NavigationHistoryChanged += OnNavigationHistoryChanged;

            // 初始化时同步一次当前状态
            var currentUrl = _browser.GetCurrentUrl();
            if (!string.IsNullOrWhiteSpace(currentUrl))
            {
                UrlText = currentUrl;
                LogHelper.Debug($"[AddrBarVM] 初始化同步当前 URL: {currentUrl}");
            }
            CanGoBack = _browser.CanGoBack;
            CanGoForward = _browser.CanGoForward;

            LogHelper.Info($"[AddrBarVM] 事件订阅完成 — 后退={CanGoBack}, 前进={CanGoForward}");
        }
        catch (Exception ex)
        {
            LogHelper.Error("[AddrBarVM] 构造初始化失败", ex);
        }
    }

    /// <summary>浏览器 URL 变更 — 仅在用户未编辑时更新地址栏。</summary>
    private void OnAddressChanged(object? sender, string url)
    {
        if (!_isEditing)
        {
            UrlText = url;
            LogHelper.Debug($"[AddrBarVM] 同步浏览器 URL: {url}");
        }
        else
        {
            LogHelper.Debug($"[AddrBarVM] 正在编辑，跳过 URL 同步: {url} (当前输入={UrlText})");
        }
    }

    /// <summary>浏览器导航历史变更 — 同步前进/后退按钮状态。</summary>
    private void OnNavigationHistoryChanged(object? sender, EventArgs e)
    {
        if (_browser != null)
        {
            CanGoBack = _browser.CanGoBack;
            CanGoForward = _browser.CanGoForward;
        }
    }

    /// <summary>地址栏回车或点击 Go 按钮 — 导航到目标 URL。</summary>
    [RelayCommand]
    private void Navigate()
    {
        if (_browser == null)
        {
            LogHelper.Warn("[AddrBarVM] 导航失败: 浏览器未初始化（设计时模式）");
            return;
        }

        var target = UrlText?.Trim();
        if (string.IsNullOrWhiteSpace(target))
        {
            LogHelper.Warn("[AddrBarVM] 导航取消: URL 为空");
            return;
        }

        // 自动补全协议
        if (!target.StartsWith("http://") && !target.StartsWith("https://") && !target.StartsWith("file://"))
        {
            target = "https://" + target;
            UrlText = target;
        }

        LogHelper.Info($"[AddrBarVM] 地址栏导航: {target}");
        _isEditing = false;
        _browser.Navigate(target);
    }

    /// <summary>后退。</summary>
    [RelayCommand]
    private void GoBack()
    {
        LogHelper.Debug("[AddrBarVM] 后退");
        _browser?.GoBack();
    }

    /// <summary>前进。</summary>
    [RelayCommand]
    private void GoForward()
    {
        LogHelper.Debug("[AddrBarVM] 前进");
        _browser?.GoForward();
    }

    /// <summary>
    /// 设置编辑状态 — 由 AddressBarView 的 GotFocus/LostFocus 事件调用。
    /// 编辑中时锁定 UrlText，不接收浏览器 URL 同步。
    /// 编辑结束时从浏览器同步最新 URL。
    /// </summary>
    public void SetEditing(bool editing)
    {
        _isEditing = editing;
        LogHelper.Debug($"[AddrBarVM] 编辑状态切换: {(editing ? "编辑中" : "浏览中")}");
        if (!editing && _browser != null)
        {
            // 失去焦点时同步浏览器当前 URL
            var current = _browser.GetCurrentUrl();
            if (!string.IsNullOrWhiteSpace(current))
            {
                UrlText = current;
                LogHelper.Debug($"[AddrBarVM] 失焦同步 URL: {current}");
            }
        }
    }
}
