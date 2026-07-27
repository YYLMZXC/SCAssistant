using System;
using Avalonia.Controls;
using Avalonia.Platform;
using CoreGraphics;
using Foundation;
using SCAssistant.AvaloniaApp.Services;
using WebKit;

namespace SCAssistant.AvaloniaApp.iOS.Services;

/// <summary>
/// iOS 原生 WKWebView 浏览器实现，通过 NativeControlHost 嵌入 Avalonia UI。
/// </summary>
public class iOSBrowserProvider : NativeControlHost, IBrowserProvider
{
    private WKWebView? _webView;
    private string? _pendingUrl;
    private bool _isLoading;

    public event EventHandler<string>? AddressChanged;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<bool>? LoadingStateChanged;
    public event EventHandler? BrowserCrashed;

    public string CurrentUrl => _webView?.Url?.AbsoluteString ?? string.Empty;
    public string CurrentTitle => _webView?.Title ?? string.Empty;
    public bool IsLoading => _isLoading;

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        var config = new WKWebViewConfiguration();
        var prefs = new WKPreferences { JavaScriptEnabled = true };
        config.Preferences = prefs;

        _webView = new WKWebView(CGRect.Empty, config);
        _webView.NavigationDelegate = new CustomNavigationDelegate(this);

        // 异步导航：WKWebView 创建完成后再加载已缓存的 URL
        if (_pendingUrl != null && Uri.TryCreate(_pendingUrl, UriKind.Absolute, out var pendingUri))
        {
            _pendingUrl = null;
            var request = new NSUrlRequest(new NSUrl(pendingUri.AbsoluteUri));
            _webView.LoadRequest(request);
        }

        return new PlatformHandle(_webView.Handle, "iOSWebView");
    }

    public Control CreateBrowserControl() => this;

    public void Initialize(string startUrl) => Navigate(startUrl);

    public void Navigate(string url)
    {
        if (_webView != null && Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var request = new NSUrlRequest(new NSUrl(uri.AbsoluteUri));
            _webView.LoadRequest(request);
        }
        else
        {
            _pendingUrl = url;
        }
    }

    public void Reload()
    {
        _webView?.Reload();
    }

    private sealed class CustomNavigationDelegate : WKNavigationDelegate
    {
        private readonly iOSBrowserProvider _provider;

        public CustomNavigationDelegate(iOSBrowserProvider provider)
            => _provider = provider;

        public override void DidStartProvisionalNavigation(WKWebView webView, WKNavigation navigation)
        {
            _provider._isLoading = true;
            _provider.LoadingStateChanged?.Invoke(_provider, true);
            var url = webView.Url?.AbsoluteString;
            if (url != null)
                _provider.AddressChanged?.Invoke(_provider, url);
        }

        public override void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
        {
            _provider._isLoading = false;
            _provider.LoadingStateChanged?.Invoke(_provider, false);
            var url = webView.Url?.AbsoluteString;
            if (url != null)
                _provider.AddressChanged?.Invoke(_provider, url);
            _provider.TitleChanged?.Invoke(_provider, webView.Title ?? string.Empty);
        }
    }
}
