namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 浏览器提供者适配器 - 封装平台差异的WebView实现
/// 在Avalonia中，WebView控件直接嵌入UI，此Provider作为状态管理和操作封装
/// </summary>
public class BrowserProvider : IBrowserProvider
{
    private string _currentUrl = string.Empty;
    private string _currentTitle = string.Empty;
    private bool _isLoading;
    private readonly Stack<string> _backStack = new();
    private readonly Stack<string> _forwardStack = new();

    // 这些属性由 View 层的 WebView 事件回调来更新
    public bool CanGoBack => _backStack.Count > 0;
    public bool CanGoForward => _forwardStack.Count > 0;
    public bool IsLoading => _isLoading;

    public event EventHandler<string>? NavigationStarted;
    public event EventHandler<string>? NavigationCompleted;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<bool>? LoadingStateChanged;

    public BrowserProvider()
    {
    }

    /// <summary>
    /// 由外部WebView控件调用来更新状态
    /// </summary>
    public void OnNavigating(string url)
    {
        if (!string.IsNullOrEmpty(_currentUrl) && _currentUrl != url)
        {
            _backStack.Push(_currentUrl);
            _forwardStack.Clear();
        }
        _currentUrl = url;
        _isLoading = true;
        NavigationStarted?.Invoke(this, url);
        LoadingStateChanged?.Invoke(this, true);
    }

    /// <summary>
    /// 由外部WebView控件调用来更新完成状态
    /// </summary>
    public void OnNavigationCompleted(string url)
    {
        _currentUrl = url;
        _isLoading = false;
        NavigationCompleted?.Invoke(this, url);
        LoadingStateChanged?.Invoke(this, false);
    }

    /// <summary>
    /// 由外部WebView控件调用来更新标题
    /// </summary>
    public void OnTitleChanged(string title)
    {
        _currentTitle = title;
        TitleChanged?.Invoke(this, title);
    }

    public void Navigate(string url)
    {
        // URL归一化
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // 可能是搜索查询，使用默认搜索引擎
            if (!url.Contains('.') && !url.Contains('/') && !url.Contains(' '))
            {
                url = "https://www.google.com/search?q=" + Uri.EscapeDataString(url);
            }
            else if (url.Contains(' ') || !url.Contains('.'))
            {
                url = "https://www.google.com/search?q=" + Uri.EscapeDataString(url);
            }
            else
            {
                url = "https://" + url;
            }
        }

        NavigationRequested?.Invoke(this, url);
    }

    public void GoBack()
    {
        if (_backStack.Count > 0)
        {
            _forwardStack.Push(_currentUrl);
            var url = _backStack.Pop();
            NavigationRequested?.Invoke(this, url);
        }
    }

    public void GoForward()
    {
        if (_forwardStack.Count > 0)
        {
            _backStack.Push(_currentUrl);
            var url = _forwardStack.Pop();
            NavigationRequested?.Invoke(this, url);
        }
    }

    public void Refresh()
    {
        RefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Stop()
    {
        StopRequested?.Invoke(this, EventArgs.Empty);
    }

    public async Task<string> ExecuteScriptAsync(string script)
    {
        // 实际执行由View层处理
        var tcs = new TaskCompletionSource<string>();
        var handler = new EventHandler<string>((s, result) => tcs.TrySetResult(result));

        ExecuteScriptRequested += handler;
        ExecuteScriptRequested?.Invoke(this, script);

        try
        {
            return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch
        {
            return string.Empty;
        }
        finally
        {
            ExecuteScriptRequested -= handler;
        }
    }

    public string GetCurrentUrl() => _currentUrl;

    public string GetTitle() => _currentTitle;

    public async Task<string> GetCookiesAsync(string domain)
    {
        var tcs = new TaskCompletionSource<string>();
        var handler = new EventHandler<GetCookiesEventArgs>((s, e) =>
        {
            if (e.Domain == domain)
                tcs.TrySetResult(e.Cookies);
        });

        GetCookiesRequested += handler;
        GetCookiesRequested?.Invoke(this, new GetCookiesEventArgs(domain));

        try
        {
            return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch
        {
            return string.Empty;
        }
        finally
        {
            GetCookiesRequested -= handler;
        }
    }

    public Task ClearCookiesAsync()
    {
        ClearCookiesRequested?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public object? GetNativeControl() => null; // 在Avalonia中WebView直接嵌入XAML

    /// <summary>
    /// 导航请求事件 - 由View层订阅来处理实际的WebView导航
    /// </summary>
    public event EventHandler<string>? NavigationRequested;

    /// <summary>
    /// 刷新请求事件
    /// </summary>
    public event EventHandler? RefreshRequested;

    /// <summary>
    /// 停止请求事件
    /// </summary>
    public event EventHandler? StopRequested;

    /// <summary>
    /// 执行脚本请求事件
    /// </summary>
    public event EventHandler<string>? ExecuteScriptRequested;

    /// <summary>
    /// 获取Cookie请求事件
    /// </summary>
    public event EventHandler<GetCookiesEventArgs>? GetCookiesRequested;

    /// <summary>
    /// 清除Cookie请求事件
    /// </summary>
    public event EventHandler? ClearCookiesRequested;
}

/// <summary>
/// GetCookies事件参数
/// </summary>
public class GetCookiesEventArgs : EventArgs
{
    public string Domain { get; }
    public string Cookies { get; set; } = string.Empty;

    public GetCookiesEventArgs(string domain)
    {
        Domain = domain;
    }
}
