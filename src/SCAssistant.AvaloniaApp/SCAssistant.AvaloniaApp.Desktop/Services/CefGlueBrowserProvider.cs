using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Exclr8Cef.WebView;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.Desktop.Services;

/// <summary>
/// 基于 Exclr8Cef (exclr8cef) 的跨平台浏览器实现。
/// 使用 Exclr8Cef.WebView.WebView 控件，
/// 底层为 Chromium Embedded Framework (CEF)，
/// 支持 Windows / macOS / Linux 桌面平台。
/// </summary>
public sealed class Exclr8CefBrowserProvider : IBrowserProvider
{
    private WebView? _browser;
    private string? _pendingNavigateUrl;
    private bool _isLoading;
    private string _currentUrl = string.Empty;
    private string _currentTitle = string.Empty;

    public event EventHandler<string>? AddressChanged;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<bool>? LoadingStateChanged;
    public event EventHandler? BrowserCrashed;

    public string CurrentUrl => _currentUrl;
    public string CurrentTitle => _currentTitle;
    public bool IsLoading => _isLoading;

    public Control CreateBrowserControl()
    {
        _browser = new WebView();

        _browser.PropertyChanged += OnBrowserPropertyChanged;

        // 处理待导航 URL
        if (_pendingNavigateUrl != null)
        {
            _browser.NavigateToUrl(_pendingNavigateUrl);
            _pendingNavigateUrl = null;
        }

        return _browser;
    }

    public void Initialize(string startUrl)
    {
        if (_browser != null)
        {
            _browser.NavigateToUrl(startUrl);
        }
        else
        {
            _pendingNavigateUrl = startUrl;
        }
    }

    public void Navigate(string url)
    {
        _currentUrl = url;
        if (_browser != null)
        {
            _browser.NavigateToUrl(url);
        }
        else
        {
            _pendingNavigateUrl = url;
        }
    }

    public void Reload()
    {
        if (_browser?.Browser is { } b)
        {
            b.Reload();
        }
    }

    private void OnBrowserPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == WebView.UrlProperty)
        {
            var url = e.GetNewValue<string>() ?? string.Empty;
            Dispatcher.UIThread.Post(() =>
            {
                _currentUrl = url;
                AddressChanged?.Invoke(this, url);
            });
        }
        else if (e.Property == WebView.TitleProperty)
        {
            var title = e.GetNewValue<string>() ?? string.Empty;
            Dispatcher.UIThread.Post(() =>
            {
                _isLoading = false;
                _currentTitle = title;
                TitleChanged?.Invoke(this, title);
                LoadingStateChanged?.Invoke(this, false);
            });
        }
        else if (e.Property == WebView.IsLoadingProperty)
        {
            var loading = e.GetNewValue<bool>();
            Dispatcher.UIThread.Post(() =>
            {
                _isLoading = loading;
                LoadingStateChanged?.Invoke(this, loading);
            });
        }
    }
}
