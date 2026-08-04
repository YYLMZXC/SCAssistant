using System;
using System.Diagnostics;
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
/// 注意：Uno Platform 6.x 的 WebView2 不暴露 CoreWebView2InitializationCompleted 事件，
/// 仅提供 NavigationStarting / NavigationCompleted / Source / CoreWebView2 / Reload 等基础 API。
/// 使用 Loaded 事件作为控件就绪信号，确保 WebView2 加入可视化树后再执行导航。
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
        _webView = new WebView2();
        _isReady = false;

        // 使用 Loaded 事件确保 WebView2 已加入可视化树、底层原生控件已创建后再导航
        _webView.Loaded += OnWebViewLoaded;

        _webView.NavigationStarting += (_, args) =>
        {
            _isLoading = true;
            _currentUrl = args.Uri?.ToString() ?? string.Empty;
            AddressChanged?.Invoke(this, _currentUrl);
            LoadingStateChanged?.Invoke(this, true);
        };

        _webView.NavigationCompleted += (sender, _) =>
        {
            _isLoading = false;
            try
            {
                if (sender.CoreWebView2 is not null)
                {
                    _currentTitle = sender.CoreWebView2.DocumentTitle ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrowserProvider] Failed to read document title: {ex.Message}");
            }
            TitleChanged?.Invoke(this, _currentTitle);
            LoadingStateChanged?.Invoke(this, false);
        };

        return _webView;
    }

    /// <summary>
    /// WebView2 加入可视化树后触发。在此之前导航可能因底层原生控件未创建而失败/空白。
    /// </summary>
    private void OnWebViewLoaded(object sender, RoutedEventArgs e)
    {
        if (_webView is null) return;
        _webView.Loaded -= OnWebViewLoaded;

        Debug.WriteLine("[BrowserProvider] WebView2 Loaded - ready to navigate");
        _isReady = true;

        if (_pendingNavigateUrl is not null)
        {
            var url = _pendingNavigateUrl;
            _pendingNavigateUrl = null;
            DoNavigate(url);
        }
    }

    public void Initialize(string startUrl)
    {
        _pendingNavigateUrl = startUrl;
        if (_webView is not null && _isReady)
        {
            _pendingNavigateUrl = null;
            DoNavigate(startUrl);
        }
    }

    public void Navigate(string url)
    {
        _currentUrl = url;
        if (_webView is not null && _isReady)
        {
            DoNavigate(url);
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

    private void DoNavigate(string url)
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
                // Uno Platform 桌面端 CoreWebView2 可能为 null，使用 Source 属性
                Debug.WriteLine($"[BrowserProvider] Navigating via Source: {url}");
                _webView.Source = new Uri(url);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BrowserProvider] Navigation failed: {ex.Message}");
            SystemBrowserProvider.OpenUrl(url);
        }
    }
}
