using System;
using System.Threading.Tasks;

namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 浏览器提供者 — 提供跨平台 WebView 抽象。
/// 桌面端由 WebViewBrowserControl 实现底层渲染，移动端由原生 WebView 实现。
/// </summary>
public class BrowserProvider : IBrowserProvider
{
    private IBrowserProvider? _platformWebView;
    private string _currentUrl = string.Empty;
    private string _currentTitle = "SC 助手";
    private bool _isLoading;
    private bool _canGoBack;
    private bool _canGoForward;

    public bool CanGoBack => _platformWebView?.CanGoBack ?? _canGoBack;
    public bool CanGoForward => _platformWebView?.CanGoForward ?? _canGoForward;
    public bool IsLoading => _platformWebView?.IsLoading ?? _isLoading;

    public event EventHandler<string>? AddressChanged;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<bool>? LoadingStateChanged;
    public event EventHandler<string>? DownloadRequested;
    public event EventHandler? NavigationHistoryChanged;

    /// <summary>
    /// 设置平台 WebView 实现（由 BrowserView 初始化时调用）。
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
            _currentTitle = url;
            TitleChanged?.Invoke(this, url);
            SetLoading(false);
            NavigationHistoryChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Reload()
    {
        LogHelper.Info("[BrowserProvider] 刷新");
        if (_platformWebView != null)
        {
            _platformWebView.Reload();
        }
        else if (!string.IsNullOrEmpty(_currentUrl))
        {
            Navigate(_currentUrl);
        }
    }

    public void GoBack()
    {
        LogHelper.Debug("[BrowserProvider] 后退");
        if (_platformWebView != null)
        {
            _platformWebView.GoBack();
        }
    }

    public void GoForward()
    {
        LogHelper.Debug("[BrowserProvider] 前进");
        if (_platformWebView != null)
        {
            _platformWebView.GoForward();
        }
    }

    public string GetCurrentUrl() => _platformWebView?.GetCurrentUrl() ?? _currentUrl;

    public string GetTitle() => _platformWebView?.GetTitle() ?? _currentTitle;

    public Task<string> ExecuteScriptAsync(string script)
    {
        if (_platformWebView != null)
            return _platformWebView.ExecuteScriptAsync(script);
        return Task.FromResult(string.Empty);
    }

    #region Platform Event Handlers (called by platform-specific code)

    /// <summary>
    /// 处理平台地址变更事件。
    /// </summary>
    public void HandlePlatformAddressChanged(string url)
    {
        _currentUrl = url;
        _canGoBack = _platformWebView?.CanGoBack ?? false;
        _canGoForward = _platformWebView?.CanGoForward ?? false;
        AddressChanged?.Invoke(this, url);
        NavigationHistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 处理平台标题变更事件。
    /// </summary>
    public void HandlePlatformTitleChanged(string title)
    {
        _currentTitle = title;
        TitleChanged?.Invoke(this, title);
    }

    /// <summary>
    /// 处理平台加载状态变更事件。
    /// </summary>
    public void HandlePlatformLoadingStateChanged(bool loading)
    {
        _isLoading = loading;
        LoadingStateChanged?.Invoke(this, loading);
    }

    /// <summary>
    /// 处理平台下载请求事件。
    /// </summary>
    public void HandlePlatformDownloadRequested(string url)
    {
        DownloadRequested?.Invoke(this, url);
    }

    /// <summary>
    /// 处理平台导航历史变更事件。
    /// </summary>
    public void HandlePlatformNavigationHistoryChanged()
    {
        _canGoBack = _platformWebView?.CanGoBack ?? false;
        _canGoForward = _platformWebView?.CanGoForward ?? false;
        NavigationHistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

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

    private void OnPlatformAddressChanged(object? sender, string url)
    {
        LogHelper.Debug($"[BrowserProvider] 地址变更: {url}");
        AddressChanged?.Invoke(this, url);
    }

    private void OnPlatformTitleChanged(object? sender, string title)
    {
        LogHelper.Debug($"[BrowserProvider] 标题变更: {title}");
        TitleChanged?.Invoke(this, title);
    }

    private void OnPlatformLoadingStateChanged(object? sender, bool loading)
    {
        LogHelper.Debug($"[BrowserProvider] 加载状态: {(loading ? "加载中" : "完成")}");
        LoadingStateChanged?.Invoke(this, loading);
    }

    private void OnPlatformDownloadRequested(object? sender, string url)
    {
        LogHelper.Info($"[BrowserProvider] 下载请求: {url}");
        DownloadRequested?.Invoke(this, url);
    }

    private void OnPlatformNavigationHistoryChanged(object? sender, EventArgs e)
    {
        NavigationHistoryChanged?.Invoke(this, e);
    }
}