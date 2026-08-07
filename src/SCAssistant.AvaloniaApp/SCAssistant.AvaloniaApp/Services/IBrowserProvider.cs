using System;
using System.Threading.Tasks;

namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 浏览器功能抽象接口。
/// </summary>
public interface IBrowserProvider
{
    /// <summary>浏览器是否已就绪（平台 WebView 已初始化完成）。</summary>
    bool IsReady { get; }

    /// <summary>浏览器就绪事件（平台 WebView 初始化完成时触发）。</summary>
    event EventHandler? ReadyChanged;

    /// <summary>地址变更（导航到新 URL）。</summary>
    event EventHandler<string> AddressChanged;

    /// <summary>页面标题变更。</summary>
    event EventHandler<string> TitleChanged;

    /// <summary>加载状态变更。</summary>
    event EventHandler<bool> LoadingStateChanged;

    /// <summary>下载请求（页面触发下载）。</summary>
    event EventHandler<string> DownloadRequested;

    /// <summary>导航历史变更（前进/后退可用性变化）。</summary>
    event EventHandler NavigationHistoryChanged;

    /// <summary>是否可以后退。</summary>
    bool CanGoBack { get; }

    /// <summary>是否可以前进。</summary>
    bool CanGoForward { get; }

    /// <summary>是否正在加载。</summary>
    bool IsLoading { get; }

    /// <summary>导航到指定 URL。</summary>
    void Navigate(string url);

    /// <summary>重新加载当前页。</summary>
    void Reload();

    /// <summary>后退。</summary>
    void GoBack();

    /// <summary>前进。</summary>
    void GoForward();

    /// <summary>获取当前 URL。</summary>
    string GetCurrentUrl();

    /// <summary>获取页面标题。</summary>
    string GetTitle();

    /// <summary>执行 JavaScript。</summary>
    Task<string> ExecuteScriptAsync(string script);

    /// <summary>初始化浏览器实例。</summary>
    void Initialize();
}
