using System;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using SCAssistant.AvaloniaApp.Services;

#if WINDOWS
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2;
using Microsoft.Web.WebView2.Core;
#elif ANDROID
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Avalonia.Controls.Platform;
#elif IOS
using CoreGraphics;
using Foundation;
using UIKit;
using WebKit;
#endif

namespace SCAssistant.AvaloniaApp.Views;

/// <summary>
/// 跨平台 WebView 浏览器控件（单文件条件编译版）。
/// ┌────────────┬─────────────────────────────────────────┐
/// │ 平台       │ 底层引擎                                │
/// ├────────────┼─────────────────────────────────────────┤
/// │ Windows    │ Microsoft.Web.WebView2 (CoreWebView2)   │
/// │ Android    │ Android.Webkit.WebView                  │
/// │ iOS        │ WebKit.WKWebView                        │
/// │ Linux/macOS│ 反射调用 WebKitGTK / WKWebView          │
/// └────────────┴─────────────────────────────────────────┘
/// 所有平台均实现 IBrowserProvider 接口，通过统一事件桥接到 BrowserProvider。
/// </summary>
public class WebViewBrowserControl : Control, IBrowserProvider, IDisposable
{
    // ═══════════════════════════════════════════════════════════
    // 所有平台共用字段与接口属性 / 事件声明
    // ═══════════════════════════════════════════════════════════

    /// <summary>是否已完成平台 WebView 初始化。</summary>
    protected bool _isInitialized;

    /// <summary>是否已释放资源。</summary>
    protected bool _disposed;

    /// <summary>当前导航 URL 缓存（初始化前 Navigate 时保存）。</summary>
    protected string _currentUrl = string.Empty;

    /// <summary>是否正在加载页面（平台无法直接读取时的回退）。</summary>
    protected bool _isLoading;

    public bool IsReady => _isInitialized;

    public event EventHandler? ReadyChanged;
    public event EventHandler<string>? AddressChanged;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<bool>? LoadingStateChanged;
    public event EventHandler<string>? DownloadRequested;
    public event EventHandler? NavigationHistoryChanged;

    // 各平台独有字段（通过条件编译声明，避免运行时多余内存）
#if WINDOWS
    private CoreWebView2Controller? _controller;
    private CoreWebView2? _coreWebView;
    private CoreWebView2Environment? _environment;
#elif ANDROID
    private WebView? _webView;
    private Activity? _activity;
    private FrameLayout? _container;
    private bool _contentViewAdded;
    private FrameLayout.LayoutParams? _layoutParams;
#elif IOS
    private WKWebView? _webView;
    private UIViewController? _viewController;
    private IDisposable? _titleObserver;
    private IDisposable? _urlObserver;
    private IDisposable? _loadingObserver;
#else
    // Linux / macOS — 反射对象
    private object? _nativeWebView;
    private object? _nativeContainer;
    private Type? _nativeWebViewType;
    private Type? _nativeContainerType;
    private MethodInfo? _loadUrlMethod;
    private MethodInfo? _reloadMethod;
    private MethodInfo? _goBackMethod;
    private MethodInfo? _goForwardMethod;
    private MethodInfo? _stopLoadingMethod;
    private MethodInfo? _evaluateJavaScriptMethod;
    private PropertyInfo? _urlProperty;
    private PropertyInfo? _titleProperty;
    private PropertyInfo? _canGoBackProperty;
    private PropertyInfo? _canGoForwardProperty;
    private PropertyInfo? _isLoadingProperty;
#endif

    // ═══════════════════════════════════════════════════════════
    // IBrowserProvider — CanGoBack / CanGoForward / IsLoading
    // ═══════════════════════════════════════════════════════════

    public bool CanGoBack
    {
        get
        {
#if WINDOWS
            return _coreWebView?.CanGoBack ?? false;
#elif ANDROID
            return _webView?.CanGoBack() ?? false;
#elif IOS
            return _webView?.CanGoBack ?? false;
#else
            if (_nativeWebView == null || _canGoBackProperty == null) return false;
            try { return (bool)_canGoBackProperty.GetValue(_nativeWebView)!; } catch { return false; }
#endif
        }
    }

    public bool CanGoForward
    {
        get
        {
#if WINDOWS
            return _coreWebView?.CanGoForward ?? false;
#elif ANDROID
            return _webView?.CanGoForward() ?? false;
#elif IOS
            return _webView?.CanGoForward ?? false;
#else
            if (_nativeWebView == null || _canGoForwardProperty == null) return false;
            try { return (bool)_canGoForwardProperty.GetValue(_nativeWebView)!; } catch { return false; }
#endif
        }
    }

    public bool IsLoading
    {
        get
        {
#if WINDOWS
            return _isLoading;
#elif ANDROID
            return _webView != null && _webView.Progress < 100;
#elif IOS
            return _webView != null && _webView.IsLoading;
#else
            if (_nativeWebView == null || _isLoadingProperty == null) return _isLoading;
            try { return (bool)_isLoadingProperty.GetValue(_nativeWebView)!; } catch { return _isLoading; }
#endif
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Avalonia 控件生命周期（所有平台一致入口，内部分派到平台实现）
    // ═══════════════════════════════════════════════════════════

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_isInitialized || _disposed) return;
#if WINDOWS
        _ = InitializeWindowsAsync();
#elif ANDROID
        InitializeAndroid();
#elif IOS
        InitializeIos();
#else
        InitializeUnixWebView();
#endif
    }

