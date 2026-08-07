using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.Android;

/// <summary>
/// Android 平台原生 WebView 浏览器控件 — 基于 Android.Webkit.WebView。
/// 通过 Activity.AddContentView 将原生 WebView 嵌入到 Avalonia 界面中。
/// </summary>
public class WebViewBrowserControl : Control, IBrowserProvider, IDisposable
{
    private WebView? _webView;
    private Activity? _activity;
    private bool _isInitialized;
    private bool _disposed;
    private string _currentUrl = string.Empty;
    private FrameLayout? _container;
    private FrameLayout.LayoutParams? _layoutParams;

    public bool IsReady => _isInitialized;

    public bool CanGoBack => _webView?.CanGoBack() ?? false;
    public bool CanGoForward => _webView?.CanGoForward() ?? false;
    public bool IsLoading => _webView != null && _webView.Progress < 100;

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
            _activity = GetActivity();
            if (_activity == null)
            {
                LogHelper.Error("[Android WebView] 无法获取 Activity");
                return;
            }

            _activity.RunOnUiThread(() =>
            {
                CreateWebViewOnUiThread();
            });
        }
        catch (Exception ex)
        {
            LogHelper.Error("[Android WebView] 初始化失败", ex);
        }
    }

    private void CreateWebViewOnUiThread()
    {
        try
        {
            if (_activity == null || _disposed) return;

            // 创建容器 FrameLayout 用于控制 WebView 布局
            _container = new FrameLayout(_activity);

            var density = _activity.Resources.DisplayMetrics?.Density ?? 1.5f;
            int widthPx = Bounds.Width > 0 ? (int)(Bounds.Width * density) : ViewGroup.LayoutParams.MatchParent;
            int heightPx = Bounds.Height > 0 ? (int)(Bounds.Height * density) : ViewGroup.LayoutParams.MatchParent;

            _layoutParams = new FrameLayout.LayoutParams(widthPx, heightPx);

            _activity.AddContentView(_container, _layoutParams);

            // 创建 WebView
            _webView = new WebView(_activity);

            // 配置 WebView 设置 — 确保网页能正常加载和交互
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

            // 设置 WebViewClient 处理导航事件
            _webView.SetWebViewClient(new BrowserWebViewClient(this));

            // 设置 WebChromeClient 处理 JS 弹窗、进度等
            _webView.SetWebChromeClient(new BrowserWebChromeClient(this));

            // 添加到容器
            _container.AddView(_webView, new FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MatchParent,
                FrameLayout.LayoutParams.MatchParent));

            // 监听 Avalonia 尺寸变化
            SizeChanged += OnAvaloniaSizeChanged;

            _isInitialized = true;

            LogHelper.Info("[Android WebView] 初始化完成");
            ReadyChanged?.Invoke(this, EventArgs.Empty);

            // 如果有 URL，立即导航
            if (!string.IsNullOrEmpty(_currentUrl))
            {
                _webView.LoadUrl(_currentUrl);
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error("[Android WebView] UI 线程创建 WebView 失败", ex);
        }
    }

    private void OnAvaloniaSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (_activity == null || _container == null || _layoutParams == null) return;

        try
        {
            _activity.RunOnUiThread(() =>
            {
                if (_container == null || _layoutParams == null) return;

                var density = _activity.Resources.DisplayMetrics?.Density ?? 1.5f;
                int widthPx = Bounds.Width > 0 ? (int)(Bounds.Width * density) : ViewGroup.LayoutParams.MatchParent;
                int heightPx = Bounds.Height > 0 ? (int)(Bounds.Height * density) : ViewGroup.LayoutParams.MatchParent;

                _layoutParams.Width = widthPx;
                _layoutParams.Height = heightPx;
                _container.LayoutParameters = _layoutParams;
            });
        }
        catch (Exception ex)
        {
            LogHelper.Error("[Android WebView] 更新布局失败", ex);
        }
    }

    private Activity? GetActivity()
    {
        // 优先使用 MainActivity 注册的 Activity
        if (MainActivity.CurrentActivity != null)
        {
            return MainActivity.CurrentActivity;
        }

        // 备选：通过平台句柄获取
        if (VisualRoot is TopLevel topLevel)
        {
            var handle = topLevel.TryGetPlatformHandle();
            if (handle != null)
            {
                return GetActivityFromHandle(handle);
            }
        }

        // 最后备选：通过 ActivityManager 查找
        return FindCurrentActivity();
    }

    private static Activity? GetActivityFromHandle(IPlatformHandle handle)
    {
        // 尝试通过反射获取 Activity (AndroidPlatformHandle.Activity)
        var handleType = handle.GetType();
        var activityProperty = handleType.GetProperty("Activity");
        if (activityProperty != null)
        {
            return activityProperty.GetValue(handle) as Activity;
        }

        return null;
    }

    private static Activity? FindCurrentActivity()
    {
        // 此方法作为备选方案，优先使用 MainActivity.CurrentActivity
        return null;
    }

    #region IBrowserProvider Implementation

    public void Navigate(string url)
    {
        _currentUrl = url;

        if (!_isInitialized || _webView == null || _activity == null) return;

        if (!url.StartsWith("http://") && !url.StartsWith("https://") && !url.StartsWith("file://"))
        {
            url = "https://" + url;
        }

        _activity.RunOnUiThread(() =>
        {
            try
            {
                _webView?.LoadUrl(url);
                LogHelper.Debug($"[Android WebView] 导航: {url}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[Android WebView] 导航失败: {url}", ex);
            }
        });
    }

    public void Reload()
    {
        if (_webView == null || _activity == null) return;

        _activity.RunOnUiThread(() =>
        {
            try
            {
                _webView?.Reload();
                LogHelper.Debug("[Android WebView] 刷新");
            }
            catch (Exception ex)
            {
                LogHelper.Error("[Android WebView] 刷新失败", ex);
            }
        });
    }

    public void GoBack()
    {
        if (_webView == null || _activity == null) return;

        _activity.RunOnUiThread(() =>
        {
            try
            {
                if (_webView?.CanGoBack() == true)
                {
                    _webView.GoBack();
                    LogHelper.Debug("[Android WebView] 后退");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error("[Android WebView] 后退失败", ex);
            }
        });
    }

    public void GoForward()
    {
        if (_webView == null || _activity == null) return;

        _activity.RunOnUiThread(() =>
        {
            try
            {
                if (_webView?.CanGoForward() == true)
                {
                    _webView.GoForward();
                    LogHelper.Debug("[Android WebView] 前进");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error("[Android WebView] 前进失败", ex);
            }
        });
    }

    public string GetCurrentUrl() => _webView?.Url ?? _currentUrl;

    public string GetTitle() => _webView?.Title ?? string.Empty;

    public Task<string> ExecuteScriptAsync(string script)
    {
        // Android WebView JavaScript 执行需要通过 WebChromeClient 实现
        // 这里提供基础实现，后续可扩展
        return Task.FromResult(string.Empty);
    }

    public void Initialize()
    {
        if (!_isInitialized && _activity != null)
        {
            InitializeWebView();
        }
    }

    #endregion

    #region WebViewClient

    private class BrowserWebViewClient : WebViewClient
    {
        private readonly WebViewBrowserControl _control;

        public BrowserWebViewClient(WebViewBrowserControl control)
        {
            _control = control;
        }

        public override bool ShouldOverrideUrlLoading(WebView? view, IWebResourceRequest? request)
        {
            if (request?.Url == null) return false;

            var url = request.Url.ToString();

            // 处理自定义 scheme
            if (url.StartsWith("app://") || url.StartsWith("scassistant://"))
            {
                return true;
            }

            return false;
        }

        public override void OnPageStarted(WebView? view, string? url, global::Android.Graphics.Bitmap? favicon)
        {
            base.OnPageStarted(view, url, favicon);
            _control._currentUrl = url ?? string.Empty;
            _control.AddressChanged?.Invoke(_control, url ?? string.Empty);
            _control.LoadingStateChanged?.Invoke(_control, true);
        }

        public override void OnPageFinished(WebView? view, string? url)
        {
            base.OnPageFinished(view, url);
            _control.LoadingStateChanged?.Invoke(_control, false);
            _control.NavigationHistoryChanged?.Invoke(_control, EventArgs.Empty);
        }

        public override void OnReceivedError(WebView? view, global::Android.Webkit.ClientError errorCode, string? description, string? failingUrl)
        {
            base.OnReceivedError(view, errorCode, description, failingUrl);
            LogHelper.Warn($"[Android WebView] 错误: {errorCode} - {description}");
        }

        public override WebResourceResponse? ShouldInterceptRequest(WebView? view, IWebResourceRequest? request)
        {
            return null;
        }
    }

    private class BrowserWebChromeClient : WebChromeClient
    {
        private readonly WebViewBrowserControl _control;

        public BrowserWebChromeClient(WebViewBrowserControl control)
        {
            _control = control;
        }

        public override void OnReceivedTitle(WebView? view, string? title)
        {
            base.OnReceivedTitle(view, title);
            _control.TitleChanged?.Invoke(_control, title ?? string.Empty);
        }

        public override void OnProgressChanged(WebView? view, int newProgress)
        {
            base.OnProgressChanged(view, newProgress);
        }

        public override bool OnConsoleMessage(ConsoleMessage? consoleMessage)
        {
            if (consoleMessage != null)
            {
                LogHelper.Debug($"[Android WebView JS] {consoleMessage.Message()}");
            }
            return base.OnConsoleMessage(consoleMessage);
        }
    }

    #endregion

    #region Android WebView Helper Types

    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            if (_webView != null)
            {
                _webView.StopLoading();
                _webView.ClearHistory();
                _webView.ClearCache(true);

                if (_activity != null)
                {
                    _activity.RunOnUiThread(() =>
                    {
                        try
                        {
                            _webView.Visibility = ViewStates.Gone;
                            _webView.RemoveAllViews();
                            _webView.Destroy();
                        }
                        catch { }
                    });
                }
                else
                {
                    _webView.RemoveAllViews();
                    _webView.Destroy();
                }

                _webView = null;
            }

            if (_container != null && _activity != null)
            {
                _activity.RunOnUiThread(() =>
                {
                    try
                    {
                        _container.Visibility = ViewStates.Gone;
                        _container.RemoveAllViews();
                        _container = null;
                    }
                    catch { }
                });
            }

            SizeChanged -= OnAvaloniaSizeChanged;
        }
        catch (Exception ex)
        {
            LogHelper.Error("[Android WebView] 释放资源时出错", ex);
        }
    }
}