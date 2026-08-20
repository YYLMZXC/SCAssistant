using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SCAssistant.UnoApp.Models;

// 桌面端 WebView2 持久化缓存支持
#if __SKIA__
extern alias WpfWebView;
#endif

// Android 平台：自定义 URL scheme 拦截
#if __ANDROID__
using Android.Content;
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
    private bool _canGoBack;
    private bool _canGoForward;
    private string? _pendingNavigateUrl;
    private bool _isReady;
    private UserAgentPlatform _userAgentPlatform = UserAgentPlatform.Auto;

    // 幂等标志：保证事件处理器只注册一次。
    // 背景：CoreWebView2Initialized 事件与 EnsureCoreWebView2Async 完成后是两条初始化路径，
    // 都会触发注册；若无幂等保护，DownloadStarting / NewWindowRequested / WebMessageReceived
    // 等事件会被重复订阅，导致下载请求被触发多次等业务 bug。
    private bool _isNewWindowHandlerRegistered;
    private bool _isDesktopDownloadHandlerRegistered;
    private bool _isMobileDownloadHandlerRegistered;
    private bool _isWebMessageHandlerRegistered;
    private bool _isCoreHttpStatusLogRegistered;

    /// <summary>已成功应用的 UA 平台，避免重复设置（性能冗余）。</summary>
    private UserAgentPlatform? _appliedUserAgentPlatform;

    /// <summary>CoreWebView2 内核初始化超时时间。</summary>
    private static readonly TimeSpan CoreInitTimeout = TimeSpan.FromSeconds(30);

    /// <summary>上一个有效的 HTTP/HTTPS 页面 URL，用于 scheme 跳转后恢复黑屏。</summary>
    private string _lastKnownGoodUrl = string.Empty;

    /// <summary>是否正在从 scheme 跳转恢复（防止恢复期间的递归处理）。</summary>
    private bool _isRestoringFromScheme;

    // 移动端平台标志：Android WebView / iOS WKWebView 行为与桌面 Edge WebView2 不同
    // 两者都是 Skia 渲染层上的原生覆盖视图，需特殊处理布局和导航时序
    private static readonly bool IsIOSPlatform = OperatingSystem.IsIOS();
    private static readonly bool IsAndroidPlatform = OperatingSystem.IsAndroid();
    private static readonly bool IsMobilePlatform = IsIOSPlatform || IsAndroidPlatform;

    public event EventHandler<string>? AddressChanged;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<bool>? LoadingStateChanged;
    public event EventHandler<string>? DownloadRequested;
    public event EventHandler? NavigationHistoryChanged;

    public string CurrentUrl => _currentUrl;
    public string CurrentTitle => _currentTitle;
    public bool IsLoading => _isLoading;
    public bool CanGoBack => _canGoBack;
    public bool CanGoForward => _canGoForward;

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

		_webView.NavigationStarting += (sender, args) =>
		{
			var url = args.Uri?.ToString() ?? string.Empty;

#if __ANDROID__
			// 拦截非 http/https 自定义 URL scheme（如 wtloginmqq://、mqq:// 等）
			// Android WebView 默认丢弃这类导航，需通过 Intent 跳转到对应 App
			if (!string.IsNullOrEmpty(url) && TryHandleCustomScheme(url))
			{
				args.Cancel = true;
				// 自定义 scheme 跳转（如 QQ 登录 wtloginmqq://）后，Android WebView
				// 通常会因为导航被中途取消而变为空白页。
				// 延迟恢复到上一个有效的 HTTP 页面，防止黑屏。
				if (!_isRestoringFromScheme)
				{
					_ = RestoreAfterSchemeRedirectAsync();
				}
				return;
			}
#endif

			// 记录正常的 HTTP/HTTPS 导航 URL，用于 scheme 跳转后页面恢复
			if (!string.IsNullOrEmpty(url) &&
				(url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
				 url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
			{
				_lastKnownGoodUrl = url;
			}

			_isLoading = true;
			_currentUrl = url;
			LogHelper.Info($"[浏览器] 导航开始 -> {_currentUrl}");
			AddressChanged?.Invoke(this, _currentUrl);
			LoadingStateChanged?.Invoke(this, true);
		};

        _webView.NavigationCompleted += (sender, args) =>
        {
            _isLoading = false;

            // 注意：WebView2 导航成功时 WebErrorStatus 恒为 Unknown（正常行为），
            // 不能拿 WebErrorStatus 判断成败，仅在失败时输出它做诊断。
            if (args.IsSuccess)
            {
                LogHelper.Info("[浏览器] 导航完成 成功");
            }
            else
            {
                LogHelper.Warn($"[浏览器] 导航完成 失败, WebErrorStatus={args.WebErrorStatus}");
            }

            try
            {
                if (sender.CoreWebView2 is not null)
                {
                    _currentTitle = sender.CoreWebView2.DocumentTitle ?? string.Empty;
                    _canGoBack = sender.CoreWebView2.CanGoBack;
                    _canGoForward = sender.CoreWebView2.CanGoForward;
                    NavigationHistoryChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error("[浏览器] 读取文档标题失败", ex);
            }
            TitleChanged?.Invoke(this, _currentTitle);
            LoadingStateChanged?.Invoke(this, false);

            // 页面加载完成后注入资源失败钩子，上报子资源（图片/脚本等）加载失败
            _ = InjectResourceErrorHookAsync();
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

            // 初始化超时保护：内核长时间无响应时避免页面永久空白且无日志
            var initTask = InitializeWebViewCoreAsync();
            var completedTask = await Task.WhenAny(initTask, Task.Delay(CoreInitTimeout));
            if (completedTask != initTask)
            {
                sw.Stop();
                LogHelper.Error($"[浏览器] CoreWebView2 初始化超时（{CoreInitTimeout.TotalSeconds} 秒），放弃后续初始化。请检查 WebView2 Runtime 是否安装或可用。");
                return;
            }
            await initTask; // 初始化内部若抛异常，在此传播到下方 catch

            sw.Stop();
            LogHelper.Info($"[浏览器] EnsureCoreWebView2Async 完成，耗时 {sw.ElapsedMilliseconds}ms");

            // 内核就绪：先置位，供后续注册/UA 方法判断（此时 CoreWebView2 才是真实内核而非占位对象）
            _isReady = true;

            ApplyPerformanceSettings();
            RegisterCoreHttpStatusLog();
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

    /// <summary>
    /// 执行 WebView2 内核初始化（不同平台路径不同）。
    /// 抽离为独立方法以便在 InitializeCoreWebView2Async 中统一加超时控制。
    /// </summary>
    private async Task InitializeWebViewCoreAsync()
    {
#if __SKIA__
        if (OperatingSystem.IsWindows())
        {
            await InitializeWithUserDataFolderAsync();
            return;
        }
#endif
        // 注：EnsureCoreWebView2Async 在不同平台返回类型不同（桌面为 Task，移动端为 IAsyncAction），
        // 统一用 await 兼容
        await _webView!.EnsureCoreWebView2Async();
    }

#if __SKIA__
    private async Task InitializeWithUserDataFolderAsync()
    {
        try
        {
            var userDataFolder = AppPaths.WebView2;
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

    /// <summary>
    /// 注册 CoreWebView2 级导航完成监听，仅用于记录主文档 HTTP 状态码异常（4xx/5xx）。
    /// 说明：控件级 NavigationCompleted 参数（WebViewNavigationCompletedEventArgs）不暴露
    /// HTTP 状态码，而 Core 级参数（CoreWebView2NavigationCompletedEventArgs）有 HttpStatusCode。
    /// WebView2 中 404/500 等错误页 IsSuccess 仍为 true，必须借助状态码才能发现。
    /// </summary>
    private void RegisterCoreHttpStatusLog()
    {
        if (!_isReady) return;
        if (_isCoreHttpStatusLogRegistered) return; // 幂等：只注册一次
        if (_webView?.CoreWebView2 is null) return;

        try
        {
            _webView.CoreWebView2.NavigationCompleted += (_, args) =>
            {
                var code = args.HttpStatusCode;
                if (code >= 400)
                {
                    LogHelper.Warn($"[浏览器] 主文档 HTTP 状态码异常: {code}");
                }
            };
            _isCoreHttpStatusLogRegistered = true;
            LogHelper.Info("[浏览器] Core 级 HTTP 状态码监听已注册");
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"[浏览器] 注册 HTTP 状态码监听失败: {ex.Message}");
        }
    }

    private bool _isHandlingNewWindow; // 防止 NewWindowRequested 重入
    private void RegisterNewWindowHandler()
    {
        // 内核未就绪时 CoreWebView2 可能是占位对象，注册无效；幂等防止重复注册
        if (!_isReady) return;
        if (_isNewWindowHandlerRegistered) return;
        if (_webView?.CoreWebView2 is null) return;

        try
        {
            _webView.CoreWebView2.NewWindowRequested += (sender, args) =>
            {
                if (_isHandlingNewWindow) return; // 防止重入
                _isHandlingNewWindow = true;

                try
                {
                    var newUri = args.Uri;
                    LogHelper.Info($"[浏览器] NewWindowRequested -> {newUri}，拦截并在当前窗口打开");
                    args.Handled = true;

                    // 如果是可下载文件，直接触发下载，不要导航
                    if (IsDownloadableUrl(newUri))
                    {
                        LogHelper.Info($"[浏览器] NewWindowRequested 检测到下载链接，触发下载: {newUri}");
                        DownloadRequested?.Invoke(this, newUri);
                    }
                    else
                    {
                        Navigate(newUri);
                    }
                }
                finally
                {
                    _isHandlingNewWindow = false;
                }
            };
            _isNewWindowHandlerRegistered = true;
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
        _lastKnownGoodUrl = startUrl;
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

    public void GoBack()
    {
        LogHelper.Info("[浏览器] 请求后退");
        if (_webView?.CoreWebView2 is not null && _canGoBack)
        {
            _webView.CoreWebView2.GoBack();
        }
    }

    public void GoForward()
    {
        LogHelper.Info("[浏览器] 请求前进");
        if (_webView?.CoreWebView2 is not null && _canGoForward)
        {
            _webView.CoreWebView2.GoForward();
        }
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
                // 注意：Uno WebView2 的 Source setter 在 Android 上会触发原生 WebView.loadUrl。
                // 这里不能再额外调用 CoreWebView2.Navigate，否则同一个 URL 会被 loadUrl 两次，
                // 导致网页重复加载（页面闪烁、资源重复请求、状态被重置）。
                // 若 Source 导航确实失败，由 VerifyMobileNavigationAsync 在 1 秒后兜底重试。
                _webView.Source = new Uri(url);
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

            // 桌面端使用 CoreWebView2.Navigate() 时，控件 Source 属性不会立即同步，
            // 此时读取会得到空值，容易误导排查。仅在移动端（刚设置过 Source）记录。
            if (IsMobilePlatform)
            {
                LogHelper.Info($"[浏览器] 导航后 Source={_webView.Source?.ToString() ?? "null"}");
            }

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
                LogHelper.Warn($"[浏览器] {platform}: 导航似乎未生效，重试导航");
#if __ANDROID__
                // Android 兜底：使用原生 Navigate 重试（避免再次设置 Source 造成重复导航）。
                // 仅在 Source 导航确实未生效时才走到这里。
                try
                {
                    if (_webView.CoreWebView2 is not null)
                    {
                        _webView.CoreWebView2.Navigate(url);
                        return;
                    }
                }
                catch (Exception ex2)
                {
                    LogHelper.Warn($"[浏览器] Android 导航重试(Navigate)失败: {ex2.Message}");
                }
#endif
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

        // 内核未就绪时 CoreWebView2 可能是占位对象，设置无效；等就绪后统一应用一次
        if (!_isReady) return;

        // 已为当前平台成功应用过 UA，跳过避免重复设置（性能冗余）
        if (_appliedUserAgentPlatform == _userAgentPlatform) return;

        var ua = GetUserAgentString(_userAgentPlatform);
        LogHelper.Info($"[浏览器] ApplyUserAgent 平台={_userAgentPlatform}, UA={(ua is null ? "跟随系统" : ua)}");

        // 桌面端：直接设置 WebView2 的 UA
        if (!IsMobilePlatform && _webView.CoreWebView2?.Settings is not null)
        {
            try
            {
                _webView.CoreWebView2.Settings.UserAgent = ua;
                _appliedUserAgentPlatform = _userAgentPlatform;
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
            _appliedUserAgentPlatform = _userAgentPlatform; // 标记已应用，避免重复注入
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
        // 内核未就绪时 CoreWebView2 可能是占位对象，注册无效；幂等防止重复注册
        if (!_isReady) return;
        if (_isDesktopDownloadHandlerRegistered) return;
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
            _isDesktopDownloadHandlerRegistered = true;
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
        if (!_isReady) return;
        if (_isMobileDownloadHandlerRegistered) return; // 幂等：只注册一次
        _isMobileDownloadHandlerRegistered = true;

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
        if (!_isReady) return;
        if (_isWebMessageHandlerRegistered) return; // 幂等：只注册一次
        if (_webView is null) return;

        _webView.WebMessageReceived += (_, args) =>
        {
            var raw = args.WebMessageAsJson;
            if (string.IsNullOrWhiteSpace(raw)) return;

            var message = raw.Trim('"');
            if (message is null) return;

            if (message.StartsWith("download:", StringComparison.Ordinal))
            {
                var downloadUrl = message["download:".Length..];
                LogHelper.Info($"[浏览器] JS postMessage 通知下载 -> {downloadUrl}");
                DownloadRequested?.Invoke(this, downloadUrl);
            }
            else if (message.StartsWith("log:", StringComparison.Ordinal))
            {
                // 页面内资源失败日志（低优先级诊断）
                var logMsg = message["log:".Length..];
                if (logMsg.StartsWith("resource-error:", StringComparison.Ordinal))
                {
                    LogHelper.Warn($"[浏览器] 资源加载失败: {logMsg["resource-error:".Length..]}");
                }
                else if (logMsg.StartsWith("js-error:", StringComparison.Ordinal))
                {
                    LogHelper.Warn($"[浏览器] 页面 JS 错误: {logMsg["js-error:".Length..]}");
                }
            }
        };

        _isWebMessageHandlerRegistered = true;
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
        '.iso','.img',
        // SurvivalCraft 模组与地图文件
        '.scmod','.scworld','.scmap','.scskin','.sctexture',
        // SurvivalCraft netmod 与 dll 模组文件
        '.netmod','.dll'
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
    /// 在当前页面注入资源加载失败监听 JS：
    /// - 子资源（图片/脚本/样式等）加载失败 -> postMessage("log:resource-error:...")
    /// - 页面脚本运行时错误 -> postMessage("log:js-error:...")
    /// C# 侧在 RegisterWebMessageHandler 中解析并记录日志。
    /// </summary>
    private async Task InjectResourceErrorHookAsync()
    {
        if (_webView?.CoreWebView2 is null) return;

        try
        {
            await _webView.CoreWebView2.ExecuteScriptAsync(ResourceErrorHookJs);
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"[浏览器] 资源失败钩子 JS 注入失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 资源加载失败监听 JavaScript（低优先级诊断增强）。
    /// 跨平台兼容：优先 window.chrome.webview.postMessage，移动端回退 window.external.notify。
    /// </summary>
    private const string ResourceErrorHookJs = @"
(function() {
    if (window.__scResourceErrorHookInstalled) return;
    window.__scResourceErrorHookInstalled = true;

    function postLog(msg) {
        try {
            if (window.chrome && window.chrome.webview && typeof window.chrome.webview.postMessage === 'function') {
                window.chrome.webview.postMessage(msg);
                return;
            }
        } catch(e) {}
        try {
            if (window.external && typeof window.external.notify === 'function') {
                window.external.notify(msg);
                return;
            }
        } catch(e) {}
    }

    // 子资源加载失败（img/script/link/audio/video 等）
    document.addEventListener('error', function(e) {
        var el = e.target;
        var src = el && (el.src || el.href);
        if (src) postLog('log:resource-error:' + src);
    }, true);

    // 页面脚本运行时错误
    window.addEventListener('error', function(e) {
        var msg = 'log:js-error:' + (e.message || 'unknown') + ' @ ' + (e.filename || '') + ':' + (e.lineno || 0);
        postLog(msg);
    });
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
            ".iso", ".img",
            // SurvivalCraft 模组与地图文件
            ".scmod", ".scworld", ".scmap", ".scskin", ".sctexture",
            // SurvivalCraft netmod 与 dll 模组文件
            ".netmod", ".dll"
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

#if __ANDROID__
    /// <summary>
    /// Android：判断 URL 是否属于标准 Web scheme（http/https/file/about/data/javascript/blob），
    /// 对于自定义 URL scheme（如 wtloginmqq://、mqq:// 等），通过 Android Intent 跳转到对应 App。
    /// 返回 true 表示已通过 Intent 处理，调用方应取消 WebView 导航。
    /// </summary>
    private static bool TryHandleCustomScheme(string url)
    {
        // 标准 Web 协议由 WebView 自行处理
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("file://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("about:", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // 自定义 scheme：通过 Android Intent 跳转到对应 App（QQ、微信等）
        try
        {
            var intent = new Intent(Intent.ActionView);
            intent.SetData(Android.Net.Uri.Parse(url));
            intent.AddFlags(ActivityFlags.NewTask);
            Android.App.Application.Context.StartActivity(intent);
            LogHelper.Info($"[浏览器] 已通过 Intent 打开自定义 scheme: {url}");
            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"[浏览器] 无法处理自定义 scheme {url}: {ex.Message}");
            // 如果系统没有能处理该 scheme 的 App（如未安装 QQ），返回 false
            // 让 WebView 尝试处理（虽然大概率会静默失败，但不影响其他逻辑）
            return false;
        }
    }

    /// <summary>
    /// 自定义 scheme 跳转（如 QQ 登录的 wtloginmqq://）后，
    /// Android WebView 常因导航被中途取消而变为空白页。
    /// 此方法在短暂延迟后重新加载上一个有效页面，防止黑屏。
    /// </summary>
    private async Task RestoreAfterSchemeRedirectAsync()
    {
        _isRestoringFromScheme = true;
        try
        {
            // 短暂延迟，让 WebView 完成内部状态回滚
            await Task.Delay(400);

            if (_webView is null || !_isReady) return;

            // 优先尝试直接刷新当前页面（保持页面状态，不产生额外历史记录）
            if (!string.IsNullOrEmpty(_currentUrl) &&
                (_currentUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                 _currentUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            {
                LogHelper.Info($"[浏览器] scheme跳转后刷新当前页面 -> {_currentUrl}");
                _webView.Reload();
            }
            else if (!string.IsNullOrEmpty(_lastKnownGoodUrl))
            {
                // 当前 URL 不可用（如 about:blank），恢复到上一个有效页面
                LogHelper.Info($"[浏览器] scheme跳转后恢复到上一个页面 -> {_lastKnownGoodUrl}");
                DoNavigate(_lastKnownGoodUrl);
            }
            else
            {
                LogHelper.Warn("[浏览器] scheme跳转后无可恢复的有效页面");
            }
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"[浏览器] scheme跳转后恢复页面失败: {ex.Message}");
        }
        finally
        {
            _isRestoringFromScheme = false;
        }
    }
#endif

    /// <summary>
    /// 当应用从后台恢复时调用（如用户从 QQ 授权返回）。
    /// 刷新当前页面，使登录页面能够检测到最新的登录状态。
    /// </summary>
    public void HandleAppResumed()
    {
        if (_webView is null || !_isReady) return;

        LogHelper.Info("[浏览器] 应用从后台恢复，刷新页面以检测登录状态");
        Reload();
    }
}
