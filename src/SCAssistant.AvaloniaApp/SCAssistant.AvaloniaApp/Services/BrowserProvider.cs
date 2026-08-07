using System;
using System.Threading.Tasks;

namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 浏览器提供者 — 提供跨平台 WebView 抽象。
/// </summary>
public class BrowserProvider : IBrowserProvider
{
    // WebView 代理 — 由平台层注入
    private IBrowserProvider? _platformWebView;

    private string _currentUrl = string.Empty;
    private string _currentTitle = "SC 助手";
    private bool _isLoading;

    public bool CanGoBack => _platformWebView?.CanGoBack ?? false;
    public bool CanGoForward => _platformWebView?.CanGoForward ?? false;
    public bool IsLoading => _platformWebView?.IsLoading ?? _isLoading;

    public event EventHandler<string>? AddressChanged;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<bool>? LoadingStateChanged;
    public event EventHandler<string>? DownloadRequested;
    public event EventHandler? NavigationHistoryChanged;

    /// <summary>
    /// 设置平台 WebView 实现（仅桌面端需要）。
    /// </summary>
    public void SetPlatformWebView(IBrowserProvider provider)
    {
        if (_platformWebView != null)
        {
            DetachPlatformEvents();
        }

        _platformWebView = provider;
        AttachPlatformEvents();

        LogHelper.Info("[BrowserProvider] 平台 WebView 已设置");
    }

    public void Initialize()
    {
        LogHelper.Info("[BrowserProvider] 初始化");
    }

    public void Navigate(string url)
    {
        LogHelper.Info($"[BrowserProvider] 导航: {url}");
        _currentUrl = url;

        if (_platformWebView != null)
        {
            _platformWebView.Navigate(url);
        }
        else
        {
            // 无平台 WebView 时模拟导航
            SetLoading(true);
            AddressChanged?.Invoke(this, url);
            TitleChanged?.Invoke(this, url);
            _currentTitle = url;

            // 模拟加载完成
            SetLoading(false);
            NavigationHistoryChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Reload()
    {
        LogHelper.Info("[BrowserProvider] 刷新");
        _platformWebView?.Reload();
        if (_platformWebView == null && !string.IsNullOrEmpty(_currentUrl))
        {
            Navigate(_currentUrl);
        }
    }

    public void GoBack()
    {
        LogHelper.Info("[BrowserProvider] 后退");
        _platformWebView?.GoBack();
    }

    public void GoForward()
    {
        LogHelper.Info("[BrowserProvider] 前进");
        _platformWebView?.GoForward();
    }

    public string GetCurrentUrl() => _platformWebView?.GetCurrentUrl() ?? _currentUrl;

    public string GetTitle() => _platformWebView?.GetTitle() ?? _currentTitle;

    public Task<string> ExecuteScriptAsync(string script)
    {
        if (_platformWebView != null)
            return _platformWebView.ExecuteScriptAsync(script);
        return Task.FromResult(string.Empty);
    }

    private void SetLoading(bool isLoading)
    {
        _isLoading = isLoading;
        LoadingStateChanged?.Invoke(this, isLoading);
    }

    private void AttachPlatformEvents()
    {
        if (_platformWebView == null) return;

        _platformWebView.AddressChanged += OnPlatformAddressChanged;
        _platformWebView.TitleChanged += OnPlatformTitleChanged;
        _platformWebView.LoadingStateChanged += OnPlatformLoadingStateChanged;
        _platformWebView.DownloadRequested += OnPlatformDownloadRequested;
        _platformWebView.NavigationHistoryChanged += OnPlatformNavigationHistoryChanged;
    }

    private void DetachPlatformEvents()
    {
        if (_platformWebView == null) return;

        _platformWebView.AddressChanged -= OnPlatformAddressChanged;
        _platformWebView.TitleChanged -= OnPlatformTitleChanged;
        _platformWebView.LoadingStateChanged -= OnPlatformLoadingStateChanged;
        _platformWebView.DownloadRequested -= OnPlatformDownloadRequested;
        _platformWebView.NavigationHistoryChanged -= OnPlatformNavigationHistoryChanged;
    }

    private void OnPlatformAddressChanged(object? sender, string url) => AddressChanged?.Invoke(this, url);
    private void OnPlatformTitleChanged(object? sender, string title) => TitleChanged?.Invoke(this, title);
    private void OnPlatformLoadingStateChanged(object? sender, bool loading) => LoadingStateChanged?.Invoke(this, loading);
    private void OnPlatformDownloadRequested(object? sender, string url) => DownloadRequested?.Invoke(this, url);
    private void OnPlatformNavigationHistoryChanged(object? sender, EventArgs e) => NavigationHistoryChanged?.Invoke(this, e);
}
