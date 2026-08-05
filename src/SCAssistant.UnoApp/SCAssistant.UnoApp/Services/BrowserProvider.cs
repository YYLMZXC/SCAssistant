using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SCAssistant.UnoApp.Models;

// 桌面端 WebView2 持久化缓存支持
#if __SKIA__
extern alias WpfWebView;
#endif

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
    private UserAgentPlatform _userAgentPlatform = UserAgentPlatform.Auto;

    // 移动端平台标志：Android WebView / iOS WKWebView 行为与桌面 Edge WebView2 不同
    // 两者都是 Skia 渲染层上的原生覆盖视图，需特殊处理布局和导航时序
    private static readonly bool IsIOSPlatform = OperatingSystem.IsIOS();
    private static readonly bool IsAndroidPlatform = OperatingSystem.IsAndroid();
    private static readonly bool IsMobilePlatform = IsIOSPlatform || IsAndroidPlatform;

    public event EventHandler<string>? AddressChanged;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<bool>? LoadingStateChanged;
    public event EventHandler<string>? DownloadRequested;

    public string CurrentUrl => _currentUrl;
    public string CurrentTitle => _currentTitle;
    public bool IsLoading => _isLoading;

    public object CreateBrowserControl()
    {
        LogHelper.Info("[浏览器] CreateBrowserControl - 正在创建 WebView2 控件");
        _webView = new WebView2();
        _isReady = false;

        // 确保 WebView2 拉伸填满父容器
        _webView.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
        _webView.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch;

        LogHelper.Info($"[浏览器] WebView2 类型: {_webView.GetType().FullName}, 程序集: {_webView.GetType().Assembly.GetName().FullName}");

        // Loaded 仅用于触发内核初始化，不直接执行业务导航
        _webView.Loaded += OnWebViewLoaded;

        // iOS: 监听尺寸变化，确保 WKWebView 获得正确的 frame
        _webView.SizeChanged += (sender, e) =>
        {
            var wv = sender as WebView2;
            LogHelper.Info($"[浏览器] SizeChanged: ActualWidth={wv?.ActualWidth}, ActualHeight={wv?.ActualHeight}, Width={wv?.Width}, Height={wv?.Height}, DesiredSize={wv?.DesiredSize}");
        };

        // CoreWebView2Initialized 事件
        _webView.CoreWebView2Initialized += (sender, e) =>
        {
            if (e.Exception is not null)
            {
                LogHelper.Error($"[浏览器] CoreWebView2Initialized 初始化失败: {e.Exception.GetType().Name}: {e.Exception.Message}", e.Exception);
                return;
            }
            LogHelper.Info("[浏览器] CoreWebView2Initialized 成功 - 运行时已完全就绪");
            LogHelper.Info($"[浏览器] CoreWebView2 类型: {sender.CoreWebView2?.GetType().FullName ?? "null"}");
            RegisterNewWindowHandler();
            RegisterDownloadHandler();
            ApplyUserAgent();
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

    private void OnWebViewLoaded(object sender, RoutedEventArgs e)
    {
        if (_webView is null) return;
        _webView.Loaded -= OnWebViewLoaded;

        LogHelper.Info("[浏览器] WebView2.Loaded - 控件已挂入可视化树，开始初始化内核");
        _ = InitializeCoreWebView2Async();
    }

    private async Task InitializeCoreWebView2Async()
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            LogHelper.Info($"[浏览器] 正在调用 EnsureCoreWebView2Async... (iOS={IsIOSPlatform}, Android={IsAndroidPlatform})");

#if __SKIA__
            if (OperatingSystem.IsWindows())
            {
                await InitializeWithUserDataFolderAsync();
            }
            else
            {
                await _webView!.EnsureCoreWebView2Async();
            }
#else
            await _webView!.EnsureCoreWebView2Async();
#endif

            sw.Stop();
            LogHelper.Info($"[浏览器] EnsureCoreWebView2Async 完成，耗时 {sw.ElapsedMilliseconds}ms");

            ApplyPerformanceSettings();
            RegisterNewWindowHandler();
            RegisterDownloadHandler();
            ApplyUserAgent();

            if (IsMobilePlatform)
            {
                var platform = IsAndroidPlatform ? "Android" : "iOS";
                LogHelper.Info($"[浏览器] {platform} 原生 WebView 初始化完成: ActualWidth={_webView.ActualWidth}, ActualHeight={_webView.ActualHeight}");
            }

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
        }
    }

