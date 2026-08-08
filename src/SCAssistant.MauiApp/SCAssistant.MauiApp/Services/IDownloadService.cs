namespace SCAssistant.Maui.Services;

using SCAssistant.Maui.Models;

/// <summary>
/// IDownloadService — HTTP 文件下载服务接口。
/// </summary>
public interface IDownloadService
{
    Task<DownloadRecord> StartDownloadAsync(string url, string? fileName = null, CancellationToken ct = default);
    Task CancelDownloadAsync(string downloadId);
    event EventHandler<DownloadRecord>? DownloadProgressChanged;
    event EventHandler<DownloadRecord>? DownloadCompleted;
    IReadOnlyList<DownloadRecord> ActiveDownloads { get; }
}
