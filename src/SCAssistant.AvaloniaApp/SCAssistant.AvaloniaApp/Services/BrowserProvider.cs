using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 浏览器提供者 — 提供跨平台 WebView 抽象。
/// 支持导航队列：在平台 WebView 就绪前的 Navigate 请求会被缓存，就绪后自动执行。
/// </summary>
public class BrowserProvider : IBrowserProvider
{
    private IBrowserProvider? _platformWebView;
    private readonly Queue<string> _pendingNavigations = new();
    private string _currentUrl = string.Empty;
    private string _currentTitle = "SC 助手";
    private bool _isLoading;
    private bool _canGoBack;
    private bool _canGoForward;
    private bool _isReady;

    public bool IsReady
    {
        get => _isReady;
        private set
        {
            if (_isReady == value) return;
            _isReady = value;
            ReadyChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool CanGoBack => _platformWebView?.CanGoBack ?? _canGoBack;
    public bool CanGoForward => _platformWebView?.CanGoForward ?? _canGoForward;
    public bool IsLoading => _platformWebView?.IsLoading ?? _isLoading;

    public event EventHandler? ReadyChanged;
    public event EventHandler<string>? AddressChanged;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<bool>? LoadingStateChanged;
    public event EventHandler<string>? DownloadRequested;
    public event EventHandler? NavigationHistoryChanged;

    /// <summary>
    /// 设置平台 WebView 实现（由 BrowserView 初始化时调用）。
    /// 仅注册平台，不立即标记就绪 — 等待平台 WebView 初始化完成后再 MarkPlatformReady。
    /// </summary>
    public void SetPlatformWebView(IBrowserProvider provider)
    {
        if (_platformWebView != null)
        {
            DetachPlatformEvents();
        }

        _platformWebView = provider;
        AttachPlatformEvents();

        // 如果平台已经就绪（例如同步实现），直接标记
        if (provider.IsReady)
        {
            IsReady = true;
            FlushPendingNavigations();
        }
        else
        {
            LogHelper.Info("[BrowserProvider] 平台 WebView 已设置，等待初始化完成...");
        }
    }

    /// <summary>
    /// 平台 WebView 初始化完成后调用此方法，触发排队导航的执行。
    /// </summary>
    public void MarkPlatformReady()
    {
        if (IsReady) return;

        LogHelper.Info("[BrowserProvider] 平台 WebView 已就绪");
        IsReady = true;
        FlushPendingNavigations();
    }

    public void Initialize()
    {
        LogHelper.Info("[BrowserProvider] 初始化");
    }

    public void Navigate(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        LogHelper.Info($"[BrowserProvider] 导航请求: {url} (就绪={IsReady})");

        _currentUrl = url;
        AddressChanged?.Invoke(this, url);

        if (_platformWebView != null && IsReady)
        {
            _platformWebView.Navigate(url);
        }
        else
        {
            // 平台 WebView 未就绪 — 缓存导航请求，就绪后执行
            lock (_pendingNavigations)
            {
                // 用新的 URL 替换旧的，只保留最后一次请求（避免队列堆积过期请求）
                _pendingNavigations.Clear();
                _pendingNavigations.Enqueue(url);
            }
            LogHelper.Info($"[BrowserProvider] 导航已排队，等待 WebView 就绪: {url}");

            // UI 层面显示加载状态
            SetLoading(true);
        }
    }

    public void Reload()
    {
        LogHelper.Info("[BrowserProvider] 刷新");
        if (_platformWebView != null && IsReady)
        {
            _platformWebView.Reload();
        }
        else if (!string.IsNullOrEmpty(_currentUrl))
        {
            // 未就绪时刷新就是重新导航
            Navigate(_currentUrl);
        }
    }

    public void GoBack()
    {
        LogHelper.Debug("[BrowserProvider] 后退");
        if (_platformWebView != null && IsReady)
        {
            _platformWebView.GoBack();
        }
    }

    public void GoForward()
    {
        LogHelper.Debug("[BrowserProvider] 前进");
        if (_platformWebView != null && IsReady)
        {
            _platformWebView.GoForward();
        }
    }

    public string GetCurrentUrl() => _platformWebView?.GetCurrentUrl() ?? _currentUrl;

    public string GetTitle() => _platformWebView?.GetTitle() ?? _currentTitle;

    public Task<string> ExecuteScriptAsync(string script)
    {
        if (_platformWebView != null && IsReady)
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

    /// <summary>
    /// 执行排队的导航请求。
    /// </summary>
    private void FlushPendingNavigations()
    {
        List<string> pending;
        lock (_pendingNavigations)
        {
            pending = new List<string>(_pendingNavigations);
            _pendingNavigations.Clear();
        }

        if (pending.Count == 0)
        {
            LogHelper.Info("[BrowserProvider] 无排队导航请求");
            return;
        }

        LogHelper.Info($"[BrowserProvider] 执行 {pending.Count} 个排队导航请求");
        SetLoading(true);

        foreach (var url in pending)
        {
            LogHelper.Info($"[BrowserProvider] 执行排队导航: {url}");
            _platformWebView?.Navigate(url);
        }
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
