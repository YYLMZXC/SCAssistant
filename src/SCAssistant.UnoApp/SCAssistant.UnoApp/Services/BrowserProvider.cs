using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SCAssistant.UnoApp.Services;

/// <summary>
/// 基于 Uno Platform 跨平台 WebView2 的浏览器实现。
/// Uno 将 WebView2 映射为各平台原生浏览器：
/// - Windows: Edge WebView2
/// - Android: Android WebView
/// - iOS: WKWebView
/// - Desktop Skia: Uno 模拟实现
///
/// 正确生命周期：
/// 1. 创建 WebView2 控件对象，加入可视化树
/// 2. 调用 EnsureCoreWebView2Async() 等待内核初始化完成
/// 3. 内核就绪后才允许导航（Source = / CoreWebView2.Navigate）
///
/// 注意：Loaded 事件仅代表 UI 控件入树，不代表 CoreWebView2 内核就绪。
/// </summary>
public class BrowserProvider : IBrowserProvider
{
    private WebView2? _webView;
    private string _currentUrl = string.Empty;
    private string _currentTitle = string.Empty;
    private bool _isLoading;
    private string? _pendingNavigateUrl;
    private bool _isReady;

    public event EventHandler<string>? AddressChanged;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<bool>? LoadingStateChanged;

    public string CurrentUrl => _currentUrl;
    public string CurrentTitle => _currentTitle;
    public bool IsLoading => _isLoading;

    public object CreateBrowserControl()
    {
        LogHelper.Info("[Browser] CreateBrowserControl - creating WebView2");
        _webView = new WebView2();
        _isReady = false;

        LogHelper.Info($"[Browser] WebView2 type: {_webView.GetType().FullName}, assembly: {_webView.GetType().Assembly.GetName().FullName}");

        // Loaded 仅用于触发内核初始化，不直接执行业务导航
        _webView.Loaded += OnWebViewLoaded;

        // ⚠️ 关键诊断：CoreWebView2Initialized 事件
        // 如果这个事件从未触发 → WebView2 Runtime 缺失或 WPF 程序集加载失败
        _webView.CoreWebView2Initialized += (sender, e) =>
        {
            if (e.Exception is not null)
            {
                LogHelper.Error($"[Browser] CoreWebView2Initialized FAILED: {e.Exception.GetType().Name}: {e.Exception.Message}", e.Exception);
                return;
            }
            LogHelper.Info("[Browser] CoreWebView2Initialized OK - runtime is fully functional");
            LogHelper.Info($"[Browser] CoreWebView2 type: {sender.CoreWebView2?.GetType().FullName ?? "null"}");
        };

        _webView.NavigationStarting += (_, args) =>
        {
            _isLoading = true;
            _currentUrl = args.Uri?.ToString() ?? string.Empty;
            LogHelper.Info($"[Browser] NavigationStarting -> {_currentUrl}");
            AddressChanged?.Invoke(this, _currentUrl);
            LoadingStateChanged?.Invoke(this, true);
        };

        _webView.NavigationCompleted += (sender, args) =>
        {
            _isLoading = false;
            LogHelper.Info($"[Browser] NavigationCompleted success={args.IsSuccess} err={args.WebErrorStatus}");

            try
            {
                if (sender.CoreWebView2 is not null)
                {
                    _currentTitle = sender.CoreWebView2.DocumentTitle ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error("[Browser] Failed to read document title", ex);
            }
            TitleChanged?.Invoke(this, _currentTitle);
            LoadingStateChanged?.Invoke(this, false);
        };

        // 诊断：CoreWebView2 属性访问
        try
        {
            var cv = _webView.CoreWebView2;
            LogHelper.Info($"[Browser] CoreWebView2 prop (pre-init): {(cv is null ? "null" : cv.GetType().FullName)}");
        }
        catch (Exception ex)
        {
            LogHelper.Info($"[Browser] CoreWebView2 prop (pre-init) threw: {ex.GetType().Name}: {ex.Message}");
        }

        LogHelper.Info($"[Browser] CreateBrowserControl done, _isReady={_isReady}");
        return _webView;
    }

