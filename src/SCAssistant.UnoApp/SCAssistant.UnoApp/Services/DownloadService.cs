using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SCAssistant.UnoApp.Models;

namespace SCAssistant.UnoApp.Services;

public class DownloadService : IDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellations = new();

    public DownloadService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(30)
        };
        // 设置合理的 User-Agent，避免被服务器拒绝
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        LogHelper.Info("[下载服务] 初始化完成");
    }

    public async Task StartDownloadAsync(
        DownloadRecord record,
        IProgress<(double Percent, long Received, long Total)>? onProgress = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(record.Url))
            throw new ArgumentException("下载 URL 不能为空", nameof(record));

        // 使用传入的 CancellationToken 包装一个内部 CancellationTokenSource
        var internalCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _cancellations[record.Id] = internalCts;

        try
        {
            LogHelper.Info($"[下载服务] 开始下载: {record.FileName}, URL={record.Url}");

            record.State = DownloadState.Downloading;
            record.DownloadTime = DateTime.Now;

            using var response = await _httpClient.GetAsync(
                record.Url, HttpCompletionOption.ResponseHeadersRead, internalCts.Token);

            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            record.FileSize = totalBytes > 0 ? totalBytes : 0;

            LogHelper.Info($"[下载服务] 响应 200 OK, 文件大小: {(totalBytes > 0 ? $"{totalBytes} 字节" : "未知")}");

            // 确定保存路径
            var downloadDir = GetDownloadDirectory();
            Directory.CreateDirectory(downloadDir);

            // 处理重名文件
            var savePath = GetSavePath(downloadDir, record.FileName);

            // 如果文件名未知，尝试从 URL 或 Content-Disposition 获取
            if (string.IsNullOrWhiteSpace(record.FileName))
                record.FileName = GetFileNameFromResponse(response, record.Url);

            savePath = GetSavePath(downloadDir, record.FileName);
            record.LocalPath = savePath;

            // 流式下载写入文件
            using var contentStream = await response.Content.ReadAsStreamAsync(internalCts.Token);
            using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write,
                FileShare.None, bufferSize: 8192, useAsync: true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;
            var lastReportTime = DateTime.UtcNow;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, internalCts.Token)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, internalCts.Token);
                totalRead += bytesRead;

                // 限流: 每 200ms 只上报一次进度，避免 UI 刷新过于频繁
                var now = DateTime.UtcNow;
                if ((now - lastReportTime).TotalMilliseconds >= 200)
                {
                    var percent = totalBytes > 0 ? (double)totalRead / totalBytes * 100 : -1;
                    record.Progress = percent > 0 ? percent : 0;
                    onProgress?.Report((record.Progress, totalRead, totalBytes));
                    lastReportTime = now;
                }
            }

            // 完成
            record.State = DownloadState.Completed;
            record.CompletedTime = DateTime.Now;
            record.Progress = 100;
            onProgress?.Report((100, totalRead, totalRead));
            LogHelper.Info($"[下载服务] 下载完成: {record.FileName}, 大小={totalRead} 字节, 路径={savePath}");
        }
        catch (OperationCanceledException)
        {
            record.State = DownloadState.Cancelled;
            record.ErrorMessage = "下载已取消";
            LogHelper.Warn($"[下载服务] 下载取消: {record.FileName}");
            // 删除未完成的文件
            TryDeleteFile(record.LocalPath);
        }
        catch (Exception ex)
        {
            record.State = DownloadState.Failed;
            record.ErrorMessage = ex.Message;
            LogHelper.Error($"[下载服务] 下载失败: {record.FileName}", ex);
            // 删除未完成的文件
            TryDeleteFile(record.LocalPath);
        }
        finally
        {
            _cancellations.TryRemove(record.Id, out _);
            internalCts.Dispose();
        }
    }

    public void CancelDownload(string recordId)
    {
        if (_cancellations.TryGetValue(recordId, out var cts))
        {
            LogHelper.Info($"[下载服务] 请求取消下载: {recordId}");
            cts.Cancel();
        }
    }

    public string GetDownloadDirectory()
    {
#if ANDROID
        return AndroidDownloadDirectory();
#elif __IOS__
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
#else
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");
#endif
    }

#if ANDROID
    /// <summary>
    /// 获取 Android 下载目录。
    /// Android 10+: 优先使用 MediaStore 公共 Downloads 目录，
    /// 如果无法写入则回退到应用私有目录。
    /// Android 9-: 使用 Environment 公共目录。
    /// </summary>
    private static string AndroidDownloadDirectory()
    {
        try
        {
            // 尝试应用专属外部存储（不需要运行时权限，Android API 29+）
            var appContext = Android.App.Application.Context;
            if (appContext is not null)
            {
                var externalDir = appContext.GetExternalFilesDir(
                    Android.OS.Environment.DirectoryDownloads);
                if (externalDir is not null)
                    return externalDir.AbsolutePath;
            }
        }
        catch { /* 回退到公共目录 */ }

        // 回退：公共 Downloads 目录（API 28 以下或需要权限的旧设备）
        try
        {
            var publicDir = Android.OS.Environment.GetExternalStoragePublicDirectory(
                Android.OS.Environment.DirectoryDownloads);
            if (publicDir is not null)
                return publicDir.AbsolutePath;
        }
        catch { /* 最终回退 */ }

        // 最终回退
        return "/sdcard/Download";
    }
#endif

    private static string GetSavePath(string directory, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "download";

        var basePath = Path.Combine(directory, fileName);
        if (!File.Exists(basePath))
            return basePath;

        // 重名处理: file.txt -> file (1).txt
        var dirName = Path.GetDirectoryName(basePath) ?? directory;
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var counter = 1;

        string newPath;
        do
        {
            newPath = Path.Combine(dirName, $"{nameWithoutExt} ({counter}){ext}");
            counter++;
        } while (File.Exists(newPath));

        return newPath;
    }

    private static string GetFileNameFromResponse(HttpResponseMessage response, string url)
    {
        // 先从 Content-Disposition 头部获取
        var contentDisposition = response.Content.Headers.ContentDisposition;
        if (contentDisposition?.FileName is not null)
        {
            var fileName = contentDisposition.FileName.Trim('"');
            if (!string.IsNullOrWhiteSpace(fileName))
                return Uri.UnescapeDataString(fileName);
        }
        if (contentDisposition?.FileNameStar is not null)
        {
            var fileName = contentDisposition.FileNameStar.Trim('"');
            if (!string.IsNullOrWhiteSpace(fileName))
                return Uri.UnescapeDataString(fileName);
        }

        // 从 URL 中提取文件名
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            var name = Path.GetFileName(path);
            if (!string.IsNullOrWhiteSpace(name))
                return Uri.UnescapeDataString(name);
        }
        catch { }

        return "download";
    }

    private static void TryDeleteFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"[下载服务] 清理未完成文件失败: {path}, {ex.Message}");
        }
    }
}
