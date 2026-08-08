using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCAssistant.Maui.Services;

namespace SCAssistant.Maui.ViewModels;

/// <summary>
/// 地址栏 ViewModel — 管理 URL 编辑状态、导航历史、前进后退。
/// 独立于 MainViewModel，防止其他代码影响地址栏显示。
/// </summary>
public partial class AddressBarViewModel : ViewModelBase
{
    private readonly IBrowserProvider? _browser;

    [ObservableProperty]
    private string _urlText = string.Empty;

    [ObservableProperty]
    private bool _canGoBack;

    [ObservableProperty]
    private bool _canGoForward;

    private bool _isEditing;

    public bool IsInitialized { get; }

    public AddressBarViewModel()
    {
        IsInitialized = false;
    }

    public AddressBarViewModel(IBrowserProvider browser)
    {
        _browser = browser;
        IsInitialized = true;

        LogHelper.Info($"[AddrBarVM] 构造完成 — 浏览器就绪={browser.IsReady}");

        try
        {
            _browser.AddressChanged += OnAddressChanged;
            _browser.NavigationHistoryChanged += OnNavigationHistoryChanged;

            var currentUrl = _browser.GetCurrentUrl();
            if (!string.IsNullOrWhiteSpace(currentUrl))
                UrlText = currentUrl;

            CanGoBack = _browser.CanGoBack;
            CanGoForward = _browser.CanGoForward;

            LogHelper.Info($"[AddrBarVM] 事件订阅完成 — 后退={CanGoBack}, 前进={CanGoForward}");
        }
        catch (Exception ex)
        {
            LogHelper.Error("[AddrBarVM] 构造初始化失败", ex);
        }
    }

    private void OnAddressChanged(object? sender, string url)
    {
        if (_isEditing) return;
        if (string.IsNullOrWhiteSpace(url)) return;

        UrlText = url;
    }

    public void SyncFromBrowser()
    {
        if (_browser == null) return;

        var current = _browser.GetCurrentUrl();
        if (!string.IsNullOrWhiteSpace(current))
            UrlText = current;

        CanGoBack = _browser.CanGoBack;
        CanGoForward = _browser.CanGoForward;
    }

    private void OnNavigationHistoryChanged(object? sender, EventArgs e)
    {
        if (_browser != null)
        {
            CanGoBack = _browser.CanGoBack;
            CanGoForward = _browser.CanGoForward;
        }
    }

    [RelayCommand]
    private void Navigate()
    {
        if (_browser == null) return;

        var target = UrlText?.Trim();
        if (string.IsNullOrWhiteSpace(target)) return;

        if (!target.StartsWith("http://") && !target.StartsWith("https://") && !target.StartsWith("file://"))
        {
            target = "https://" + target;
            UrlText = target;
        }

        LogHelper.Info($"[AddrBarVM] 地址栏导航: {target}");
        _isEditing = false;
        _browser.Navigate(target);
    }

    [RelayCommand]
    private void GoBack()
    {
        _browser?.GoBack();
    }

    [RelayCommand]
    private void GoForward()
    {
        _browser?.GoForward();
    }

    public void SetEditing(bool editing)
    {
        _isEditing = editing;
        if (!editing && _browser != null)
        {
            var current = _browser.GetCurrentUrl();
            if (!string.IsNullOrWhiteSpace(current))
                UrlText = current;
        }
    }
}