#if __SKIA__
    private async Task InitializeWithUserDataFolderAsync()
    {
        try
        {
            var userDataFolder = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SCAssistant", "WebView2Data");
            System.IO.Directory.CreateDirectory(userDataFolder);

            LogHelper.Info($"[浏览器] 正在创建 CoreWebView2Environment (userDataFolder={userDataFolder})");
            var env = await WpfWebView::Microsoft.Web.WebView2.Core.CoreWebView2Environment
                .CreateAsync(userDataFolder: userDataFolder);
            await _webView!.EnsureCoreWebView2Async(env);
            LogHelper.Info($"[浏览器] 使用持久化缓存目录初始化成功: {userDataFolder}");
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"[浏览器] 创建自定义 CoreWebView2Environment 失败: {ex.Message}，回退默认初始化");
            await _webView!.EnsureCoreWebView2Async();
        }
    }
#endif

    private void ApplyPerformanceSettings()
    {
        if (_webView?.CoreWebView2?.Settings is null) return;

        try
        {
            var settings = _webView.CoreWebView2.Settings;
            settings.IsWebMessageEnabled = true;
            LogHelper.Info("[浏览器] 性能设置已应用");
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"[浏览器] 应用性能设置失败: {ex.Message}");
        }
    }

    private void RegisterNewWindowHandler()
    {
        if (_webView?.CoreWebView2 is null) return;

        try
        {
            _webView.CoreWebView2.NewWindowRequested += (sender, args) =>
            {
                var newUri = args.Uri;
                LogHelper.Info($"[浏览器] NewWindowRequested -> {newUri}，拦截并在当前窗口打开");
                args.Handled = true;
                Navigate(newUri);
            };
            LogHelper.Info("[浏览器] NewWindowRequested 事件已注册");
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"[浏览器] 注册 NewWindowRequested 失败: {ex.Message}");
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

        LogHelper.Info($"[浏览器] DoNavigate -> {url} (iOS={IsIOSPlatform}, Android={IsAndroidPlatform})");

        // 移动端：尺寸检查
        if (IsMobilePlatform && (_webView.ActualWidth <= 0 || _webView.ActualHeight <= 0))
        {
            var platform = IsAndroidPlatform ? "Android" : "iOS";
            LogHelper.Warn($"[浏览器] {platform}: WebView 尺寸为 {_webView.ActualWidth}x{_webView.ActualHeight}，延迟导航");
            _pendingNavigateUrl = url;
            _ = DeferredMobileNavigateAsync(url);
            return;
        }

        try
        {
            if (IsMobilePlatform)
            {
                LogHelper.Info("[浏览器] 移动端: 使用 Source 属性导航");
                _webView.Source = new Uri(url);

                if (IsAndroidPlatform && _webView.CoreWebView2 is not null)
                {
                    try
                    {
                        _webView.CoreWebView2.Navigate(url);
                        LogHelper.Info("[浏览器] Android: 同时触发 CoreWebView2.Navigate 兜底");
                    }
                    catch (Exception ex2)
                    {
                        LogHelper.Warn($"[浏览器] Android CoreWebView2.Navigate 兜底失败: {ex2.Message}");
                    }
                }
            }
            else if (_webView.CoreWebView2 is not null)
            {
                LogHelper.Info("[浏览器] 桌面: CoreWebView2 可用，使用 CoreWebView2.Navigate()");
                _webView.CoreWebView2.Navigate(url);
            }
            else
            {
                LogHelper.Info("[浏览器] CoreWebView2 为空，使用 Source 属性");
                _webView.Source = new Uri(url);
            }

            LogHelper.Info($"[浏览器] 导航后 Source={(object?)_webView.Source}");

            if (IsMobilePlatform)
            {
                _ = VerifyMobileNavigationAsync(url);
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[浏览器] 导航失败 URL={url}: {ex.GetType().Name}: {ex.Message}", ex);
            LogHelper.Info("[浏览器] 回退到系统浏览器");
            SystemBrowserProvider.OpenUrl(url);
        }
    }

    private async Task DeferredMobileNavigateAsync(string url)
    {
        var platform = IsAndroidPlatform ? "Android" : "iOS";
        var maxWait = 3000;
        var elapsed = 0;

        while (elapsed < maxWait && _webView is not null)
        {
            await Task.Delay(200);
            elapsed += 200;

            if (_webView.ActualWidth > 0 && _webView.ActualHeight > 0)
            {
                LogHelper.Info($"[浏览器] {platform}: 尺寸就绪 ({elapsed}ms, {_webView.ActualWidth}x{_webView.ActualHeight})，执行延迟导航");
                _pendingNavigateUrl = null;
                DoNavigate(url);
                return;
            }
        }

        LogHelper.Warn($"[浏览器] {platform}: 延迟导航超时 ({maxWait}ms)，强制尝试");
        _pendingNavigateUrl = null;
        try { _webView!.Source = new Uri(url); } catch { }
    }

    private async Task VerifyMobileNavigationAsync(string url)
    {
        try
        {
            await Task.Delay(1000);
            if (_webView is null) return;

            var platform = IsAndroidPlatform ? "Android" : "iOS";
            var currentSource = _webView.Source?.ToString();
            LogHelper.Info($"[浏览器] {platform} 导航验证: 期望={url}, 实际Source={currentSource ?? "null"}, IsLoading={_isLoading}");

            if (currentSource != url && !_isLoading)
            {
                LogHelper.Warn($"[浏览器] {platform}: 导航似乎未生效，重试 Source 设置");
                _webView.Source = new Uri(url);
            }
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"[浏览器] 移动端导航验证异常: {ex.Message}");
        }
    }

    // =============================================================================
    // 用户代理（User-Agent）设置
    // =============================================================================

    public void SetUserAgent(UserAgentPlatform platform)
    {
        LogHelper.Info($"[浏览器] SetUserAgent: {platform}");
        _userAgentPlatform = platform;
        ApplyUserAgent();
    }

    /// <summary>
    /// 将 UA 设置应用到 WebView。
    /// 桌面端: 直接设置 CoreWebView2.Settings.UserAgent。
    /// 移动端: 注入 JS 覆盖 navigator.userAgent（Uno WebView2 可能不暴露原生 UA 设置）。
    /// </summary>
    private void ApplyUserAgent()
    {
        if (_webView is null) return;

        var ua = GetUserAgentString(_userAgentPlatform);
        LogHelper.Info($"[浏览器] ApplyUserAgent 平台={_userAgentPlatform}, UA={(ua is null ? "跟随系统" : ua)}");

        // 桌面端：直接设置 WebView2 的 UA
        if (!IsMobilePlatform && _webView.CoreWebView2?.Settings is not null)
        {
            try
            {
                _webView.CoreWebView2.Settings.UserAgent = ua;
                LogHelper.Info("[浏览器] UA 已通过 CoreWebView2.Settings 设置");
                return;
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"[浏览器] 直接设置 UA 失败: {ex.Message}，尝试 JS 注入");
            }
        }

        // 移动端 / 桌面端 UA 设置失败时的兜底：JS 覆盖 navigator.userAgent
        if (ua is not null && _webView.CoreWebView2 is not null)
        {
            _ = InjectUserAgentAsync(ua);
        }
    }

    private async Task InjectUserAgentAsync(string ua)
    {
        try
        {
            var escapedUa = ua.Replace("'", "\\'");
            var js = $$"""
            Object.defineProperty(navigator, 'userAgent', {
                get: function() { return '{{escapedUa}}'; }
            });
            Object.defineProperty(navigator, 'appVersion', {
                get: function() { return '{{escapedUa}}'; }
            });
            """;
            if (_webView?.CoreWebView2 is not null)
            {
                await _webView.CoreWebView2.ExecuteScriptAsync(js);
                LogHelper.Info("[浏览器] UA JS 覆盖已注入");
            }
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"[浏览器] UA JS 注入失败: {ex.Message}");
        }
    }

    private static string? GetUserAgentString(UserAgentPlatform platform)
    {
        return platform switch
        {
            UserAgentPlatform.Desktop => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            UserAgentPlatform.Mobile => "Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36",
            _ => null // Auto — 不覆盖
        };
    }

    // =============================================================================
    // Cookie 获取（用于下载鉴权）
    // =============================================================================

    public async Task<string> GetCookiesAsync(string url)
    {
        try
        {
            // 桌面端: CoreWebView2.CookieManager
            if (!IsMobilePlatform && _webView?.CoreWebView2?.CookieManager is not null)
            {
                var cookies = await _webView.CoreWebView2.CookieManager.GetCookiesAsync(url);
                var cookieStrings = new System.Collections.Generic.List<string>();
                foreach (var c in cookies)
                {
                    cookieStrings.Add($"{c.Name}={c.Value}");
                }
                var result = string.Join("; ", cookieStrings);
                LogHelper.Info($"[浏览器] 获取了 {cookieStrings.Count} 个 Cookie");
                return result;
            }

            // 移动端：通过 JS 获取 document.cookie
            if (_webView?.CoreWebView2 is not null)
            {
                var cookieStr = await _webView.CoreWebView2.ExecuteScriptAsync("document.cookie");
                if (cookieStr is not null)
                {
                    var trimmed = cookieStr.Trim('"');
                    LogHelper.Info($"[浏览器] JS document.cookie 获取成功，长度={trimmed.Length}");
                    return trimmed;
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"[浏览器] 获取 Cookie 失败: {ex.Message}");
        }
        return string.Empty;
    }

    // =============================================================================
    // 下载处理（跨平台统一管线）
    // =============================================================================

    /// <summary>
    /// 注册下载事件处理（跨平台统一下载管线）。
    ///
    /// 策略:
    /// - 桌面端 (WebView2): 拦截 CoreWebView2.DownloadStarting，取消原生下载对话框，
    ///   统一走本系统自定义下载管线（显示进度、保存到下载目录）。
    /// - Android/iOS: 使用双重拦截方案：
    ///   1) 页面加载后注入 JS，拦截已知扩展名的 &lt;a&gt; 点击
    ///   2) NavigationStarting 中检测下载型 URL 作为兜底
    ///   3) WebMessageReceived 接收 JS 通知
    /// </summary>
    private void RegisterDownloadHandler()
    {
        if (_webView is null) return;

        try
        {
            // === 桌面端: 拦截 DownloadStarting，取消原生对话框，统一走自定义下载 ===
            if (!IsMobilePlatform)
            {
                RegisterDesktopDownloadHandler();
            }

            // === 移动端: JS 注入 + NavigationStarting 双重拦截 ===
            if (IsMobilePlatform)
            {
                RegisterMobileDownloadHandler();
            }

            // === 所有平台: WebMessageReceived（JS 通知下载） ===
            RegisterWebMessageHandler();
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"[浏览器] 注册下载处理失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 桌面端：拦截 WebView2 原生下载，取消默认对话框，统一走自定义下载管线。
    /// </summary>
    private void RegisterDesktopDownloadHandler()
    {
        if (_webView?.CoreWebView2 is null) return;

        try
        {
            _webView.CoreWebView2.DownloadStarting += (sender, args) =>
            {
                var dlUrl = args.DownloadOperation.Uri;
                var filePath = args.DownloadOperation.ResultFilePath ?? string.Empty;
                LogHelper.Info($"[浏览器] 桌面端 DownloadStarting 拦截 -> URL={dlUrl}, Path={filePath}");

                // 取消 WebView2 原生下载对话框
                args.Cancel = true;

                // 走统一的自定义下载管线
                DownloadRequested?.Invoke(this, dlUrl);
            };
            LogHelper.Info("[浏览器] 桌面端 DownloadStarting 拦截已注册 — 下载将走统一管线");
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"[浏览器] 注册 DownloadStarting 失败，回退原生下载: {ex.Message}");
        }
    }

    /// <summary>
    /// 移动端：JS 注入拦截 + NavigationStarting 检测。
    /// </summary>
    private void RegisterMobileDownloadHandler()
    {
        // 每次页面导航完成后注入下载拦截 JS
        _webView!.NavigationCompleted += (sender, args) =>
        {
            _ = InjectDownloadInterceptorAsync();
        };

        // NavigationStarting 中检测下载型 URL（兜底）
        _webView.NavigationStarting += (sender, args) =>
        {
            var url = args.Uri?.ToString();
            if (string.IsNullOrWhiteSpace(url)) return;

            if (IsDownloadableUrl(url))
            {
                args.Cancel = true;
                LogHelper.Info($"[浏览器] 移动端 NavigationStarting 检测到下载链接，拦截 -> {url}");
                DownloadRequested?.Invoke(this, url);
            }
        };

        LogHelper.Info("[浏览器] 移动端下载拦截已注册 (JS注入 + NavigationStarting 检测)");
    }

    /// <summary>
    /// 所有平台：监听 WebMessage，处理 JS postMessage 通知的下载请求。
    /// </summary>
    private void RegisterWebMessageHandler()
    {
        if (_webView is null) return;

        _webView.WebMessageReceived += (_, args) =>
        {
            var raw = args.WebMessageAsJson;
            if (string.IsNullOrWhiteSpace(raw)) return;

            var message = raw.Trim('"');
            if (message is not null && message.StartsWith("download:", StringComparison.Ordinal))
            {
                var downloadUrl = message["download:".Length..];
                LogHelper.Info($"[浏览器] JS postMessage 通知下载 -> {downloadUrl}");
                DownloadRequested?.Invoke(this, downloadUrl);
            }
        };

        LogHelper.Info("[浏览器] WebMessageReceived 已注册");
    }

    /// <summary>
    /// 在当前页面注入下载拦截 JavaScript。
    /// 拦截已知下载扩展名的 &lt;a&gt; 标签点击，通过 postMessage 通知 C# 层。
    /// 使用跨平台兼容的 postMessage 封装，兼容 Uno 各平台 WebView2 桥接。
    /// </summary>
    private async Task InjectDownloadInterceptorAsync()
    {
        if (_webView?.CoreWebView2 is null) return;

        try
        {
            await _webView.CoreWebView2.ExecuteScriptAsync(DownloadInterceptorJs);
            LogHelper.Info("[浏览器] 下载拦截 JS 注入完成");
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"[浏览器] JS 注入失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 下载拦截 JavaScript。
    /// 跨平台兼容：优先使用 window.chrome.webview.postMessage（Uno 在桌面端 polyfill），
    /// 同时存在 window.external.notify 作为移动端兜底。
    /// </summary>
    private const string DownloadInterceptorJs = @"
(function() {
    if (window.__scDownloadInterceptorInstalled) return;
    window.__scDownloadInterceptorInstalled = true;

    var downloadExtensions = [
        '.apk','.zip','.rar','.7z','.tar','.gz','.bz2','.xz',
        '.mp3','.wav','.ogg','.flac','.aac','.m4a',
        '.mp4','.avi','.mkv','.mov','.wmv','.flv','.webm',
        '.pdf','.doc','.docx','.xls','.xlsx','.ppt','.pptx',
        '.txt','.csv','.json','.xml','.log',
        '.png','.jpg','.jpeg','.gif','.bmp','.svg','.webp',
        '.exe','.msi','.dmg','.deb','.rpm',
        '.iso','.img'
    ];

    function isDownloadLink(url) {
        if (!url) return false;
        var lower = url.toLowerCase().split('?')[0].split('#')[0];
        for (var i = 0; i < downloadExtensions.length; i++) {
            if (lower.endsWith(downloadExtensions[i])) return true;
        }
        return false;
    }

    // 跨平台 postMessage 封装
    function notifyApp(msg) {
        var fullMsg = 'download:' + msg;
        try {
            // Uno 桌面端: chrome.webview.postMessage (主要)
            if (window.chrome && window.chrome.webview && typeof window.chrome.webview.postMessage === 'function') {
                window.chrome.webview.postMessage(fullMsg);
                return;
            }
        } catch(e) {}
        try {
            // Uno 移动端 / 旧版本兼容: external.notify
            if (window.external && typeof window.external.notify === 'function') {
                window.external.notify(fullMsg);
                return;
            }
        } catch(e) {}
        try {
            // Android WebView JavaScriptInterface 兼容
            if (window.__scBridge) {
                window.__scBridge.postMessage(fullMsg);
                return;
            }
        } catch(e) {}
    }

    // 方案 A: 全局点击委托（捕获阶段）
    document.addEventListener('click', function(e) {
        var target = e.target;
        while (target && target !== document) {
            if (target.tagName === 'A' && target.href) {
                if (isDownloadLink(target.href)) {
                    e.preventDefault();
                    e.stopPropagation();
                    notifyApp(target.href);
                    return false;
                }
                return;
            }
            target = target.parentElement;
        }
    }, true);

    // 方案 B: 拦截所有带 download 属性的 <a> 标签
    var links = document.querySelectorAll('a[download]');
    for (var i = 0; i < links.length; i++) {
        (function(link) {
            link.addEventListener('click', function(e) {
                if (isDownloadLink(link.href)) {
                    e.preventDefault();
                    e.stopPropagation();
                    notifyApp(link.href);
                }
            });
        })(links[i]);
    }

    // 方案 C: 拦截所有 <a> 标签（全面覆盖）
    var allLinks = document.querySelectorAll('a');
    for (var j = 0; j < allLinks.length; j++) {
        (function(link) {
            if (link.__scDownloadHooked) return;
            link.__scDownloadHooked = true;
            link.addEventListener('click', function(e) {
                if (isDownloadLink(link.href)) {
                    e.preventDefault();
                    e.stopPropagation();
                    notifyApp(link.href);
                }
            });
        })(allLinks[j]);
    }
})();
";

    /// <summary>
    /// 判断 URL 是否指向可下载文件（按扩展名匹配）。
    /// 作为 JS 注入拦截的兜底方案。
    /// </summary>
    private static bool IsDownloadableUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        var extensions = new[]
        {
            ".apk", ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz",
            ".mp3", ".wav", ".ogg", ".flac", ".aac", ".m4a",
            ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm",
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".txt", ".csv", ".json", ".xml", ".log",
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".svg", ".webp",
            ".exe", ".msi", ".dmg", ".deb", ".rpm",
            ".iso", ".img"
        };

        var path = new Uri(url).AbsolutePath.ToLowerInvariant();
        var queryIndex = path.IndexOf('?');
        if (queryIndex >= 0) path = path.Substring(0, queryIndex);

        foreach (var ext in extensions)
        {
            if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                LogHelper.Info($"[浏览器] IsDownloadableUrl -> {url} (匹配扩展名 {ext})");
                return true;
            }
        }
        return false;
    }
}
