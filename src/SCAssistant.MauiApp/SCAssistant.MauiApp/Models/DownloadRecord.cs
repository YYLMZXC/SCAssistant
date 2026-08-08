namespace SCAssistant.Maui.Models;

/// <summary>
/// 下载记录数据模型。
/// </summary>
public class DownloadRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? LocalPath { get; set; }
    public long TotalBytes { get; set; }
    public long DownloadedBytes { get; set; }
    public double Progress => TotalBytes > 0 ? (double)DownloadedBytes / TotalBytes : 0;
    public DownloadStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum DownloadStatus
{
    Queued,
    Downloading,
    Completed,
    Failed,
    Cancelled
}