    protected override Size MeasureOverride(Size availableSize) => availableSize;
    protected override Size ArrangeOverride(Size finalSize) => finalSize;

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Dispose();
    }

    // ═══════════════════════════════════════════════════════════
    // #region WINDOWS — WebView2 实现
    // ═══════════════════════════════════════════════════════════
#if WINDOWS

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

    private async Task InitializeWindowsAsync()
    {
        try
        {
            LogHelper.Info("[WebView2] 初始化 WebView2 环境...");
            _environment = await CoreWebView2Environment.CreateAsync();
            var parentHwnd = GetParentHandle();
            LogHelper.Info($"[WebView2] 父窗口句柄: {parentHwnd}");

            _controller = await _environment.CreateCoreWebView2ControllerAsync(parentHwnd);
            _coreWebView = _controller.CoreWebView2;
            _controller.ParentWindow = parentHwnd;
            UpdateWindowsBounds();

            _coreWebView.NavigationStarting += OnWinNavigationStarting;
            _coreWebView.NavigationCompleted += OnWinNavigationCompleted;
            _coreWebView.SourceChanged += OnWinSourceChanged;
            _coreWebView.DocumentTitleChanged += OnWinDocumentTitleChanged;
            _coreWebView.DownloadStarting += OnWinDownloadStarting;
            _coreWebView.HistoryChanged += OnWinHistoryChanged;

            _coreWebView.Settings.AreDefaultContextMenusEnabled = true;
            _coreWebView.Settings.AreDevToolsEnabled = true;
            _coreWebView.Settings.IsZoomControlEnabled = true;

            SizeChanged += (_, _) => UpdateWindowsBounds();
            if (TopLevel.GetTopLevel(this) is Window window)
            {
                window.SizeChanged += (_, _) => UpdateWindowsBounds();
                window.PositionChanged += (_, _) => UpdateWindowsBounds();
            }

            _isInitialized = true;
            LogHelper.Info("[WebView2] 初始化完成");
            ReadyChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            LogHelper.Error("[WebView2] 初始化失败", ex);
        }
    }

    private IntPtr GetParentHandle()
    {
        if (VisualRoot is TopLevel topLevel)
        {
            var handle = topLevel.TryGetPlatformHandle();
            if (handle != null) return handle.Handle;
        }
        return GetForegroundWindow();
    }

    private void UpdateWindowsBounds()
    {
        if (_controller == null) return;
        var bounds = Bounds;
        if (bounds.Width > 0 && bounds.Height > 0)
        {
            var offset = CalculateOffsetFromTopLevel();
            _controller.Bounds = new System.Drawing.Rectangle(
                (int)offset.X, (int)offset.Y, (int)bounds.Width, (int)bounds.Height);
            LogHelper.Debug($"[WebView2] UpdateBounds: offset=({offset.X:F0},{offset.Y:F0}), size=({bounds.Width:F0}x{bounds.Height:F0})");
        }
    }

    private void OnWinNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (!e.IsRedirected && e.NavigationId != 0)
        {
            _isLoading = true;
            LoadingStateChanged?.Invoke(this, true);
        }
    }
    private void OnWinNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        _isLoading = false;
        LoadingStateChanged?.Invoke(this, false);
        if (!e.IsSuccess) LogHelper.Warn($"[WebView2] 导航失败: HTTP {e.HttpStatusCode}");
    }
    private void OnWinSourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        if (_coreWebView != null)
        {
            var url = _coreWebView.Source;
            _currentUrl = url;
            AddressChanged?.Invoke(this, url);
        }
    }
    private void OnWinDocumentTitleChanged(object? sender, object e)
        => TitleChanged?.Invoke(this, _coreWebView?.DocumentTitle ?? string.Empty);
    private void OnWinDownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        DownloadRequested?.Invoke(this, e.DownloadOperation.Uri);
        e.Cancel = true;
    }
    private void OnWinHistoryChanged(object? sender, object e)
        => NavigationHistoryChanged?.Invoke(this, EventArgs.Empty);

#endif

    // ═══════════════════════════════════════════════════════════
    // #region ANDROID — WebView 实现
    // ═══════════════════════════════════════════════════════════
