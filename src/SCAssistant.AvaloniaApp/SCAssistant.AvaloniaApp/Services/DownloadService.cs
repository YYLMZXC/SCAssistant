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
    /// <summary>全局共享的 HttpClient 实例，30 分钟超时（支持大文件下载）。</summary>
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(30)
    };

    /// <summary>
    /// 使用 HTTP GET 流式下载文件到指定路径。
    /// </summary>
    /// <param name="url">文件下载地址。</param>
    /// <param name="savePath">本地保存路径（含文件名）。</param>
    /// <returns>生成的下载任务 ID。</returns>
    public async Task<string> StartDownloadAsync(string url, string savePath)
    {
        // 生成短 ID 用作任务标识
        var taskId = Guid.NewGuid().ToString("N")[..12];

        try
        {
            // 确保目标目录存在
            var dir = Path.GetDirectoryName(savePath);
            if (dir != null)
                Directory.CreateDirectory(dir);

            LogHelper.Info($"[DownloadService] 开始下载: {url} → {savePath}");

            // 使用 ResponseHeadersRead 实现流式下载，避免大文件完全缓存到内存
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);

            // 流式拷贝：边下载边写入磁盘
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
