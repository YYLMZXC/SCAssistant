using System;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using UIKit;
using WebKit;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.iOS;

/// <summary>
/// iOS 平台原生 WebView 浏览器控件 — 基于 WebKit.WKWebView。
/// </summary>
public class WebViewBrowserControl : Control, IBrowserProvider, IDisposable
{
    /// <summary>iOS 原生 WKWebView 实例。</summary>
    private WKWebView? _webView;

    /// <summary>当前宿主 ViewController。</summary>
    private UIViewController? _viewController;

    /// <summary>是否已完成初始化。</summary>
    private bool _isInitialized;

    /// <summary>是否已释放资源。</summary>
    private bool _disposed;

    /// <summary>当前导航 URL 缓存。</summary>
    private string _currentUrl = string.Empty;

    /// <summary>KVO 监听器：页面标题变化。</summary>
    private IDisposable? _titleObserver;

    /// <summary>KVO 监听器：URL 地址变化。</summary>
    private IDisposable? _urlObserver;

    /// <summary>KVO 监听器：加载状态变化。</summary>
    private IDisposable? _loadingObserver;

    public bool IsReady => _isInitialized;

    public bool CanGoBack => _webView?.CanGoBack ?? false;
    public bool CanGoForward => _webView?.CanGoForward ?? false;
    public bool IsLoading => _webView != null && _webView.IsLoading;

    public event EventHandler? ReadyChanged;
    public event EventHandler<string>? AddressChanged;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<bool>? LoadingStateChanged;
    public event EventHandler<string>? DownloadRequested;
    public event EventHandler? NavigationHistoryChanged;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (!_isInitialized)
        {
            InitializeWebView();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Dispose();
    }

    private void InitializeWebView()
    {
        try
        {
            _viewController = GetViewController();
            if (_viewController == null)
            {
                LogHelper.Error("[iOS WebView] 无法获取 UIViewController");
                return;
            }

            CreateWebView();
        }
        catch (Exception ex)
        {
            LogHelper.Error("[iOS WebView] 初始化失败", ex);
        }
    }

    private void CreateWebView()
    {
        try
        {
            if (_viewController == null || _disposed) return;

            var config = new WKWebViewConfiguration();
            config.Preferences.JavaScriptCanOpenWindowsAutomatically = true;

            // 以空 Frame 创建，实际位置/尺寸由 UpdateFrame 依据控件在 Avalonia 可视树中的
            // 实际矩形计算后设置，避免 WKWebView 占满整个 VC 视图而遮住顶部地址栏与底部标签栏。
            // 不使用 AutoresizingMask——Frame 完全由 UpdateFrame 手动管理，防止旋转时先被
            // 自动撑满全屏再修正造成的闪烁。
            _webView = new WKWebView(CGRect.Empty, config);

            _webView.Configuration.Preferences.JavaScriptEnabled = true;

            // KVO 监听 URL 变化
            _urlObserver = _webView.AddObserver("URL", NSKeyValueObservingOptions.New, change =>
            {
                if (_webView?.Url != null)
                {
                    var url = _webView.Url.AbsoluteString ?? string.Empty;
                    _currentUrl = url;
                    AddressChanged?.Invoke(this, url);
                }
            });

            // KVO 监听标题变化
            _titleObserver = _webView.AddObserver("Title", NSKeyValueObservingOptions.New, change =>
            {
                if (_webView?.Title != null)
                {
                    TitleChanged?.Invoke(this, _webView.Title);
                }
            });

            // KVO 监听加载状态
            _loadingObserver = _webView.AddObserver("IsLoading", NSKeyValueObservingOptions.New, change =>
            {
                if (_webView != null)
                {
                    LoadingStateChanged?.Invoke(this, _webView.IsLoading);
                    if (!_webView.IsLoading)
                    {
                        NavigationHistoryChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            });

            // 设置导航委托
            _webView.NavigationDelegate = new WebViewNavigationDelegate(this);

            // 添加到视图层级
            _viewController.View.AddSubview(_webView);

            UpdateFrame();

            SizeChanged += OnAvaloniaSizeChanged;

            _isInitialized = true;

            LogHelper.Info("[iOS WebView] 初始化完成");
            ReadyChanged?.Invoke(this, EventArgs.Empty);

            if (!string.IsNullOrEmpty(_currentUrl))
            {
                Navigate(_currentUrl);
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error("[iOS WebView] 创建 WebView 失败", ex);
        }
    }

    private void OnAvaloniaSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateFrame();
    }

    /// <summary>
    /// 依据控件在 Avalonia 可视树中的实际矩形，计算其相对于顶级视图的偏移并设置 WKWebView.Frame。
    /// 这样 WKWebView 只占据 BrowserView 区域（地址栏下方、标签栏上方），不再遮住顶/底导航。
    /// 与桌面端 WebView2 的 UpdateBounds/CalculateOffsetFromTopLevel 保持一致逻辑。
    /// </summary>
    private void UpdateFrame()
    {
        if (_webView == null || _viewController == null) return;

        try
        {
            var size = Bounds.Size;
            if (size.Width <= 0 || size.Height <= 0) return;

            // 遍历可视树累加 Bounds.X/Y，得到控件相对于顶级视图的偏移（DIP == iOS point）
            var offset = CalculateOffsetFromTopLevel();

            _webView.Frame = new CGRect(
                (float)offset.X,
                (float)offset.Y,
                (float)size.Width,
                (float)size.Height);

            LogHelper.Debug($"[iOS WebView] UpdateFrame: offset=({offset.X:F0},{offset.Y:F0}), size=({size.Width:F0}x{size.Height:F0})");
        }
        catch (Exception ex)
        {
            LogHelper.Error("[iOS WebView] 更新布局失败", ex);
        }
    }

    /// <summary>
    /// 计算控件相对于顶级视图（TopLevel，对应 root VC View）的偏移位置。
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

            if (current is TopLevel) break;
            current = current.Parent as Control;
        }

        return new Point(x, y);
    }

