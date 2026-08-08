namespace SCAssistant.Maui.Services;

/// <summary>
/// 浏览器提供者接口 — 封装平台 WebView 的导航与控制能力。
/// </summary>
public interface IBrowserProvider
{
    string GetCurrentUrl();
    string GetCurrentTitle();
    bool CanGoBack { get; }
    bool CanGoForward { get; }
    bool IsReady { get; }
    bool IsLoading { get; }

    void Navigate(string url);
    void GoBack();
    void GoForward();
    void Reload();

    event EventHandler<string>? AddressChanged;
    event EventHandler<string>? TitleChanged;
    event EventHandler<bool>? LoadingStateChanged;
    event EventHandler? NavigationHistoryChanged;
    event EventHandler<string>? DownloadRequested;
}