#if ANDROID

    private void InitializeAndroid()
    {
        try
        {
            _activity = GetAndroidActivity();
            if (_activity == null)
            {
                LogHelper.Error("[Android WebView] 无法获取 Activity");
                return;
            }
            _activity.RunOnUiThread(CreateAndroidWebViewOnUiThread);
        }
        catch (Exception ex)
        {
            LogHelper.Error("[Android WebView] 初始化失败", ex);
        }
    }

    private void CreateAndroidWebViewOnUiThread()
    {
        try
        {
            if (_activity == null || _disposed) return;
            _container = new FrameLayout(_activity);
            var density = _activity.Resources.DisplayMetrics?.Density ?? 1.5f;
            var offset = CalculateOffsetFromTopLevel();
            int widthPx = Bounds.Width > 0 ? (int)(Bounds.Width * density) : ViewGroup.LayoutParams.MatchParent;
            int heightPx = Bounds.Height > 0 ? (int)(Bounds.Height * density) : ViewGroup.LayoutParams.MatchParent;
            _layoutParams = new FrameLayout.LayoutParams(widthPx, heightPx)
            {
                LeftMargin = (int)(offset.X * density),
                TopMargin = (int)(offset.Y * density)
            };

            _webView = new WebView(_activity);
            var settings = _webView.Settings;
            settings.JavaScriptEnabled = true;
            settings.JavaScriptCanOpenWindowsAutomatically = true;
            settings.DomStorageEnabled = true;
            settings.DatabaseEnabled = true;
            settings.UseWideViewPort = true;
            settings.LoadWithOverviewMode = true;
            settings.SetSupportZoom(true);
            settings.LoadsImagesAutomatically = true;
            settings.BlockNetworkImage = false;
            settings.DefaultTextEncodingName = "utf-8";

            _webView.SetWebViewClient(new BrowserWebViewClient(this));
            _webView.SetWebChromeClient(new BrowserWebChromeClient(this));
            _container.AddView(_webView, new FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MatchParent, FrameLayout.LayoutParams.MatchParent));

            // 优先尝试 IAndroidViewSurface 反射嵌入（若 Avalonia 版本支持）；
            // 失败则回退到 AddContentView，并由 SizeChanged / LayoutUpdated 同步位置。
            if (!TrySetAndroidViewSurface(_container))
            {
                if (!_contentViewAdded && _activity != null)
                {
                    _activity.AddContentView(_container, _layoutParams);
                    _contentViewAdded = true;
                }
                SizeChanged += OnAndroidSizeChanged;
                LayoutUpdated += (_, _) => UpdateAndroidLayout();
            }

            _isInitialized = true;
            LogHelper.Info("[Android WebView] 初始化完成");
            ReadyChanged?.Invoke(this, EventArgs.Empty);

            if (!string.IsNullOrEmpty(_currentUrl))
                _webView.LoadUrl(_currentUrl);
        }
        catch (Exception ex)
        {
            LogHelper.Error("[Android WebView] UI 线程创建失败", ex);
        }
    }

    /// <summary>
    /// 尝试通过反射调用 IAndroidViewSurface.SetNativeView。
    /// 若当前 Avalonia 版本提供该接口则返回 true，否则返回 false 让调用方回退到 AddContentView。
    /// </summary>
    private bool TrySetAndroidViewSurface(global::Android.Views.View nativeView)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return false;
            var platformImplProp = typeof(TopLevel).GetProperty("PlatformImpl",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var platformImpl = platformImplProp?.GetValue(topLevel);
            if (platformImpl == null) return false;
            var iface = platformImpl.GetType().GetInterface("Avalonia.Android.IAndroidViewSurface");
            if (iface != null)
            {
                var setMethod = iface.GetMethod("SetNativeView", new[] { typeof(global::Android.Views.View) });
                if (setMethod != null)
                {
                    setMethod.Invoke(platformImpl, new object?[] { nativeView });
                    return true;
                }
            }
            // Control 级 PlatformImpl（若存在）
            var controlPlatformImpl = this.GetType().GetProperty("PlatformImpl",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(this);
            if (controlPlatformImpl != null)
            {
                var iface2 = controlPlatformImpl.GetType().GetInterface("Avalonia.Android.IAndroidViewSurface");
                if (iface2 != null)
                {
                    var setMethod2 = iface2.GetMethod("SetNativeView", new[] { typeof(global::Android.Views.View) });
                    if (setMethod2 != null)
                    {
                        setMethod2.Invoke(controlPlatformImpl, new object?[] { nativeView });
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.Debug($"[Android WebView] IAndroidViewSurface 反射跳过: {ex.Message}");
        }
        return false;
    }

    /// <summary>
    /// 回退模式（AddContentView）下同步容器尺寸与位置。
    /// IAndroidViewSurface 模式下此方法不会被调用。
    /// </summary>
    private void UpdateAndroidLayout()
    {
        if (_activity == null || _container == null || _layoutParams == null) return;
        try
        {
            _activity.RunOnUiThread(() =>
            {
                if (_container == null || _layoutParams == null || _activity == null) return;
                var density = _activity!.Resources.DisplayMetrics?.Density ?? 1.5f;
                var size = Bounds.Size;
                var offset = CalculateOffsetFromTopLevel();
                _layoutParams.Width = size.Width > 0 ? (int)(size.Width * density) : ViewGroup.LayoutParams.MatchParent;
                _layoutParams.Height = size.Height > 0 ? (int)(size.Height * density) : ViewGroup.LayoutParams.MatchParent;
                _layoutParams.LeftMargin = (int)(offset.X * density);
                _layoutParams.TopMargin = (int)(offset.Y * density);
                _container.LayoutParameters = _layoutParams;
            });
        }
        catch (Exception ex)
        {
            LogHelper.Error("[Android WebView] 布局更新失败", ex);
        }
    }

    private void OnAndroidSizeChanged(object? sender, SizeChangedEventArgs e) => UpdateAndroidLayout();

    private Activity? GetAndroidActivity()
    {
        // 优先由入口点注入静态 CurrentActivity
        var mainActivityType = Type.GetType("SCAssistant.AvaloniaApp.Platforms.Android.MainActivity, SCAssistant.AvaloniaApp");
        if (mainActivityType != null)
        {
            var prop = mainActivityType.GetProperty("CurrentActivity", BindingFlags.Public | BindingFlags.Static);
            if (prop?.GetValue(null) is Activity act) return act;
        }
        if (VisualRoot is TopLevel topLevel)
        {
            var handle = topLevel.TryGetPlatformHandle();
            if (handle != null)
            {
                var handleType = handle.GetType();
                var p = handleType.GetProperty("Activity");
                if (p?.GetValue(handle) is Activity act2) return act2;
            }
        }
        return null;
    }

    private class BrowserWebViewClient : WebViewClient
    {
        private readonly WebViewBrowserControl _c;
        public BrowserWebViewClient(WebViewBrowserControl c) { _c = c; }

        public override bool ShouldOverrideUrlLoading(WebView? view, IWebResourceRequest? request)
        {
            var url = request?.Url?.ToString();
            if (url == null) return false;
            if (url.StartsWith("app://") || url.StartsWith("scassistant://")) return true;
            return false;
        }
        public override void OnPageStarted(WebView? view, string? url, global::Android.Graphics.Bitmap? favicon)
        {
            base.OnPageStarted(view, url, favicon);
            _c._currentUrl = url ?? string.Empty;
            _c.AddressChanged?.Invoke(_c, url ?? string.Empty);
            _c.LoadingStateChanged?.Invoke(_c, true);
        }
        public override void OnPageFinished(WebView? view, string? url)
        {
            base.OnPageFinished(view, url);
            _c.LoadingStateChanged?.Invoke(_c, false);
            _c.NavigationHistoryChanged?.Invoke(_c, EventArgs.Empty);
        }
        public override void OnReceivedError(WebView? view, ClientError errorCode, string? description, string? failingUrl)
            => LogHelper.Warn($"[Android WebView] 错误: {errorCode} - {description}");
    }

    private class BrowserWebChromeClient : WebChromeClient
    {
        private readonly WebViewBrowserControl _c;
        public BrowserWebChromeClient(WebViewBrowserControl c) { _c = c; }
        public override void OnReceivedTitle(WebView? view, string? title)
            => _c.TitleChanged?.Invoke(_c, title ?? string.Empty);
        public override bool OnConsoleMessage(ConsoleMessage? cm)
        {
            if (cm != null) LogHelper.Debug($"[Android JS] {cm.Message()}");
            return base.OnConsoleMessage(cm);
        }
    }

    private class ValueCallbackCallback : Java.Lang.Object, IValueCallback
    {
        private readonly Action<Java.Lang.Object?> _callback;
        public ValueCallbackCallback(Action<Java.Lang.Object?> callback) { _callback = callback; }
        public void OnReceiveValue(Java.Lang.Object? value) => _callback(value);
    }

#endif

    // ═══════════════════════════════════════════════════════════
    // #region IOS — WKWebView 实现
    // ═══════════════════════════════════════════════════════════
#if IOS

    private void InitializeIos()
    {
        try
        {
            _viewController = GetIosViewController();
            if (_viewController == null)
            {
                LogHelper.Error("[iOS WebView] 无法获取 UIViewController");
                return;
            }
            CreateIosWebView();
        }
        catch (Exception ex)
        {
            LogHelper.Error("[iOS WebView] 初始化失败", ex);
        }
    }

    private void CreateIosWebView()
    {
        try
        {
            if (_viewController == null || _disposed) return;
            var config = new WKWebViewConfiguration();
            config.Preferences.JavaScriptCanOpenWindowsAutomatically = true;
            _webView = new WKWebView(CGRect.Empty, config);
            _webView.Configuration.Preferences.JavaScriptEnabled = true;

            _urlObserver = _webView.AddObserver("URL", NSKeyValueObservingOptions.New, change =>
            {
                if (_webView?.Url != null)
                {
                    var url = _webView.Url.AbsoluteString ?? string.Empty;
                    _currentUrl = url;
                    AddressChanged?.Invoke(this, url);
                }
            });
            _titleObserver = _webView.AddObserver("Title", NSKeyValueObservingOptions.New, change =>
                TitleChanged?.Invoke(this, _webView?.Title ?? string.Empty));
            _loadingObserver = _webView.AddObserver("IsLoading", NSKeyValueObservingOptions.New, change =>
            {
                if (_webView != null)
                {
                    LoadingStateChanged?.Invoke(this, _webView.IsLoading);
                    if (!_webView.IsLoading) NavigationHistoryChanged?.Invoke(this, EventArgs.Empty);
                }
            });

            _webView.NavigationDelegate = new WebViewNavigationDelegate(this);
            _viewController.View.AddSubview(_webView);
            UpdateIosFrame();
            // SizeChanged 仅在尺寸变化时触发；位置变化（SafeAreaMargin 应用、设置面板开关、
            // 标签切换等）不会触发，需额外监听 LayoutUpdated 同步原生 WKWebView 的 Frame，
            // 避免其停留在旧位置覆盖顶部地址栏或在底部留白。
            SizeChanged += (_, _) => UpdateIosFrame();
            LayoutUpdated += (_, _) => UpdateIosFrame();

            _isInitialized = true;
            LogHelper.Info("[iOS WebView] 初始化完成");
            ReadyChanged?.Invoke(this, EventArgs.Empty);

            if (!string.IsNullOrEmpty(_currentUrl)) Navigate(_currentUrl);
        }
        catch (Exception ex)
        {
            LogHelper.Error("[iOS WebView] 创建失败", ex);
        }
    }

    private void UpdateIosFrame()
    {
        if (_webView == null || _viewController == null) return;
        try
        {
            var size = Bounds.Size;
            if (size.Width <= 0 || size.Height <= 0) return;
            var offset = CalculateOffsetFromTopLevel();
            _webView.Frame = new CGRect((float)offset.X, (float)offset.Y, (float)size.Width, (float)size.Height);
            LogHelper.Debug($"[iOS WebView] UpdateFrame: offset=({offset.X:F0},{offset.Y:F0}), size=({size.Width:F0}x{size.Height:F0})");
        }
        catch (Exception ex)
        {
            LogHelper.Error("[iOS WebView] 更新布局失败", ex);
        }
    }

    private UIViewController? GetIosViewController()
    {
        // 优先从入口静态属性获取
        var appType = Type.GetType("SCAssistant.AvaloniaApp.Platforms.iOS.Application, SCAssistant.AvaloniaApp");
        if (appType != null)
        {
            var p = appType.GetProperty("CurrentViewController", BindingFlags.Public | BindingFlags.Static);
            if (p?.GetValue(null) is UIViewController vc) return vc;
        }
        if (VisualRoot is TopLevel topLevel)
        {
            var handle = topLevel.TryGetPlatformHandle();
            if (handle != null)
            {
                var t = handle.GetType();
                var vp = t.GetProperty("ViewController");
                if (vp?.GetValue(handle) is UIViewController vc2) return vc2;
                var viewP = t.GetProperty("View");
                if (viewP?.GetValue(handle) is UIView view) return view.Window?.RootViewController;
            }
        }
        return GetTopIosViewController();
    }

    private static UIViewController? GetTopIosViewController()
    {
        try
        {
            var window = UIApplication.SharedApplication.KeyWindow;
            if (window?.RootViewController != null) return FindTopVC(window.RootViewController);
        } catch { }
        return null;
    }
    private static UIViewController FindTopVC(UIViewController vc)
    {
        if (vc.PresentedViewController != null) return FindTopVC(vc.PresentedViewController);
        if (vc is UINavigationController nav && nav.VisibleViewController != null) return FindTopVC(nav.VisibleViewController);
        if (vc is UITabBarController tab && tab.SelectedViewController != null) return FindTopVC(tab.SelectedViewController);
        return vc;
    }

    private class WebViewNavigationDelegate : WKNavigationDelegate
    {
        private readonly WebViewBrowserControl _c;
        public WebViewNavigationDelegate(WebViewBrowserControl c) { _c = c; }
        public override void DidStartProvisionalNavigation(WKWebView webView, WKNavigation navigation)
            => _c.LoadingStateChanged?.Invoke(_c, true);
        public override void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
        {
            _c.LoadingStateChanged?.Invoke(_c, false);
            _c.NavigationHistoryChanged?.Invoke(_c, EventArgs.Empty);
        }
        public override void DidFailProvisionalNavigation(WKWebView webView, WKNavigation navigation, NSError error)
        {
            _c.LoadingStateChanged?.Invoke(_c, false);
            LogHelper.Warn($"[iOS WebView] 导航失败: {error?.Description}");
        }
    }

#endif

    // ═══════════════════════════════════════════════════════════
    // #region LINUX / MACOS — 反射 WebKit 实现（兜底）
    // ═══════════════════════════════════════════════════════════
#if !(WINDOWS || ANDROID || IOS)

    private void InitializeUnixWebView()
    {
        try
        {
            if (OperatingSystem.IsLinux()) InitializeLinux();
            else if (OperatingSystem.IsMacOS()) InitializeMacOS();
            else LogHelper.Error("[WebKit] 不支持的操作系统");
        }
        catch (Exception ex) { LogHelper.Error("[WebKit] 初始化失败", ex); }
    }

    private void InitializeLinux()
    {
        var gtkAsm = Assembly.Load("WebKitSharp") ?? Assembly.Load("WebKit") ?? Assembly.Load("Gtk.WebKit");
        _nativeWebViewType = gtkAsm?.GetType("WebKit.WebView")
                            ?? gtkAsm?.GetType("WebKitSharp.WebView")
                            ?? gtkAsm?.GetType("Gtk.WebKit.WebView");
        if (_nativeWebViewType == null)
        {
            LogHelper.Warn("[WebKit] Linux 下未找到 WebKitGTK，使用系统浏览器兜底");
            return;
        }
        CreateNativeWebViewCommon();
    }

    private void InitializeMacOS()
    {
        Assembly? asm = null;
        foreach (var n in new[] { "Xamarin.Mac", "Microsoft.macOS" })
        {
            try { asm = Assembly.Load(n); if (asm != null) break; } catch { }
        }
        if (asm == null) { try { asm = Assembly.Load("WebKit"); } catch { } }
        _nativeWebViewType = asm?.GetType("WebKit.WKWebView") ?? asm?.GetType("WKWebView");
        if (_nativeWebViewType == null)
        {
            LogHelper.Warn("[WebKit] macOS 下未找到 WKWebView");
            return;
        }
        CreateNativeWebViewCommon();
    }

    private void CreateNativeWebViewCommon()
    {
        try
        {
            if (_nativeWebViewType == null || _disposed) return;
            ConstructorInfo? ctor = _nativeWebViewType.GetConstructor(Type.EmptyTypes);
            if (ctor == null)
            {
                foreach (var c in _nativeWebViewType.GetConstructors())
                {
                    if (c.GetParameters().Length <= 2) { ctor = c; break; }
                }
            }
            if (ctor == null) { LogHelper.Error("[WebKit] 找不到 WebView 构造函数"); return; }
            var ps = ctor.GetParameters();
            object?[]? args = null;
            if (ps.Length == 0) _nativeWebView = ctor.Invoke(null);
            else
            {
                args = new object?[ps.Length];
                for (int i = 0; i < ps.Length; i++)
                    args[i] = ps[i].ParameterType.GetConstructor(Type.EmptyTypes)?.Invoke(null);
                _nativeWebView = ctor.Invoke(args);
            }
            if (_nativeWebView == null) { LogHelper.Error("[WebKit] 创建 WebView 失败"); return; }

            BindMembers();
            SetupUnixListeners();
            EmbedNative();
            SizeChanged += (_, _) => { /* 由父容器自动管理 */ };

            _isInitialized = true;
            LogHelper.Info("[WebKit] 初始化完成");
            ReadyChanged?.Invoke(this, EventArgs.Empty);
            if (!string.IsNullOrEmpty(_currentUrl)) Navigate(_currentUrl);
        }
        catch (Exception ex) { LogHelper.Error("[WebKit] 创建失败", ex); }
    }

    private void BindMembers()
    {
        if (_nativeWebViewType == null) return;
        _loadUrlMethod = _nativeWebViewType.GetMethod("LoadUrl", new[] { typeof(string) })
                        ?? _nativeWebViewType.GetMethod("LoadRequest")
                        ?? _nativeWebViewType.GetMethod("Navigate", new[] { typeof(string) });
        _reloadMethod = _nativeWebViewType.GetMethod("Reload") ?? _nativeWebViewType.GetMethod("Refresh");
        _goBackMethod = _nativeWebViewType.GetMethod("GoBack");
        _goForwardMethod = _nativeWebViewType.GetMethod("GoForward");
        _stopLoadingMethod = _nativeWebViewType.GetMethod("StopLoading");
        _evaluateJavaScriptMethod = _nativeWebViewType.GetMethod("EvaluateJavaScript", new[] { typeof(string) })
            ?? _nativeWebViewType.GetMethod("EvaluateJavaScript", new[] { typeof(string), typeof(object) });
        _urlProperty = _nativeWebViewType.GetProperty("Url") ?? _nativeWebViewType.GetProperty("URL");
        _titleProperty = _nativeWebViewType.GetProperty("Title");
        _canGoBackProperty = _nativeWebViewType.GetProperty("CanGoBack");
        _canGoForwardProperty = _nativeWebViewType.GetProperty("CanGoForward");
        _isLoadingProperty = _nativeWebViewType.GetProperty("IsLoading") ?? _nativeWebViewType.GetProperty("Loading");
    }

    private void SetupUnixListeners()
    {
        if (_nativeWebView == null || _nativeWebViewType == null) return;
        try
        {
            var ls = _nativeWebViewType.GetEvent("LoadStarted") ?? _nativeWebViewType.GetEvent("DidStartProvisionalNavigation");
            ls?.AddEventHandler(_nativeWebView, new EventHandler((_, _) =>
            {
                _isLoading = true; LoadingStateChanged?.Invoke(this, true);
            }));
            var lf = _nativeWebViewType.GetEvent("LoadFinished") ?? _nativeWebViewType.GetEvent("DidFinishNavigation");
            lf?.AddEventHandler(_nativeWebView, new EventHandler((_, _) =>
            {
                _isLoading = false;
                LoadingStateChanged?.Invoke(this, false);
                NavigationHistoryChanged?.Invoke(this, EventArgs.Empty);
                UpdateUnixUrlAndTitle();
            }));
        }
        catch (Exception ex) { LogHelper.Warn($"[WebKit] 事件绑定部分失败: {ex.Message}"); }
    }

    private void EmbedNative()
    {
        if (_nativeWebView == null) return;
        try
        {
            if (VisualRoot is TopLevel tl)
            {
                var h = tl.TryGetPlatformHandle();
                if (h != null)
                {
                    var t = h.GetType();
                    var vp = t.GetProperty("NativeView") ?? t.GetProperty("View");
                    if (vp?.GetValue(h) is { } v)
                    {
                        _nativeContainer = v;
                        var add = v.GetType().GetMethod("Add") ?? v.GetType().GetMethod("AddChild") ?? v.GetType().GetMethod("PackStart");
                        add?.Invoke(v, new[] { _nativeWebView });
                    }
                }
            }
            if (_nativeContainer == null) LogHelper.Warn("[WebKit] 未嵌入原生 WebView，将作为独立窗口");
        }
        catch (Exception ex) { LogHelper.Error("[WebKit] 嵌入失败", ex); }
    }

    private void UpdateUnixUrlAndTitle()
    {
        try
        {
            if (_urlProperty != null && _nativeWebView != null)
            {
                var url = _urlProperty.GetValue(_nativeWebView)?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(url) && url != _currentUrl)
                {
                    _currentUrl = url;
                    AddressChanged?.Invoke(this, url);
                }
            }
            if (_titleProperty != null && _nativeWebView != null)
            {
                var t = _titleProperty.GetValue(_nativeWebView)?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(t)) TitleChanged?.Invoke(this, t);
            }
        } catch { }
    }

#endif

    // ═══════════════════════════════════════════════════════════
    // 共用辅助：计算控件相对顶级窗口/视图的偏移
    // ═══════════════════════════════════════════════════════════
    protected Avalonia.Point CalculateOffsetFromTopLevel()
    {
        if (VisualRoot is not TopLevel topLevel)
            return new Avalonia.Point(0, 0);
        var transform = this.TransformToVisual(topLevel);
        if (transform.HasValue)
            return transform.Value.Transform(new Avalonia.Point(0, 0));
        return new Avalonia.Point(0, 0);
    }

    // ═══════════════════════════════════════════════════════════
    // IBrowserProvider — 公共导航方法（所有平台分派）
    // ═══════════════════════════════════════════════════════════

    public void Navigate(string url)
    {
        _currentUrl = url;
        if (!_isInitialized) { LogHelper.Debug($"[WebView] 跳过导航(未初始化): {url}"); return; }
        try
        {
            if (!url.StartsWith("http://") && !url.StartsWith("https://") && !url.StartsWith("file://"))
                url = "https://" + url;

#if WINDOWS
            _coreWebView?.Navigate(url);
#elif ANDROID
            _activity?.RunOnUiThread(() => _webView?.LoadUrl(url));
#elif IOS
            var nsUrl = NSUrl.FromString(url);
            if (nsUrl != null) _webView?.LoadRequest(new NSUrlRequest(nsUrl));
#else
            _loadUrlMethod?.Invoke(_nativeWebView, new object[] { url });
#endif
            LogHelper.Debug($"[WebView] 导航: {url}");
        }
        catch (Exception ex) { LogHelper.Error($"[WebView] 导航失败: {url}", ex); }
    }

    public void Reload()
    {
        try
        {
#if WINDOWS
            _coreWebView?.Reload();
#elif ANDROID
            _activity?.RunOnUiThread(() => _webView?.Reload());
#elif IOS
            _webView?.Reload();
#else
            _reloadMethod?.Invoke(_nativeWebView, null);
#endif
            LogHelper.Debug("[WebView] 刷新");
        }
        catch (Exception ex) { LogHelper.Error("[WebView] 刷新失败", ex); }
    }

    public void GoBack()
    {
        try
        {
#if WINDOWS
            if (_coreWebView?.CanGoBack == true) _coreWebView.GoBack();
#elif ANDROID
            _activity?.RunOnUiThread(() => { if (_webView?.CanGoBack() == true) _webView.GoBack(); });
#elif IOS
            if (_webView?.CanGoBack == true) _webView.GoBack();
#else
            if (_nativeWebView != null && _goBackMethod != null && CanGoBack) _goBackMethod.Invoke(_nativeWebView, null);
#endif
            LogHelper.Debug("[WebView] 后退");
        }
        catch (Exception ex) { LogHelper.Error("[WebView] 后退失败", ex); }
    }

    public void GoForward()
    {
        try
        {
#if WINDOWS
            if (_coreWebView?.CanGoForward == true) _coreWebView.GoForward();
#elif ANDROID
            _activity?.RunOnUiThread(() => { if (_webView?.CanGoForward() == true) _webView.GoForward(); });
#elif IOS
            if (_webView?.CanGoForward == true) _webView.GoForward();
#else
            if (_nativeWebView != null && _goForwardMethod != null && CanGoForward) _goForwardMethod.Invoke(_nativeWebView, null);
#endif
            LogHelper.Debug("[WebView] 前进");
        }
        catch (Exception ex) { LogHelper.Error("[WebView] 前进失败", ex); }
    }

    public string GetCurrentUrl()
    {
#if WINDOWS
        return _coreWebView?.Source ?? _currentUrl;
#elif ANDROID
        return _webView?.Url ?? _currentUrl;
#elif IOS
        return _webView?.Url?.AbsoluteString ?? _currentUrl;
#else
        if (_urlProperty != null && _nativeWebView != null)
        { try { return _urlProperty.GetValue(_nativeWebView)?.ToString() ?? _currentUrl; } catch { } }
        return _currentUrl;
#endif
    }

    public string GetTitle()
    {
#if WINDOWS
        return _coreWebView?.DocumentTitle ?? string.Empty;
#elif ANDROID
        return _webView?.Title ?? string.Empty;
#elif IOS
        return _webView?.Title ?? string.Empty;
#else
        if (_titleProperty != null && _nativeWebView != null)
        { try { return _titleProperty.GetValue(_nativeWebView)?.ToString() ?? string.Empty; } catch { } }
        return string.Empty;
#endif
    }

    public async Task<string> ExecuteScriptAsync(string script)
    {
#if WINDOWS
        if (_coreWebView != null) return await _coreWebView.ExecuteScriptAsync(script);
        return string.Empty;
#elif ANDROID
        if (_webView == null) return string.Empty;
        var tcs = new TaskCompletionSource<string>();
        _activity!.RunOnUiThread(() =>
        {
            _webView.EvaluateJavascript(script, new ValueCallbackCallback(o =>
            {
                tcs.TrySetResult(o?.ToString() ?? string.Empty);
            }));
        });
        return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(8000));
#elif IOS
        if (_webView == null) return string.Empty;
        try
        {
            var tcs = new TaskCompletionSource<string>();
            _webView.EvaluateJavaScript(script, (r, err) =>
            {
                if (err != null) tcs.TrySetException(new Exception(err.ToString()));
                else tcs.TrySetResult(r?.ToString() ?? string.Empty);
            });
            return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch { return string.Empty; }
#else
        if (_nativeWebView == null || _evaluateJavaScriptMethod == null) return string.Empty;
        try
        {
            var ps = _evaluateJavaScriptMethod.GetParameters();
            if (ps.Length == 2)
            {
                // macOS 异步版本
                var tcs = new TaskCompletionSource<string>();
                var actionType = ps[1].ParameterType;
                object? handler = null;
                if (actionType.IsGenericType && actionType.GetGenericArguments().Length == 2)
                {
                    var argTypes = actionType.GetGenericArguments();
                    Action<object?, object?> h = (r, e) =>
                    {
                        if (e != null) tcs.TrySetException(new Exception("JS failed"));
                        else tcs.TrySetResult(r?.ToString() ?? string.Empty);
                    };
                    var invoke = actionType.GetMethod("Invoke");
                    if (invoke != null)
                    {
                        var p1 = System.Linq.Expressions.Expression.Parameter(argTypes[0], "r");
                        var p2 = System.Linq.Expressions.Expression.Parameter(argTypes[1], "e");
                        var call = System.Linq.Expressions.Expression.Call(
                            System.Linq.Expressions.Expression.Constant(h),
                            typeof(Action<object?, object?>).GetMethod("Invoke")!,
                            System.Linq.Expressions.Expression.Convert(p1, typeof(object)),
                            System.Linq.Expressions.Expression.Convert(p2, typeof(object)));
                        handler = System.Linq.Expressions.Expression.Lambda(actionType, call, p1, p2).Compile();
                    }
                }
                _evaluateJavaScriptMethod.Invoke(_nativeWebView, new object?[] { script, handler });
                return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
            }
            else
            {
                var r = _evaluateJavaScriptMethod.Invoke(_nativeWebView, new object?[] { script });
                return r?.ToString() ?? string.Empty;
            }
        }
        catch { return string.Empty; }
#endif
    }

    public void Initialize()
    {
        if (!_isInitialized)
        {
#if WINDOWS
            _ = InitializeWindowsAsync();
#elif ANDROID
            InitializeAndroid();
#elif IOS
            InitializeIos();
#else
            InitializeUnixWebView();
#endif
        }
    }

    // ═══════════════════════════════════════════════════════════
    // IDisposable
    // ═══════════════════════════════════════════════════════════

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
#if WINDOWS
            if (_coreWebView != null)
            {
                _coreWebView.NavigationStarting -= OnWinNavigationStarting;
                _coreWebView.NavigationCompleted -= OnWinNavigationCompleted;
                _coreWebView.SourceChanged -= OnWinSourceChanged;
                _coreWebView.DocumentTitleChanged -= OnWinDocumentTitleChanged;
                _coreWebView.DownloadStarting -= OnWinDownloadStarting;
                _coreWebView.HistoryChanged -= OnWinHistoryChanged;
            }
            if (_controller != null) { _controller.Close(); _controller = null; }
            _coreWebView = null; _environment = null;
#elif ANDROID
            // 反射清理 IAndroidViewSurface（若使用了该模式嵌入）
            TrySetAndroidViewSurface(null!);
            if (_webView != null)
            {
                _webView.StopLoading(); _webView.ClearHistory(); _webView.ClearCache(true);
                void Clean()
                {
                    try { _webView!.Visibility = ViewStates.Gone; _webView.RemoveAllViews(); _webView.Destroy(); } catch { }
                }
                if (_activity != null) _activity.RunOnUiThread(Clean); else Clean();
                _webView = null;
            }
            if (_container != null && _activity != null)
            {
                _activity.RunOnUiThread(() =>
                {
                    try { _container!.Visibility = ViewStates.Gone; _container.RemoveAllViews(); _container = null; } catch { }
                });
            }
            _contentViewAdded = false;
            _layoutParams = null;
#elif IOS
            _titleObserver?.Dispose(); _urlObserver?.Dispose(); _loadingObserver?.Dispose();
            _titleObserver = null; _urlObserver = null; _loadingObserver = null;
            if (_webView != null)
            {
                _webView.StopLoading(); _webView.NavigationDelegate = null;
                _webView.RemoveFromSuperview(); _webView.Dispose(); _webView = null;
            }
#else
            _stopLoadingMethod?.Invoke(_nativeWebView, null);
            _nativeWebView = null; _nativeContainer = null;
            _nativeWebViewType = null; _nativeContainerType = null;
#endif
        }
        catch (Exception ex) { LogHelper.Error("[WebView] 释放资源时出错", ex); }
    }
}
