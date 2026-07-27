using System;
using Avalonia.Controls;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.Desktop.Services;

/// <summary>
/// 基于 Avalonia.Controls.WebView 官方控件的跨平台浏览器实现。
/// NativeWebView 底层自动选用各平台原生引擎：
///   Windows → Edge WebView2
///   macOS → WKWebView
///   Linux → WPE WebKit
///   Android / iOS → 平台 WebView
/// </summary>
public sealed class WebView2BrowserProvider : IBrowserProvider
{
    private NativeWebView? _webView;
    private string? _pendingNavigateUrl;
    private bool _isLoading;

    public event EventHandler<string>? AddressChanged;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<bool>? LoadingStateChanged;
    public event EventHandler? BrowserCrashed;

    public string CurrentUrl => _webView?.Source?.ToString() ?? string.Empty;
    public string CurrentTitle => string.Empty;
    public bool IsLoading => _isLoading;

    public Control CreateBrowserControl()
    {
        _webView = new NativeWebView();

        _webView.NavigationStarted += (_, _) =>
        {
            _isLoading = true;
            LoadingStateChanged?.Invoke(this, true);
        };

        _webView.NavigationCompleted += (_, e) =>
        {
            _isLoading = false;
            LoadingStateChanged?.Invoke(this, false);
            if (e.IsSuccess)
            {
                var url = _webView.Source?.ToString();
                if (url != null)
                    AddressChanged?.Invoke(this, url);
            }
        };

        if (_pendingNavigateUrl != null)
        {
            _webView.Source = new Uri(_pendingNavigateUrl);
            _pendingNavigateUrl = null;
        }

        return _webView;
    }

    public void Initialize(string startUrl)
    {
        if (_webView != null)
        {
            _webView.Source = new Uri(startUrl);
        }
        else
        {
            _pendingNavigateUrl = startUrl;
        }
    }

    public void Navigate(string url)
    {
        if (_webView != null)
        {
            _webView.Source = new Uri(url);
        }
        else
        {
            _pendingNavigateUrl = url;
        }
    }

    public void Reload()
    {
        if (_webView == null) return;

        var current = _webView.Source;
        _webView.Source = null;
        _webView.Source = current;
    }
}
