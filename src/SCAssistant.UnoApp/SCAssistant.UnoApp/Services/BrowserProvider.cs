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
        LogHelper.Info("[浏览器] CreateBrowserControl - 正在创建 WebView2 控件");
        _webView = new WebView2();
        _isReady = false;

        LogHelper.Info($"[浏览器] WebView2 类型: {_webView.GetType().FullName}, 程序集: {_webView.GetType().Assembly.GetName().FullName}");

        // Loaded 仅用于触发内核初始化，不直接执行业务导航
        _webView.Loaded += OnWebViewLoaded;

        // ⚠️ 关键诊断：CoreWebView2Initialized 事件
        // 如果这个事件从未触发 → WebView2 Runtime 缺失或 WPF 程序集加载失败
        _webView.CoreWebView2Initialized += (sender, e) =>
        {
            if (e.Exception is not null)
            {
                LogHelper.Error($"[浏览器] CoreWebView2Initialized 初始化失败: {e.Exception.GetType().Name}: {e.Exception.Message}", e.Exception);
                return;
            }
            LogHelper.Info("[浏览器] CoreWebView2Initialized 成功 - 运行时已完全就绪");
            LogHelper.Info($"[浏览器] CoreWebView2 类型: {sender.CoreWebView2?.GetType().FullName ?? "null"}");
        };

        _webView.NavigationStarting += (_, args) =>
        {
            _isLoading = true;
            _currentUrl = args.Uri?.ToString() ?? string.Empty;
            LogHelper.Info($"[浏览器] 导航开始 -> {_currentUrl}");
            AddressChanged?.Invoke(this, _currentUrl);
            LoadingStateChanged?.Invoke(this, true);
        };

        _webView.NavigationCompleted += (sender, args) =>
        {
            _isLoading = false;
            LogHelper.Info($"[浏览器] 导航完成 成功={args.IsSuccess} 错误={args.WebErrorStatus}");

            try
            {
                if (sender.CoreWebView2 is not null)
                {
                    _currentTitle = sender.CoreWebView2.DocumentTitle ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error("[浏览器] 读取文档标题失败", ex);
            }
            TitleChanged?.Invoke(this, _currentTitle);
            LoadingStateChanged?.Invoke(this, false);
        };

        // 诊断：CoreWebView2 属性访问
        try
        {
            var cv = _webView.CoreWebView2;
            LogHelper.Info($"[浏览器] CoreWebView2 属性(初始化前): {(cv is null ? "null" : cv.GetType().FullName)}");
        }
        catch (Exception ex)
        {
            LogHelper.Info($"[浏览器] CoreWebView2 属性(初始化前)访问异常: {ex.GetType().Name}: {ex.Message}");
        }

        LogHelper.Info($"[浏览器] CreateBrowserControl 完成, _isReady={_isReady}");
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

        LogHelper.Info("[浏览器] WebView2.Loaded - 控件已挂入可视化树，开始初始化内核");
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
            LogHelper.Info("[浏览器] 正在调用 EnsureCoreWebView2Async...");
            await _webView!.EnsureCoreWebView2Async();
            sw.Stop();
            LogHelper.Info($"[浏览器] EnsureCoreWebView2Async 完成，耗时 {sw.ElapsedMilliseconds}ms");

            // 诊断：初始化后检查 CoreWebView2 状态
            try
            {
                var cv = _webView.CoreWebView2;
                LogHelper.Info($"[浏览器] CoreWebView2 初始化后状态: {(cv is null ? "null" : $"类型={cv.GetType().FullName}, 可以导航")}");
                if (cv is not null)
                {
                    LogHelper.Info($"[浏览器] CoreWebView2.Settings={(cv.Settings is null ? "null" : "正常")}");
                }
            }
            catch (Exception ex2)
            {
                LogHelper.Error($"[浏览器] 初始化后检查 CoreWebView2 失败: {ex2.Message}", ex2);
            }

            _isReady = true;

            if (_pendingNavigateUrl is not null)
            {
                var url = _pendingNavigateUrl;
                _pendingNavigateUrl = null;
                LogHelper.Info($"[浏览器] 执行挂起的导航 -> {url}");
                DoNavigate(url);
            }
            else
            {
                LogHelper.Info("[浏览器] 内核就绪，无挂起的导航");
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[浏览器] CoreWebView2 初始化失败: {ex.GetType().Name}: {ex.Message}", ex);
            // 注意：Uno 内部可能会吞掉异常，这里显式记录
        }
    }

    public void Initialize(string startUrl)
    {
        LogHelper.Info($"[浏览器] Initialize(startUrl={startUrl}) _isReady={_isReady}");
        _pendingNavigateUrl = startUrl;
        if (_webView is not null && _isReady)
        {
            _pendingNavigateUrl = null;
            DoNavigate(startUrl);
        }
        else
        {
            LogHelper.Info("[浏览器] 初始化延迟 - 等待 CoreWebView2 内核就绪");
        }
    }

    public void Navigate(string url)
    {
        LogHelper.Info($"[浏览器] Navigate(url={url}) _isReady={_isReady}");
        _currentUrl = url;
        if (_webView is not null && _isReady)
        {
            DoNavigate(url);
        }
        else
        {
            LogHelper.Info("[浏览器] 导航延迟 - 等待 CoreWebView2 内核就绪");
            _pendingNavigateUrl = url;
        }
    }

    public void Reload()
    {
        LogHelper.Info("[浏览器] 请求刷新页面");
        _webView?.Reload();
        LogHelper.Info("[浏览器] Reload 已调用");
    }

    private void DoNavigate(string url)
    {
        if (_webView is null) return;

        LogHelper.Info($"[浏览器] DoNavigate -> {url}");

        try
        {
            // 尝试 CoreWebView2.Navigate()（如果可用）和 Source 属性两种方式
            if (_webView.CoreWebView2 is not null)
            {
                LogHelper.Info("[浏览器] CoreWebView2 可用，使用 CoreWebView2.Navigate()");
                _webView.CoreWebView2.Navigate(url);
            }
            else
            {
                LogHelper.Info("[浏览器] CoreWebView2 为空，使用 Source 属性");
                _webView.Source = new Uri(url);
            }

            // 验证导航是否生效：检查 Source 属性值
            LogHelper.Info($"[浏览器] 导航后 Source={(object?)_webView.Source}");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[浏览器] 导航失败 URL={url}: {ex.GetType().Name}: {ex.Message}", ex);
            LogHelper.Info("[浏览器] 回退到系统浏览器");
            SystemBrowserProvider.OpenUrl(url);
        }
    }
}
