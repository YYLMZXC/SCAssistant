using Newtonsoft.Json;

namespace SCAssistant.AvaloniaApp.Models;

/// <summary>
/// 下载记录模型
/// </summary>
public class DownloadRecord
{
    /// <summary>
    /// 下载URL
    /// </summary>
    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 文件保存路径
    /// </summary>
    [JsonProperty("filePath")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    [JsonProperty("fileSize")]
    public long FileSize { get; set; }

    /// <summary>
    /// 下载状态：Pending, Downloading, Completed, Failed, Cancelled
    /// </summary>
    [JsonProperty("status")]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// 下载进度（0-100）
    /// </summary>
    [JsonProperty("progress")]
    public int Progress { get; set; }

    /// <summary>
    /// 下载速度（字节/秒）
    /// </summary>
    [JsonProperty("speed")]
    public long Speed { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 完成时间
    /// </summary>
    [JsonProperty("completedAt")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    [JsonProperty("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 文件名（从URL或文件路径提取）
    /// </summary>
    [JsonIgnore]
    public string FileName => Path.GetFileName(string.IsNullOrEmpty(FilePath) ? Url : FilePath);

    /// <summary>
    /// 文件大小显示文本
    /// </summary>
    [JsonIgnore]
    public string FileSizeText => FormatFileSize(FileSize);

    /// <summary>
    /// 下载速度显示文本
    /// </summary>
    [JsonIgnore]
    public string SpeedText => FormatFileSize(Speed) + "/s";

    /// <summary>
    /// 状态显示文本
    /// </summary>
    [JsonIgnore]
    public string StatusText => Status switch
    {
        "Pending" => "等待中",
        "Downloading" => "下载中",
        "Completed" => "已完成",
        "Failed" => "失败",
        "Cancelled" => "已取消",
        _ => Status
    };

    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < suffixes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {suffixes[order]}";
    }
}
