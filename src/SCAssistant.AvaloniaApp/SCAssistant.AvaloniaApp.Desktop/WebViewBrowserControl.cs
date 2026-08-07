using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Microsoft.Web.WebView2;
using Microsoft.Web.WebView2.Core;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.Desktop;

/// <summary>
/// Windows 桌面端 WebView2 浏览器控件 — 通过 CoreWebView2 API 直接操作 WebView2 引擎。
/// 使用 Win32 Interop 将 WebView2 窗口嵌入 Avalonia 控件中。
/// </summary>
public class WebViewBrowserControl : Control, IBrowserProvider, IDisposable
{
    private CoreWebView2Controller? _controller;
    private CoreWebView2? _coreWebView;
    private CoreWebView2Environment? _environment;
    private bool _isInitialized;
    private bool _disposed;

    private string _currentUrl = string.Empty;
    private bool _isLoading;

    public bool IsReady => _isInitialized;

    public bool CanGoBack => _coreWebView?.CanGoBack ?? false;
    public bool CanGoForward => _coreWebView?.CanGoForward ?? false;
    public bool IsLoading => _isLoading;

    public event EventHandler? ReadyChanged;
    public event EventHandler<string>? AddressChanged;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<bool>? LoadingStateChanged;
    public event EventHandler<string>? DownloadRequested;
    public event EventHandler? NavigationHistoryChanged;

    #region Win32 API

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    #endregion

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (!_isInitialized)
        {
            _ = InitializeAsync();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Dispose();
    }

    private async Task InitializeAsync()
    {
        try
        {
            LogHelper.Info("[WebView2] 初始化 WebView2 环境...");

            _environment = await CoreWebView2Environment.CreateAsync();

            var parentHwnd = GetParentHandle();
            LogHelper.Info($"[WebView2] 父窗口句柄: {parentHwnd}");

            _controller = await _environment.CreateCoreWebView2ControllerAsync(parentHwnd);
            _coreWebView = _controller.CoreWebView2;

            // 将 WebView2 嵌入到父窗口
            _controller.ParentWindow = parentHwnd;
            UpdateBounds();

            // 订阅 WebView2 事件
            _coreWebView.NavigationStarting += OnNavigationStarting;
            _coreWebView.NavigationCompleted += OnNavigationCompleted;
            _coreWebView.SourceChanged += OnSourceChanged;
            _coreWebView.DocumentTitleChanged += OnDocumentTitleChanged;
            _coreWebView.DownloadStarting += OnDownloadStarting;
            _coreWebView.HistoryChanged += OnHistoryChanged;

            _coreWebView.Settings.AreDefaultContextMenusEnabled = true;
            _coreWebView.Settings.AreDevToolsEnabled = true;
            _coreWebView.Settings.IsZoomControlEnabled = true;

            // 监听尺寸变化
            SizeChanged += OnSizeChanged;

            _isInitialized = true;
            LogHelper.Info("[WebView2] WebView2 初始化完成");

            // 触发就绪事件 — 通知上层可以执行排队的导航
            ReadyChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            LogHelper.Error("[WebView2] WebView2 初始化失败", ex);
        }
    }

    private IntPtr GetParentHandle()
    {
        if (VisualRoot is TopLevel topLevel)
        {
            var handle = topLevel.TryGetPlatformHandle();
            if (handle != null)
            {
                return handle.Handle;
            }
        }
        return GetForegroundWindow();
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateBounds();
    }

    private void UpdateBounds()
    {
        if (_controller == null) return;

        var bounds = Bounds;
        if (bounds.Width > 0 && bounds.Height > 0)
        {
            _controller.Bounds = new System.Drawing.Rectangle(0, 0, (int)bounds.Width, (int)bounds.Height);
        }
    }

    #region WebView2 Event Handlers

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (!e.IsRedirected && e.NavigationId != 0)
        {
            _isLoading = true;
            LoadingStateChanged?.Invoke(this, true);
        }
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        _isLoading = false;
        LoadingStateChanged?.Invoke(this, false);

        if (e.IsSuccess)
        {
            LogHelper.Debug($"[WebView2] 导航完成: {_coreWebView?.Source}");
        }
        else
        {
            LogHelper.Warn($"[WebView2] 导航失败: HTTP {e.HttpStatusCode}");
        }
    }

    private void OnSourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        if (_coreWebView != null)
        {
            var url = _coreWebView.Source;
            _currentUrl = url;
            AddressChanged?.Invoke(this, url);
        }
    }

    private void OnDocumentTitleChanged(object? sender, object e)
    {
        if (_coreWebView != null)
        {
            TitleChanged?.Invoke(this, _coreWebView.DocumentTitle);
        }
    }

    private void OnDownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        var uri = e.DownloadOperation.Uri;
        DownloadRequested?.Invoke(this, uri);
        e.Cancel = true;
    }

    private void OnHistoryChanged(object? sender, object e)
    {
        NavigationHistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Public Methods (IBrowserProvider)

    public void Navigate(string url)
    {
        _currentUrl = url;

        if (!_isInitialized || _coreWebView == null)
        {
            LogHelper.Debug($"[WebView2] 跳过导航（未初始化）: {url}");
            return;
        }

        try
        {
            if (!url.StartsWith("http://") && !url.StartsWith("https://") && !url.StartsWith("file://"))
            {
                url = "https://" + url;
            }

            _coreWebView.Navigate(url);
            LogHelper.Debug($"[WebView2] 导航: {url}");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[WebView2] 导航失败: {url}", ex);
        }
    }

    public void Reload()
    {
        if (_coreWebView != null)
        {
            _coreWebView.Reload();
            LogHelper.Debug("[WebView2] 刷新");
        }
    }

    public void GoBack()
    {
        if (_coreWebView != null && _coreWebView.CanGoBack)
        {
            _coreWebView.GoBack();
            LogHelper.Debug("[WebView2] 后退");
        }
    }

    public void GoForward()
    {
        if (_coreWebView != null && _coreWebView.CanGoForward)
        {
            _coreWebView.GoForward();
            LogHelper.Debug("[WebView2] 前进");
        }
    }

    public string GetCurrentUrl() => _coreWebView?.Source ?? _currentUrl;

    public string GetTitle() => _coreWebView?.DocumentTitle ?? string.Empty;

    public async Task<string> ExecuteScriptAsync(string script)
    {
        if (_coreWebView != null)
        {
            return await _coreWebView.ExecuteScriptAsync(script);
        }
        return string.Empty;
    }

    public void Initialize()
    {
        if (!_isInitialized)
        {
            _ = InitializeAsync();
        }
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            if (_coreWebView != null)
            {
                _coreWebView.NavigationStarting -= OnNavigationStarting;
                _coreWebView.NavigationCompleted -= OnNavigationCompleted;
                _coreWebView.SourceChanged -= OnSourceChanged;
                _coreWebView.DocumentTitleChanged -= OnDocumentTitleChanged;
                _coreWebView.DownloadStarting -= OnDownloadStarting;
                _coreWebView.HistoryChanged -= OnHistoryChanged;
            }

            if (_controller != null)
            {
                _controller.Close();
                _controller = null;
            }

            _coreWebView = null;
            _environment = null;
        }
        catch (Exception ex)
        {
            LogHelper.Error("[WebView2] 释放资源时出错", ex);
        }
    }
}
