namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 浏览器提供者接口
/// </summary>
public interface IBrowserProvider
{
    /// <summary>
    /// 导航到指定URL
    /// </summary>
    void Navigate(string url);

    /// <summary>
    /// 后退
    /// </summary>
    void GoBack();

    /// <summary>
    /// 前进
    /// </summary>
    void GoForward();

    /// <summary>
    /// 刷新
    /// </summary>
    void Refresh();

    /// <summary>
    /// 停止加载
    /// </summary>
    void Stop();

    /// <summary>
    /// 执行JavaScript
    /// </summary>
    Task<string> ExecuteScriptAsync(string script);

    /// <summary>
    /// 获取当前URL
    /// </summary>
    string GetCurrentUrl();

    /// <summary>
    /// 获取页面标题
    /// </summary>
    string GetTitle();

    /// <summary>
    /// 能否后退
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// 能否前进
    /// </summary>
    bool CanGoForward { get; }

    /// <summary>
    /// 是否正在加载
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// 获取Cookie（用于特定域）
    /// </summary>
    Task<string> GetCookiesAsync(string domain);

    /// <summary>
    /// 清除所有Cookie
    /// </summary>
    Task ClearCookiesAsync();

    /// <summary>
    /// 导航开始事件
    /// </summary>
    event EventHandler<string>? NavigationStarted;

    /// <summary>
    /// 导航完成事件
    /// </summary>
    event EventHandler<string>? NavigationCompleted;

    /// <summary>
    /// 标题变更事件
    /// </summary>
    event EventHandler<string>? TitleChanged;

    /// <summary>
    /// 加载状态变更事件
    /// </summary>
    event EventHandler<bool>? LoadingStateChanged;

    /// <summary>
    /// 获取WebView原生控件（用于嵌入UI）
    /// </summary>
    object? GetNativeControl();
}
