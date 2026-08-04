using System;

namespace SCAssistant.UnoApp.Services;

/// <summary>
/// 浏览器提供者接口 - 统一管理跨平台浏览器控件。
/// </summary>
public interface IBrowserProvider
{
    event EventHandler<string>? AddressChanged;
    event EventHandler<string>? TitleChanged;
    event EventHandler<bool>? LoadingStateChanged;

    string CurrentUrl { get; }
    string CurrentTitle { get; }
    bool IsLoading { get; }

    /// <summary>
    /// 创建浏览器控件并嵌入到 UI 中。
    /// </summary>
    object CreateBrowserControl();

    /// <summary>
    /// 初始化并导航到起始 URL。
    /// </summary>
    void Initialize(string startUrl);

    /// <summary>
    /// 导航到指定 URL。
    /// </summary>
    void Navigate(string url);

    /// <summary>
    /// 刷新当前页面。
    /// </summary>
    void Reload();
}
