using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

    // 移动端平台标志：Android WebView / iOS WKWebView 行为与桌面 Edge WebView2 不同
    // 两者都是 Skia 渲染层上的原生覆盖视图，需特殊处理布局和导航时序
    private static readonly bool IsIOSPlatform = OperatingSystem.IsIOS();
    private static readonly bool IsAndroidPlatform = OperatingSystem.IsAndroid();
    private static readonly bool IsMobilePlatform = IsIOSPlatform || IsAndroidPlatform;

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

        // 确保 WebView2 拉伸填满父容器
        // 移动端原生 WebView (Android/iOS) 需要显式设置对齐属性，否则可能尺寸为 0 导致空白页
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
            // 内核就绪后，立即注册新窗口处理
            RegisterNewWindowHandler();
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
    /// 
    /// 桌面端（Windows Skia）：使用持久化用户数据文件夹，
    /// 使 cookies、localStorage、缓存等在应用重启后得以保留，
    /// 大幅提升二次启动时的页面加载速度。
    /// </summary>
    private async Task InitializeCoreWebView2Async()
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            LogHelper.Info($"[浏览器] 正在调用 EnsureCoreWebView2Async... (iOS={IsIOSPlatform}, Android={IsAndroidPlatform})");

#if __SKIA__
            // 桌面端 (Windows Skia): 尝试使用持久化用户数据文件夹
            // 这样 cookies / localStorage / 浏览器缓存会被保留，
            // 二次启动无需重新下载 JS/CSS 等静态资源，加载速度显著提升
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

            // 内核就绪后，优化性能设置
            ApplyPerformanceSettings();

            // 注册新窗口处理（防止外部链接点击无反应）
            RegisterNewWindowHandler();

            // 移动端：记录原生 WebView 实际尺寸（排查布局问题）
            if (IsMobilePlatform)
            {
                var platform = IsAndroidPlatform ? "Android" : "iOS";
                LogHelper.Info($"[浏览器] {platform} 原生 WebView 初始化完成: ActualWidth={_webView.ActualWidth}, ActualHeight={_webView.ActualHeight}");
            }

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

#if __SKIA__
    /// <summary>
    /// 桌面端：使用持久化用户数据文件夹初始化 WebView2，
    /// 使浏览器缓存、cookies、localStorage 在应用重启后保留。
    /// </summary>
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

    /// <summary>
    /// 内核就绪后应用性能优化设置。
    /// </summary>
    private void ApplyPerformanceSettings()
    {
        if (_webView?.CoreWebView2?.Settings is null) return;

        try
        {
            var settings = _webView.CoreWebView2.Settings;
            // 确保 Web 消息功能启用（用于与页面通信）
            settings.IsWebMessageEnabled = true;
            LogHelper.Info("[浏览器] 性能设置已应用");
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"[浏览器] 应用性能设置失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 注册 NewWindowRequested 事件处理器。
    /// 当网页中的链接使用 target="_blank" 或 JavaScript window.open() 时，
    /// WebView2 会触发此事件。如果不处理，这些链接点击后无反应。
    /// 这里将新窗口请求拦截并在当前 WebView 中打开，实现普通浏览器般的跳转体验。
    /// </summary>
    private void RegisterNewWindowHandler()
    {
        if (_webView?.CoreWebView2 is null) return;

        try
        {
            _webView.CoreWebView2.NewWindowRequested += (sender, args) =>
            {
                var newUri = args.Uri;
                LogHelper.Info($"[浏览器] NewWindowRequested -> {newUri}，拦截并在当前窗口打开");

                // 阻止打开新窗口，在当前 WebView 中导航
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

        // 移动端：尺寸检查，如果 WebView 仍为 0x0，延迟导航
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
            // 移动端 (Android WebView / iOS WKWebView):
            // 优先使用 Source 属性，因为 Uno 的 CoreWebView2 包装层在移动端可能不可靠
            if (IsMobilePlatform)
            {
                LogHelper.Info("[浏览器] 移动端: 使用 Source 属性导航");
                _webView.Source = new Uri(url);

                // Android: 双重兜底，也尝试 CoreWebView2.Navigate
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

    /// <summary>
    /// 移动端尺寸不足时，延迟等待布局完成后再导航。
    /// </summary>
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
        try
        {
            _webView!.Source = new Uri(url);
        }
        catch { }
    }

    /// <summary>
    /// 移动端 (Android/iOS) 导航二次验证：短暂延迟后检查是否已开始加载，
    /// 如果导航没有触发（仍然白屏），尝试重新使用 Source 属性导航。
    /// </summary>
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
}
