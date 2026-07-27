using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using Microsoft.Web.WebView2.Core;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.Desktop.Services;

/// <summary>
/// 基于 Microsoft Edge WebView2 的 Windows 桌面浏览器实现。
/// 通过 NativeControlHost 创建 Win32 子窗口承载 WebView2 控件。
/// </summary>
public sealed class WebView2BrowserProvider : NativeControlHost, IBrowserProvider, IDisposable
{
    private CoreWebView2Controller? _controller;
    private CoreWebView2? _coreWebView;
    private IntPtr _childHwnd;
    private string? _pendingNavigateUrl;
    private bool _isInitialized;
    private bool _isLoading;

    public event EventHandler<string>? AddressChanged;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<bool>? LoadingStateChanged;
    public event EventHandler? BrowserCrashed;

    public string CurrentUrl => _coreWebView?.Source ?? string.Empty;
    public string CurrentTitle => _coreWebView?.DocumentTitle ?? string.Empty;
    public bool IsLoading => _isLoading;

    // ── Win32 child window ──────────────────────────────────────────

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle, string lpClassName, string lpWindowName,
        uint dwStyle, int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    // ── NativeControlHost ───────────────────────────────────────────

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        const uint WS_CHILD = 0x40000000;
        const uint WS_VISIBLE = 0x10000000;
        const uint WS_CLIPCHILDREN = 0x02000000;
        const uint WS_CLIPSIBLINGS = 0x04000000;

        _childHwnd = CreateWindowEx(
            0, "STATIC", "",
            WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN | WS_CLIPSIBLINGS,
            0, 0, 1, 1,
            parent.Handle, IntPtr.Zero,
            GetModuleHandle(null),
            IntPtr.Zero);

        if (_childHwnd == IntPtr.Zero)
            throw new InvalidOperationException("WebView2: 无法创建宿主窗口");

        // WebView2 初始化是异步的，不阻塞 NativeControlHost 创建流程
        _ = InitializeWebView2Async();

        return new PlatformHandle(_childHwnd, "WebView2");
    }

    private async Task InitializeWebView2Async()
    {
        try
        {
            var options = new CoreWebView2EnvironmentOptions();
            var env = await CoreWebView2Environment.CreateAsync(null, null, options)
                .ConfigureAwait(false);

            var controller = await env.CreateCoreWebView2ControllerAsync(_childHwnd)
                .ConfigureAwait(false);

            _controller = controller;
            _coreWebView = controller.CoreWebView2;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // 页面导航事件
                _coreWebView.NavigationStarting += (_, e) =>
                {
                    _isLoading = true;
                    LoadingStateChanged?.Invoke(this, true);
                    AddressChanged?.Invoke(this, e.Uri);
                };

                _coreWebView.NavigationCompleted += (_, _) =>
                {
                    _isLoading = false;
                    LoadingStateChanged?.Invoke(this, false);
                    AddressChanged?.Invoke(this, _coreWebView.Source);
                };

                _coreWebView.DocumentTitleChanged += (_, _) =>
                    TitleChanged?.Invoke(this, _coreWebView.DocumentTitle);

                _coreWebView.ProcessFailed += (_, _) =>
                    BrowserCrashed?.Invoke(this, EventArgs.Empty);

                // WebView2 设置
                var settings = _coreWebView.Settings;
                settings.IsScriptEnabled = true;
                settings.IsWebMessageEnabled = true;
                settings.AreDefaultScriptDialogsEnabled = true;
                settings.IsStatusBarEnabled = false;
            });

            _isInitialized = true;

            // 导航待定 URL（如在初始化完成前调用了 Navigate）
            if (_pendingNavigateUrl != null)
            {
                NavigateCore(_pendingNavigateUrl);
                _pendingNavigateUrl = null;
            }

            // 首次布局
            ResizeWebViewToFit();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(
                $"WebView2 初始化失败: {ex.Message}");
            BrowserCrashed?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var result = base.ArrangeOverride(finalSize);
        ResizeWebViewToFit();
        return result;
    }

    private void ResizeWebViewToFit()
    {
        if (_controller == null)
            return;

        var w = Math.Max(1, (int)Bounds.Width);
        var h = Math.Max(1, (int)Bounds.Height);
        _controller.Bounds = new Rectangle(0, 0, w, h);
    }

    // ── IBrowserProvider ────────────────────────────────────────────

    public Control CreateBrowserControl() => this;

    public void Initialize(string startUrl) => Navigate(startUrl);

    public void Navigate(string url)
    {
        if (_isInitialized && _coreWebView != null)
            NavigateCore(url);
        else
            _pendingNavigateUrl = url;
    }

    public void Reload()
    {
        _coreWebView?.Reload();
    }

    private void NavigateCore(string url)
    {
        _coreWebView?.Navigate(url);
    }

    // ── 清理 ────────────────────────────────────────────────────────

    public void Dispose()
    {
        _coreWebView?.Dispose();
        _controller?.Dispose();
        _coreWebView = null;
        _controller = null;

        if (_childHwnd != IntPtr.Zero)
        {
            DestroyWindow(_childHwnd);
            _childHwnd = IntPtr.Zero;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Dispose();
    }
}
