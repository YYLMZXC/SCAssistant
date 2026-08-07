namespace SCAssistant.AvaloniaApp.Models;

/// <summary>
/// 应用设置模型
/// </summary>
public class AppSettings
{
    /// <summary>
    /// 默认下载目录
    /// </summary>
    public string DownloadDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 最大同时下载数
    /// </summary>
    public int MaxConcurrentDownloads { get; set; } = 3;

    /// <summary>
    /// 是否启用下载历史记录
    /// </summary>
    public bool EnableDownloadHistory { get; set; } = true;

    /// <summary>
    /// 默认搜索引擎
    /// </summary>
    public string DefaultSearchEngine { get; set; } = "https://www.google.com/search?q=";

    /// <summary>
    /// 是否启用广告过滤
    /// </summary>
    public bool EnableAdBlock { get; set; } = false;

    /// <summary>
    /// 主页URL
    /// </summary>
    public string HomePageUrl { get; set; } = "https://www.google.com";

    /// <summary>
    /// 主题模式：Light, Dark, System
    /// </summary>
    public string Theme { get; set; } = "System";
}