    private UIViewController? GetViewController()
    {
        if (Application.CurrentViewController != null)
        {
            return Application.CurrentViewController;
        }

        if (VisualRoot is TopLevel topLevel)
        {
            var handle = topLevel.TryGetPlatformHandle();
            if (handle != null)
            {
                return GetViewControllerFromHandle(handle);
            }
        }

        return Application.GetTopViewController();
    }

    private static UIViewController? GetViewControllerFromHandle(IPlatformHandle handle)
    {
        var handleType = handle.GetType();
        var vcProperty = handleType.GetProperty("ViewController");
        if (vcProperty != null)
        {
            return vcProperty.GetValue(handle) as UIViewController;
        }

        var viewProperty = handleType.GetProperty("View");
        if (viewProperty != null)
        {
            var view = viewProperty.GetValue(handle) as UIView;
            return view?.Window?.RootViewController;
        }

        return null;
    }

    #region IBrowserProvider Implementation

    public void Navigate(string url)
    {
        _currentUrl = url;
        if (!_isInitialized || _webView == null) return;

        if (!url.StartsWith("http://") && !url.StartsWith("https://") && !url.StartsWith("file://"))
        {
            url = "https://" + url;
        }

        try
        {
            var nsUrl = NSUrl.FromString(url);
            if (nsUrl != null)
            {
                _webView.LoadRequest(new NSUrlRequest(nsUrl));
                LogHelper.Debug($"[iOS WebView] 导航: {url}");
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[iOS WebView] 导航失败: {url}", ex);
        }
    }

    public void Reload()
    {
        if (_webView == null) return;
        try
        {
            _webView.Reload();
            LogHelper.Debug("[iOS WebView] 刷新");
        }
        catch (Exception ex)
        {
            LogHelper.Error("[iOS WebView] 刷新失败", ex);
        }
    }

    public void GoBack()
    {
        if (_webView == null || !_webView.CanGoBack) return;
        try
        {
            _webView.GoBack();
            LogHelper.Debug("[iOS WebView] 后退");
        }
        catch (Exception ex)
        {
            LogHelper.Error("[iOS WebView] 后退失败", ex);
        }
    }

    public void GoForward()
    {
        if (_webView == null || !_webView.CanGoForward) return;
        try
        {
            _webView.GoForward();
            LogHelper.Debug("[iOS WebView] 前进");
        }
        catch (Exception ex)
        {
            LogHelper.Error("[iOS WebView] 前进失败", ex);
        }
    }

    public string GetCurrentUrl() => _webView?.Url?.AbsoluteString ?? _currentUrl;

    public string GetTitle() => _webView?.Title ?? string.Empty;

    public async Task<string> ExecuteScriptAsync(string script)
    {
        if (_webView == null) return string.Empty;

        try
        {
            var tcs = new TaskCompletionSource<string>();
            _webView.EvaluateJavaScript(script, (result, error) =>
            {
                if (error != null)
                {
                    tcs.TrySetException(new Exception(error.ToString()));
                }
                else
                {
                    tcs.TrySetResult(result?.ToString() ?? string.Empty);
                }
            });
            return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch
        {
            return string.Empty;
        }
    }

    public void Initialize()
    {
        if (!_isInitialized && _viewController != null)
        {
            InitializeWebView();
        }
    }

    #endregion

    #region Navigation Delegate（处理 WKWebView 导航事件）

    /// <summary>
    /// 自定义 WKNavigationDelegate：处理导航开始、完成和失败事件，
    /// 并通知外层 LoadingStateChanged 和 NavigationHistoryChanged 事件。
    /// </summary>
    private class WebViewNavigationDelegate : WKNavigationDelegate
    {
        private readonly WebViewBrowserControl _control;

        public WebViewNavigationDelegate(WebViewBrowserControl control)
        {
            _control = control;
        }

        public override void DidStartProvisionalNavigation(WKWebView webView, WKNavigation navigation)
        {
            _control.LoadingStateChanged?.Invoke(_control, true);
        }

        public override void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
        {
            _control.LoadingStateChanged?.Invoke(_control, false);
            _control.NavigationHistoryChanged?.Invoke(_control, EventArgs.Empty);
        }

        public override void DidFailProvisionalNavigation(WKWebView webView, WKNavigation navigation, NSError error)
        {
            _control.LoadingStateChanged?.Invoke(_control, false);
            LogHelper.Warn($"[iOS WebView] 导航失败: {error?.Description}");
        }
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            if (_titleObserver != null)
            {
                _titleObserver.Dispose();
                _titleObserver = null;
            }
            if (_urlObserver != null)
            {
                _urlObserver.Dispose();
                _urlObserver = null;
            }
            if (_loadingObserver != null)
            {
                _loadingObserver.Dispose();
                _loadingObserver = null;
            }

            if (_webView != null)
            {
                _webView.StopLoading();
                _webView.NavigationDelegate = null;
                _webView.RemoveFromSuperview();
                _webView.Dispose();
                _webView = null;
            }

            SizeChanged -= OnAvaloniaSizeChanged;
        }
        catch (Exception ex)
        {
            LogHelper.Error("[iOS WebView] 释放资源时出错", ex);
        }
    }
}