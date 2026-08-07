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
    /// <summary>WebView2 控制器（管理窗口嵌入和布局）。</summary>
    private CoreWebView2Controller? _controller;

    /// <summary>WebView2 核心引擎实例。</summary>
    private CoreWebView2? _coreWebView;

    /// <summary>WebView2 运行时环境。</summary>
    private CoreWebView2Environment? _environment;

    /// <summary>是否已完成 WebView2 初始化。</summary>
    private bool _isInitialized;

    /// <summary>是否已释放资源。</summary>
    private bool _disposed;

    /// <summary>当前导航 URL 缓存。</summary>
    private string _currentUrl = string.Empty;

    /// <summary>当前是否正在加载页面。</summary>
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

    #region Win32 API (WebView2 窗口嵌入)

    /// <summary>将子窗口句柄设置到父窗口，实现 WebView2 嵌入 Avalonia 窗口。</summary>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    /// <summary>移动/调整窗口位置和大小。</summary>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    /// <summary>检查窗口句柄是否有效。</summary>
    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    /// <summary>获取当前前台窗口句柄（回退方案）。</summary>
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    #endregion

    /// <summary>控件附加到可视化树时，启动 WebView2 异步初始化。</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (!_isInitialized)
        {
            _ = InitializeAsync();
        }

        // 订阅窗口位置/大小变化事件，确保 WebView2 位置随布局变化更新
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.SizeChanged += (_, _) => UpdateBounds();
            window.PositionChanged += (_, _) => UpdateBounds();
        }
    }

    /// <summary>控件从可视化树移除时释放 WebView2 资源。</summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Dispose();
    }

    /// <summary>异步初始化 WebView2 环境、控制器并订阅导航事件。</summary>
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

    /// <summary>获取 Avalonia 顶级窗口的原生窗口句柄，用于 WebView2 嵌入。</summary>
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
        // 回退：使用前台窗口句柄
        return GetForegroundWindow();
    }

    /// <summary>Avalonia 控件尺寸变化时同步更新 WebView2 布局。</summary>
    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateBounds();
    }

    /// <summary>计算控件相对于顶级窗口的偏移并更新 WebView2 边界。</summary>
    private void UpdateBounds()
    {
        if (_controller == null) return;

        var bounds = Bounds;
        if (bounds.Width > 0 && bounds.Height > 0)
        {
            // 通过遍历可视化树计算控件相对于顶级窗口的偏移位置
            // 这样 WebView2 会正确定位在地址栏下方，而不是覆盖地址栏
            var offset = CalculateOffsetFromTopLevel();

            _controller.Bounds = new System.Drawing.Rectangle(
                (int)offset.X,
                (int)offset.Y,
                (int)bounds.Width,
                (int)bounds.Height);

            LogHelper.Debug($"[WebView2] UpdateBounds: offset=({offset.X:F0},{offset.Y:F0}), size=({bounds.Width:F0}x{bounds.Height:F0})");
        }
    }

    /// <summary>
    /// 计算控件相对于顶级窗口的偏移位置（通过遍历可视化树）。
    /// </summary>
    private Point CalculateOffsetFromTopLevel()
    {
        double x = 0, y = 0;
        Control? current = this;

        while (current != null)
        {
            var b = current.Bounds;
            x += b.X;
            y += b.Y;

            // 检查是否到达顶级窗口
            if (current is Window) break;
            current = current.Parent as Control;
        }

        return new Point(x, y);
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

    /// <summary>导航到指定 URL（自动补全 https:// 协议）。</summary>
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

    /// <summary>释放 WebView2 资源：取消事件订阅、关闭控制器、清理引用。</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            // 取消所有事件订阅
            if (_coreWebView != null)
            {
                _coreWebView.NavigationStarting -= OnNavigationStarting;
                _coreWebView.NavigationCompleted -= OnNavigationCompleted;
                _coreWebView.SourceChanged -= OnSourceChanged;
                _coreWebView.DocumentTitleChanged -= OnDocumentTitleChanged;
                _coreWebView.DownloadStarting -= OnDownloadStarting;
                _coreWebView.HistoryChanged -= OnHistoryChanged;
            }

            // 关闭并释放 WebView2 控制器
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
