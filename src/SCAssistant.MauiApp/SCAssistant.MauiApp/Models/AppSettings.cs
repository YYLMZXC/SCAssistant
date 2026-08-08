namespace SCAssistant.Maui.Models;

/// <summary>
/// 应用设置数据模型。
/// </summary>
public class AppSettings
{
    public bool DarkMode { get; set; }
    public double ZoomLevel { get; set; } = 1.0;
    public bool AutoCheckUpdates { get; set; } = true;
    public string? LastUrl { get; set; }
    public int MaxDownloadThreads { get; set; } = 3;
    public string? DownloadDirectory { get; set; }
}
