using System;
using Avalonia.Controls;
using Avalonia.Threading;
using CefSharp.Avalonia;
using AvaloniaApplication1.Services;

namespace AvaloniaApplication1.Desktop.Services;

public class CefBrowserProvider : IBrowserProvider
{
    private WebView? _browser;

    public event EventHandler<string>? AddressChanged;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<bool>? LoadingStateChanged;
    public event EventHandler? BrowserCrashed;

    public string CurrentUrl => _browser?.Url ?? string.Empty;
    public string CurrentTitle => _browser?.Title ?? string.Empty;
    public bool IsLoading => _browser?.IsLoading ?? false;

    public Control CreateBrowserControl()
    {
        var webView = new WebView();
        webView.CefSettings.NoSandbox = true;
        webView.CefSettings.Locale = "zh-CN";

        webView.AddressChanged += url =>
            Dispatcher.UIThread.Post(() => AddressChanged?.Invoke(this, url));

        webView.TitleChanged += title =>
            Dispatcher.UIThread.Post(() => TitleChanged?.Invoke(this, title));

        webView.LoadingStateChanged += loading =>
            Dispatcher.UIThread.Post(() => LoadingStateChanged?.Invoke(this, loading));

        webView.BrowserCrashed += () =>
            Dispatcher.UIThread.Post(() => BrowserCrashed?.Invoke(this, EventArgs.Empty));

        _browser = webView;
        return webView;
    }

    public void Initialize(string startUrl)
    {
        if (_browser != null)
            _ = _browser.NavigateAsync(startUrl);
    }

    public void Navigate(string url)
    {
        if (_browser != null)
            _ = _browser.NavigateAsync(url);
    }

    public void Reload()
    {
        if (_browser != null)
            _ = _browser.ReloadAsync();
    }
}
