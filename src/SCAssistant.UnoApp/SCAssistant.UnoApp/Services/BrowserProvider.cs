using System;
using Microsoft.UI.Xaml.Controls;

namespace SCAssistant.UnoApp.Services;

/// <summary>
/// 基于 Uno Platform 跨平台 WebView2 的浏览器实现。
/// Uno 将 WebView2 映射为各平台原生浏览器：
/// - Windows: Edge WebView2
/// - Android: Android WebView
/// - iOS: WKWebView
/// - Desktop Skia: 系统浏览器回退
/// </summary>
public class BrowserProvider : IBrowserProvider
{
    private WebView2? _webView;
    private string _currentUrl = string.Empty;
    private string _currentTitle = string.Empty;
    private bool _isLoading;
    private string? _pendingNavigateUrl;

    public event EventHandler<string>? AddressChanged;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<bool>? LoadingStateChanged;

    public string CurrentUrl => _currentUrl;
    public string CurrentTitle => _currentTitle;
    public bool IsLoading => _isLoading;

    public object CreateBrowserControl()
    {
        _webView = new WebView2();

        _webView.NavigationStarting += (sender, args) =>
        {
            _isLoading = true;
            _currentUrl = args.Uri?.ToString() ?? string.Empty;
            AddressChanged?.Invoke(this, _currentUrl);
            LoadingStateChanged?.Invoke(this, true);
        };

        _webView.NavigationCompleted += (sender, args) =>
        {
            _isLoading = false;
            _currentTitle = sender.CoreWebView2?.DocumentTitle ?? string.Empty;
            TitleChanged?.Invoke(this, _currentTitle);
            LoadingStateChanged?.Invoke(this, false);
        };

        // 处理待导航 URL
        if (_pendingNavigateUrl is not null)
        {
            NavigateToUrl(_pendingNavigateUrl);
            _pendingNavigateUrl = null;
        }

        return _webView;
    }

    public void Initialize(string startUrl)
    {
        _pendingNavigateUrl = startUrl;
        if (_webView is not null)
        {
            NavigateToUrl(startUrl);
            _pendingNavigateUrl = null;
        }
    }

    public void Navigate(string url)
    {
        _currentUrl = url;
        if (_webView is not null)
        {
            NavigateToUrl(url);
        }
        else
        {
            _pendingNavigateUrl = url;
        }
    }

    public void Reload()
    {
        _webView?.Reload();
    }

    private void NavigateToUrl(string url)
    {
        if (_webView is null) return;

        try
        {
            if (_webView.CoreWebView2 is not null)
            {
                _webView.CoreWebView2.Navigate(url);
            }
            else
            {
                _webView.Source = new Uri(url);
            }
        }
        catch (Exception)
        {
            // WebView2 不可用时，尝试用系统浏览器打开
            SystemBrowserProvider.OpenUrl(url);
        }
    }
}
