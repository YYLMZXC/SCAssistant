using System;
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
        LogHelper.Info("[Browser] CreateBrowserControl - creating WebView2");
        _webView = new WebView2();
        _isReady = false;

        _webView.Loaded += OnWebViewLoaded;

        _webView.NavigationStarting += (_, args) =>
        {
            _isLoading = true;
            _currentUrl = args.Uri?.ToString() ?? string.Empty;
            LogHelper.Info($"[Browser] NavigationStarting -> {_currentUrl}");
            AddressChanged?.Invoke(this, _currentUrl);
            LoadingStateChanged?.Invoke(this, true);
        };

        _webView.NavigationCompleted += (sender, args) =>
        {
            _isLoading = false;
            LogHelper.Info($"[Browser] NavigationCompleted success={args.IsSuccess} err={args.WebErrorStatus}");

            try
            {
                if (sender.CoreWebView2 is not null)
                {
                    _currentTitle = sender.CoreWebView2.DocumentTitle ?? string.Empty;
                }
                else
                {
                    LogHelper.Info("[Browser] NavigationCompleted - CoreWebView2 is null (Skia/fallback mode)");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error("[Browser] Failed to read document title", ex);
            }
            TitleChanged?.Invoke(this, _currentTitle);
            LoadingStateChanged?.Invoke(this, false);
        };

        LogHelper.Info($"[Browser] CreateBrowserControl done, _isReady={_isReady}");
        return _webView;
    }

    private void OnWebViewLoaded(object sender, RoutedEventArgs e)
    {
        if (_webView is null) return;
        _webView.Loaded -= OnWebViewLoaded;

        LogHelper.Info("[Browser] WebView2.Loaded fired - control in visual tree");
        _isReady = true;

        if (_pendingNavigateUrl is not null)
        {
            var url = _pendingNavigateUrl;
            _pendingNavigateUrl = null;
            LogHelper.Info($"[Browser] Executing pending navigation -> {url}");
            DoNavigate(url);
        }
        else
        {
            LogHelper.Info("[Browser] Loaded but no pending navigation");
        }
    }

    public void Initialize(string startUrl)
    {
        LogHelper.Info($"[Browser] Initialize(startUrl={startUrl}) _isReady={_isReady}");
        _pendingNavigateUrl = startUrl;
        if (_webView is not null && _isReady)
        {
            _pendingNavigateUrl = null;
            DoNavigate(startUrl);
        }
        else
        {
            LogHelper.Info("[Browser] Initialize deferred - waiting for Loaded event");
        }
    }

    public void Navigate(string url)
    {
        LogHelper.Info($"[Browser] Navigate(url={url}) _isReady={_isReady}");
        _currentUrl = url;
        if (_webView is not null && _isReady)
        {
            DoNavigate(url);
        }
        else
        {
            LogHelper.Info("[Browser] Navigate deferred - waiting for Loaded event");
            _pendingNavigateUrl = url;
        }
    }

    public void Reload()
    {
        LogHelper.Info("[Browser] Reload requested");
        _webView?.Reload();
    }

    private void DoNavigate(string url)
    {
        if (_webView is null) return;

        LogHelper.Info($"[Browser] DoNavigate -> {url}");

        try
        {
            if (_webView.CoreWebView2 is not null)
            {
                LogHelper.Info("[Browser] Using CoreWebView2.Navigate()");
                _webView.CoreWebView2.Navigate(url);
            }
            else
            {
                LogHelper.Info("[Browser] CoreWebView2 is null, using Source property");
                _webView.Source = new Uri(url);
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[Browser] Navigation failed for {url}, falling back to system browser", ex);
            SystemBrowserProvider.OpenUrl(url);
        }
    }
}
