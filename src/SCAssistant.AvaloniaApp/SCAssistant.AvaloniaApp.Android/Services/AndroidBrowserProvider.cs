using System;
using Android.Webkit;
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
    private bool _isLoading;

    public event EventHandler<string>? AddressChanged;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<bool>? LoadingStateChanged;
    public event EventHandler? BrowserCrashed;

    public string CurrentUrl => _webView?.Url ?? string.Empty;
    public string CurrentTitle => _webView?.Title ?? string.Empty;
    public bool IsLoading => _isLoading;

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

        _webView.SetWebViewClient(new CustomWebViewClient(this));
        _webView.SetWebChromeClient(new CustomWebChromeClient(this));

        return new PlatformHandle(_webView.Handle, "AndroidWebView");
    }

    public Control CreateBrowserControl() => this;

    public void Initialize(string startUrl) => Navigate(startUrl);

    public void Navigate(string url)
    {
        if (_webView != null)
            Dispatcher.UIThread.Post(() => _webView.LoadUrl(url));
    }

    public void Reload()
    {
        if (_webView != null)
            Dispatcher.UIThread.Post(() => _webView.Reload());
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

        public override void OnReceivedTitle(WebView? view, string? title)
        {
            base.OnReceivedTitle(view, title);
            if (title != null)
                _provider.TitleChanged?.Invoke(_provider, title);
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
