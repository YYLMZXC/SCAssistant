using System.Collections.Concurrent;
using SCAssistant.AvaloniaApp.Models;

namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 文件下载服务实现
/// </summary>
public class DownloadService : IDownloadService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeDownloads = new();
    private readonly ConcurrentDictionary<string, (int Progress, long Speed)> _downloadStates = new();
    private readonly string _defaultDownloadDir;

    public DownloadService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(30)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "SCAssistant/1.0");

        _defaultDownloadDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            "SCAssistant");
        Directory.CreateDirectory(_defaultDownloadDir);
    }

    public async Task<string> StartDownloadAsync(string url, string? savePath = null)
    {
        var downloadId = Guid.NewGuid().ToString("N");
        var cts = new CancellationTokenSource();
        _activeDownloads[downloadId] = cts;
        _downloadStates[downloadId] = (0, 0);

        if (string.IsNullOrEmpty(savePath))
        {
            var fileName = GetFileNameFromUrl(url);
            savePath = Path.Combine(_defaultDownloadDir, fileName);
        }

        _ = Task.Run(async () =>
        {
            try
            {
                using var response = await _httpClient.GetAsync(url,
                    HttpCompletionOption.ResponseHeadersRead, cts.Token);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                await using var contentStream = await response.Content.ReadAsStreamAsync(cts.Token);
                await using var fileStream = new FileStream(savePath, FileMode.Create,
                    FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                long totalRead = 0;
                var lastReportTime = DateTime.UtcNow;
                long lastReportBytes = 0;

                int bytesRead;
                while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cts.Token)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cts.Token);
                    totalRead += bytesRead;

                    var now = DateTime.UtcNow;
                    var elapsed = (now - lastReportTime).TotalSeconds;
                    if (elapsed >= 0.5)
                    {
                        var progress = totalBytes > 0 ? (int)(totalRead * 100 / totalBytes) : -1;
                        var speed = (long)((totalRead - lastReportBytes) / elapsed);
                        _downloadStates[downloadId] = (progress, speed);
                        lastReportTime = now;
                        lastReportBytes = totalRead;
                    }

                    // Trigger periodic progress events
                }

                _downloadStates[downloadId] = (100, 0);
            }
            catch (OperationCanceledException)
            {
                _downloadStates[downloadId] = (0, 0);
            }
            catch (Exception)
            {
                _downloadStates[downloadId] = (-1, 0);
            }
            finally
            {
                _activeDownloads.TryRemove(downloadId, out _);
            }
        }, cts.Token);

        return await Task.FromResult(downloadId);
    }

    public Task CancelDownloadAsync(string downloadId)
    {
        if (_activeDownloads.TryGetValue(downloadId, out var cts))
        {
            cts.Cancel();
        }
        return Task.CompletedTask;
    }

    public Task PauseDownloadAsync(string downloadId)
    {
        // Simple implementation: cancel and mark as paused
        if (_activeDownloads.TryGetValue(downloadId, out var cts))
        {
            cts.Cancel();
            _downloadStates[downloadId] = (-2, 0); // -2 = paused
        }
        return Task.CompletedTask;
    }

    public Task ResumeDownloadAsync(string downloadId)
    {
        // Resume not supported in simplified implementation
        return Task.CompletedTask;
    }

    public Task<int> GetProgressAsync(string downloadId)
    {
        return Task.FromResult(_downloadStates.TryGetValue(downloadId, out var state)
            ? state.Progress : 0);
    }

    public Task<long> GetSpeedAsync(string downloadId)
    {
        return Task.FromResult(_downloadStates.TryGetValue(downloadId, out var state)
            ? state.Speed : 0L);
    }

    public void Dispose()
    {
        foreach (var cts in _activeDownloads.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _activeDownloads.Clear();
        _httpClient.Dispose();
    }

    private static string GetFileNameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var fileName = Path.GetFileName(uri.LocalPath);
            if (!string.IsNullOrEmpty(fileName))
                return fileName;
        }
        catch { }

        return $"download_{DateTime.Now:yyyyMMddHHmmss}";
    }
}
