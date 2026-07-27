using System;
using Avalonia.Controls;
using Avalonia.Threading;
using SCAssistant.AvaloniaApp.Services;
using Xilium.CefGlue.Avalonia;
using Xilium.CefGlue.Common;
using Xilium.CefGlue.Common.Events;

namespace SCAssistant.AvaloniaApp.Desktop.Services;

/// <summary>
/// 基于 CefGlue.Next (OutSystems CefGlue) 的跨平台浏览器实现。
/// 使用 CefGlue.Avalonia 提供的 AvaloniaCefBrowser 控件，
/// 底层为 Chromium Embedded Framework (CEF)，
/// 支持 Windows / macOS / Linux 桌面平台。
/// </summary>
public sealed class CefGlueBrowserProvider : IBrowserProvider
{
    private AvaloniaCefBrowser? _browser;
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
        _browser = new AvaloniaCefBrowser();

        _browser.LoadStart += OnBrowserLoadStart;
        _browser.TitleChanged += OnBrowserTitleChanged;

        // 处理待导航 URL
        if (_pendingNavigateUrl != null)
        {
            _browser.Address = _pendingNavigateUrl;
            _pendingNavigateUrl = null;
        }

        return _browser;
    }

    public void Initialize(string startUrl)
    {
        if (_browser != null)
        {
            _browser.Address = startUrl;
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
            _browser.Address = url;
        }
        else
        {
            _pendingNavigateUrl = url;
        }
    }

    public void Reload()
    {
        if (_browser == null) return;
        var current = _browser.Address;
        if (!string.IsNullOrEmpty(current))
        {
            _browser.Address = current;
        }
    }

    private void OnBrowserLoadStart(object? sender, LoadStartEventArgs e)
    {
        if (e.Frame.Browser.IsPopup || !e.Frame.IsMain)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            _isLoading = true;
            _currentUrl = e.Frame.Url;
            LoadingStateChanged?.Invoke(this, true);
            AddressChanged?.Invoke(this, e.Frame.Url);
        });
    }

    private void OnBrowserTitleChanged(object? sender, string title)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _isLoading = false;
            _currentTitle = title;
            TitleChanged?.Invoke(this, title);
            LoadingStateChanged?.Invoke(this, false);
        });
    }
}
