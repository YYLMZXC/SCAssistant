using System;
using Android.Webkit;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.Android.Services;

/// <summary>
/// Android 原生 WebView 浏览器实现，通过 NativeControlHost 嵌入 Avalonia UI。
/// </summary>
public class AndroidBrowserProvider : NativeControlHost, IBrowserProvider
{
    private WebView? _webView;
    private string? _pendingUrl;
    private bool _isLoading;

    public event EventHandler<string>? AddressChanged;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<bool>? LoadingStateChanged;
    public event EventHandler? BrowserCrashed;

    public string CurrentUrl => _webView?.Url ?? string.Empty;
    public string CurrentTitle => _webView?.Title ?? string.Empty;
    public bool IsLoading => _isLoading;

    /// <summary>
    /// 确保 NativeControlHost 填充父容器分配的空间，避免 WebView 尺寸为 0。
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        return availableSize;
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        var context = global::Android.App.Application.Context;
        _webView = new WebView(context);

        var settings = _webView.Settings;
        settings.JavaScriptEnabled = true;
        settings.DomStorageEnabled = true;
        settings.SetSupportZoom(true);
        settings.BuiltInZoomControls = true;
        settings.DisplayZoomControls = false;
        settings.LoadWithOverviewMode = true;
        settings.UseWideViewPort = true;
        settings.AllowFileAccess = true;
        settings.AllowContentAccess = true;
        // 允许混合内容（HTTP 资源嵌入 HTTPS 页面）
        settings.MixedContentMode = MixedContentHandling.AlwaysAllow;

        // 启用 WebView 调试 (chrome://inspect 远程调试)
        WebView.SetWebContentsDebuggingEnabled(true);

        _webView.SetWebViewClient(new CustomWebViewClient(this));
        _webView.SetWebChromeClient(new CustomWebChromeClient(this));

        // WebView 创建完成后加载已缓存的 URL
        if (_pendingUrl != null)
        {
            var url = _pendingUrl;
            _pendingUrl = null;
            Dispatcher.UIThread.Post(() => _webView.LoadUrl(url));
        }

        return new PlatformHandle(_webView.Handle, "AndroidWebView");
    }

    public Control CreateBrowserControl() => this;

    public void Initialize(string startUrl) => Navigate(startUrl);

    public void Navigate(string url)
    {
        if (_webView != null)
            Dispatcher.UIThread.Post(() => _webView.LoadUrl(url));
        else
            _pendingUrl = url;
    }

    public void Reload()
    {
        if (_webView != null)
            Dispatcher.UIThread.Post(() => _webView.Reload());
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        if (_webView != null)
        {
            _webView.RemoveAllViews();
            _webView.Destroy();
            _webView = null;
        }
        base.DestroyNativeControlCore(control);
    }

    private sealed class CustomWebViewClient : WebViewClient
    {
        private readonly AndroidBrowserProvider _provider;

        public CustomWebViewClient(AndroidBrowserProvider provider)
            => _provider = provider;

        public override void OnPageStarted(WebView? view, string? url, global::Android.Graphics.Bitmap? favicon)
        {
            base.OnPageStarted(view, url, favicon);
            _provider._isLoading = true;
            _provider.LoadingStateChanged?.Invoke(_provider, true);
            if (url != null)
                _provider.AddressChanged?.Invoke(_provider, url);
        }

        public override void OnPageFinished(WebView? view, string? url)
        {
            base.OnPageFinished(view, url);
            _provider._isLoading = false;
            _provider.LoadingStateChanged?.Invoke(_provider, false);
            if (url != null)
                _provider.AddressChanged?.Invoke(_provider, url);
        }

        public override void OnReceivedError(WebView? view, IWebResourceRequest? request, WebResourceError? error)
        {
            base.OnReceivedError(view, request, error);
            _provider._isLoading = false;
            _provider.LoadingStateChanged?.Invoke(_provider, false);
            _provider.BrowserCrashed?.Invoke(_provider, EventArgs.Empty);
        }

        /// <summary>
        /// 接受所有 SSL 证书（仅测试环境，生产环境应验证证书链）。
        /// </summary>
        public override void OnReceivedSslError(WebView? view, SslErrorHandler? handler, SslError? error)
        {
            handler?.Proceed();
        }
    }

    private sealed class CustomWebChromeClient : WebChromeClient
    {
        private readonly AndroidBrowserProvider _provider;

        public CustomWebChromeClient(AndroidBrowserProvider provider)
            => _provider = provider;

        public override void OnReceivedTitle(WebView? view, string? title)
        {
            base.OnReceivedTitle(view, title);
            if (title != null)
                _provider.TitleChanged?.Invoke(_provider, title);
        }
    }
}