    /// <summary>
    /// Loaded 表示 WebView2 UI 控件已挂入可视化树。
    /// 此时启动 CoreWebView2 内核初始化，但内核未就绪，不可导航。
    /// </summary>
    private void OnWebViewLoaded(object sender, RoutedEventArgs e)
    {
        if (_webView is null) return;
        _webView.Loaded -= OnWebViewLoaded;

        LogHelper.Info("[Browser] WebView2.Loaded - control in visual tree, starting kernel init");
        _ = InitializeCoreWebView2Async();
    }

    /// <summary>
    /// 异步初始化 CoreWebView2 内核。
    /// 控件必须在可视化树中才能调用 EnsureCoreWebView2Async。
    /// 内核初始化成功后，_isReady = true，执行挂起的导航请求。
    /// </summary>
    private async Task InitializeCoreWebView2Async()
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            LogHelper.Info("[Browser] Calling EnsureCoreWebView2Async...");
            await _webView!.EnsureCoreWebView2Async();
            sw.Stop();
            LogHelper.Info($"[Browser] EnsureCoreWebView2Async completed in {sw.ElapsedMilliseconds}ms");

            // 诊断：初始化后检查 CoreWebView2 状态
            try
            {
                var cv = _webView.CoreWebView2;
                LogHelper.Info($"[Browser] CoreWebView2 after init: {(cv is null ? "null" : $"type={cv.GetType().FullName}, can navigate")}");
                if (cv is not null)
                {
                    LogHelper.Info($"[Browser] CoreWebView2.Settings={(cv.Settings is null ? "null" : "ok")}");
                }
            }
            catch (Exception ex2)
            {
                LogHelper.Error($"[Browser] Failed to inspect CoreWebView2 after init: {ex2.Message}", ex2);
            }

            _isReady = true;

            if (_pendingNavigateUrl is not null)
            {
                var url = _pendingNavigateUrl;
                _pendingNavigateUrl = null;
                LogHelper.Info($"[Browser] Executing pending navigation -> {url}");
                DoNavigate(url);
            }
            else
            {
                LogHelper.Info("[Browser] Kernel ready but no pending navigation");
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[Browser] CoreWebView2 initialization FAILED: {ex.GetType().Name}: {ex.Message}", ex);
            // 注意：Uno 内部可能会吞掉异常，这里显式记录
        }
    }

    public void Initialize(string startUrl)
    {
        LogHelper.Info($"[Browser] Initialize(startUrl={startUrl}) _isReady={_isReady}");
        _pendingNavigateUrl = startUrl;
        if (_webView is not null && _isReady)
        {
            _pendingNavigateUrl = null;
            DoNavigate(startUrl);
        }
        else
        {
            LogHelper.Info("[Browser] Initialize deferred - waiting for CoreWebView2 kernel");
        }
    }

    public void Navigate(string url)
    {
        LogHelper.Info($"[Browser] Navigate(url={url}) _isReady={_isReady}");
        _currentUrl = url;
        if (_webView is not null && _isReady)
        {
            DoNavigate(url);
        }
        else
        {
            LogHelper.Info("[Browser] Navigate deferred - waiting for CoreWebView2 kernel");
            _pendingNavigateUrl = url;
        }
    }

    public void Reload()
    {
        LogHelper.Info("[Browser] Reload requested");
        _webView?.Reload();
    }

    private void DoNavigate(string url)
    {
        if (_webView is null) return;

        LogHelper.Info($"[Browser] DoNavigate -> {url}");

        try
        {
            // 尝试 CoreWebView2.Navigate()（如果可用）和 Source 属性两种方式
            if (_webView.CoreWebView2 is not null)
            {
                LogHelper.Info("[Browser] CoreWebView2 available, using CoreWebView2.Navigate()");
                _webView.CoreWebView2.Navigate(url);
            }
            else
            {
                LogHelper.Info("[Browser] CoreWebView2 is null, using Source property");
                _webView.Source = new Uri(url);
            }

            // 验证导航是否生效：检查 Source 属性值
            LogHelper.Info($"[Browser] After navigate, Source={(object?)_webView.Source}");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[Browser] Navigation FAILED for {url}: {ex.GetType().Name}: {ex.Message}", ex);
            LogHelper.Info("[Browser] Falling back to system browser");
            SystemBrowserProvider.OpenUrl(url);
        }
    }
}
