using System;
using System.Threading;
using System.Threading.Tasks;
using SCAssistant.UnoApp.Models;

namespace SCAssistant.UnoApp.Services;

/// <summary>
/// 文件下载服务接口 - 负责实际执行 HTTP 文件下载。
/// </summary>
public interface IDownloadService
{
    /// <summary>
    /// 开始下载文件。
    /// </summary>
    /// <param name="record">下载记录（含 URL、文件名等信息）</param>
    /// <param name="onProgress">进度回调: (进度百分比, 已下载字节, 总字节)</param>
    /// <param name="ct">取消令牌</param>
    Task StartDownloadAsync(
        DownloadRecord record,
        IProgress<(double Percent, long Received, long Total)>? onProgress = null,
        CancellationToken ct = default);

    /// <summary>
    /// 取消下载。
    /// </summary>
    void CancelDownload(string recordId);

    /// <summary>
    /// 获取平台对应的下载目录。
    /// </summary>
    string GetDownloadDirectory();
}
