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
    private readonly IBrowserProvider _browser;

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

    public AddressBarViewModel(IBrowserProvider browser)
    {
        _browser = browser;

        // 订阅浏览器事件同步地址栏状态
        _browser.AddressChanged += OnAddressChanged;
        _browser.NavigationHistoryChanged += OnNavigationHistoryChanged;
    }

    /// <summary>浏览器 URL 变更 — 仅在用户未编辑时更新地址栏。</summary>
    private void OnAddressChanged(object? sender, string url)
    {
        if (!_isEditing)
        {
            UrlText = url;
            LogHelper.Debug($"[AddrBarVM] 同步浏览器 URL: {url}");
        }
    }

    /// <summary>浏览器导航历史变更 — 同步前进/后退按钮状态。</summary>
    private void OnNavigationHistoryChanged(object? sender, EventArgs e)
    {
        CanGoBack = _browser.CanGoBack;
        CanGoForward = _browser.CanGoForward;
    }

    /// <summary>地址栏回车或点击 Go 按钮 — 导航到目标 URL。</summary>
    [RelayCommand]
    private void Navigate()
    {
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
        _browser.GoBack();
    }

    /// <summary>前进。</summary>
    [RelayCommand]
    private void GoForward()
    {
        LogHelper.Debug("[AddrBarVM] 前进");
        _browser.GoForward();
    }

    /// <summary>
    /// 设置编辑状态 — 由 AddressBarView 的 GotFocus/LostFocus 事件调用。
    /// 编辑中时锁定 UrlText，不接收浏览器 URL 同步。
    /// 编辑结束时从浏览器同步最新 URL。
    /// </summary>
    public void SetEditing(bool editing)
    {
        _isEditing = editing;
        if (!editing)
        {
            // 失去焦点时同步浏览器当前 URL
            var current = _browser.GetCurrentUrl();
            if (!string.IsNullOrWhiteSpace(current))
            {
                UrlText = current;
            }
        }
    }
}
