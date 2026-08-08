using System.Net.Http.Headers;
using SCAssistant.Maui.Models;

namespace SCAssistant.Maui.Services;

/// <summary>
/// DownloadService — 基于 HttpClient 的 HTTP 文件下载实现。
/// </summary>
public class DownloadService : IDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly IDownloadHistoryService _history;
    private readonly List<DownloadRecord> _activeDownloads = new();
    private readonly Dictionary<string, CancellationTokenSource> _cancellations = new();

    public event EventHandler<DownloadRecord>? DownloadProgressChanged;
    public event EventHandler<DownloadRecord>? DownloadCompleted;

    public IReadOnlyList<DownloadRecord> ActiveDownloads => _activeDownloads;

    public DownloadService(IDownloadHistoryService history)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        _history = history;
    }

    public async Task<DownloadRecord> StartDownloadAsync(string url, string? fileName = null, CancellationToken ct = default)
    {
        var record = new DownloadRecord
        {
            Url = url,
            FileName = fileName ?? Path.GetFileName(new Uri(url).AbsolutePath)
                         ?? "download",
            Status = DownloadStatus.Downloading,
            CreatedAt = DateTime.Now
        };

        _activeDownloads.Add(record);

        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _cancellations[record.Id] = cts;
        await _history.AddOrUpdateAsync(record);

        try
        {
            var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            response.EnsureSuccessStatusCode();

            record.TotalBytes = response.Content.Headers.ContentLength ?? -1;

            var downloadDir = Path.Combine(FileSystem.CacheDirectory, "Downloads");
            Directory.CreateDirectory(downloadDir);
            record.LocalPath = Path.Combine(downloadDir, SanitizeFileName(record.FileName));

            await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
            await using var fileStream = File.Create(record.LocalPath);

            var buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, cts.Token)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cts.Token);
                record.DownloadedBytes += bytesRead;
                DownloadProgressChanged?.Invoke(this, record);
            }

            record.Status = DownloadStatus.Completed;
            record.CompletedAt = DateTime.Now;
            DownloadCompleted?.Invoke(this, record);
        }
        catch (OperationCanceledException)
        {
            record.Status = DownloadStatus.Cancelled;
        }
        catch (Exception ex)
        {
            record.Status = DownloadStatus.Failed;
            record.ErrorMessage = ex.Message;
            LogHelper.Error($"[Download] 下载失败: {url}", ex);
        }
        finally
        {
            _activeDownloads.Remove(record);
            _cancellations.Remove(record.Id);
            await _history.AddOrUpdateAsync(record);
        }

        return record;
    }

    public Task CancelDownloadAsync(string downloadId)
    {
        if (_cancellations.TryGetValue(downloadId, out var cts))
        {
            cts.Cancel();
        }
        return Task.CompletedTask;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "download" : sanitized;
    }
}
