namespace SCAssistant.Maui.Services;

/// <summary>
/// BrowserProvider — 封装 MAUI WebView 的导航与控制。
/// 负责在 ViewModel 和 WebView 控件之间桥接。
/// </summary>
public class BrowserProvider : IBrowserProvider
{
    private string _currentUrl = string.Empty;
    private string _currentTitle = string.Empty;
    private bool _isReady;
    private bool _isLoading;

    public string GetCurrentUrl() => _currentUrl;
    public string GetCurrentTitle() => _currentTitle;
    public bool CanGoBack { get; private set; }
    public bool CanGoForward { get; private set; }
    public bool IsReady => _isReady;
    public bool IsLoading => _isLoading;

    public event EventHandler<string>? AddressChanged;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<bool>? LoadingStateChanged;
    public event EventHandler? NavigationHistoryChanged;
    public event EventHandler<string>? DownloadRequested;

    /// <summary>
    /// 关联的 MAUI WebView 控件。
    /// View 层负责创建 WebView 并注入。
    /// </summary>
    private Microsoft.Maui.Controls.WebView? _webView;

    public void SetWebView(Microsoft.Maui.Controls.WebView webView)
    {
        _webView = webView;

        _webView.Navigating += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Url))
            {
                _currentUrl = e.Url;
                AddressChanged?.Invoke(this, e.Url);
            }
            _isLoading = true;
            LoadingStateChanged?.Invoke(this, true);
        };

        _webView.Navigated += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Url))
            {
                _currentUrl = e.Url;
                AddressChanged?.Invoke(this, e.Url);
            }
            _isLoading = false;
            LoadingStateChanged?.Invoke(this, false);
            CanGoBack = _webView.CanGoBack;
            CanGoForward = _webView.CanGoForward;
            NavigationHistoryChanged?.Invoke(this, EventArgs.Empty);
            _isReady = true;
        };

        _webView.Loaded += (s, e) =>
        {
            _isReady = true;
        };
    }

    public void Navigate(string url)
    {
        if (_webView == null)
        {
            LogHelper.Warn("[BrowserProvider] Navigate: WebView 未注入");
            return;
        }

        _webView.Source = new Microsoft.Maui.Controls.UrlWebViewSource { Url = url };
        LogHelper.Info($"[BrowserProvider] 导航: {url}");
    }

    public void GoBack()
    {
        _webView?.GoBack();
    }

    public void GoForward()
    {
        _webView?.GoForward();
    }

    public void Reload()
    {
        _webView?.Reload();
    }

    /// <summary>
    /// 触发下载事件 — 由 WebView 拦截下载 URL 时调用。
    /// </summary>
    public void TriggerDownload(string url)
    {
        DownloadRequested?.Invoke(this, url);
    }
}
