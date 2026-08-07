using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 文件下载服务实现。
/// </summary>
public class DownloadService : IDownloadService
{
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(30)
    };

    public async Task<string> StartDownloadAsync(string url, string savePath)
    {
        var taskId = Guid.NewGuid().ToString("N")[..12];

        try
        {
            // 确保目录存在
            var dir = Path.GetDirectoryName(savePath);
            if (dir != null)
                Directory.CreateDirectory(dir);

            LogHelper.Info($"[DownloadService] 开始下载: {url} → {savePath}");

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);

            await contentStream.CopyToAsync(fileStream);

            LogHelper.Info($"[DownloadService] 下载完成: {savePath}");
            return taskId;
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[DownloadService] 下载失败: {url}", ex);
            throw;
        }
    }
}
