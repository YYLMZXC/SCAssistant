namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 文件下载服务接口
/// </summary>
public interface IDownloadService
{
    /// <summary>
    /// 开始下载文件
    /// </summary>
    /// <param name="url">下载URL</param>
    /// <param name="savePath">保存路径（可选）</param>
    /// <returns>下载任务ID</returns>
    Task<string> StartDownloadAsync(string url, string? savePath = null);

    /// <summary>
    /// 取消下载
    /// </summary>
    /// <param name="downloadId">下载任务ID</param>
    Task CancelDownloadAsync(string downloadId);

    /// <summary>
    /// 暂停下载
    /// </summary>
    /// <param name="downloadId">下载任务ID</param>
    Task PauseDownloadAsync(string downloadId);

    /// <summary>
    /// 恢复下载
    /// </summary>
    /// <param name="downloadId">下载任务ID</param>
    Task ResumeDownloadAsync(string downloadId);

    /// <summary>
    /// 获取下载进度
    /// </summary>
    /// <param name="downloadId">下载任务ID</param>
    /// <returns>进度百分比 (0-100)</returns>
    Task<int> GetProgressAsync(string downloadId);

    /// <summary>
    /// 获取下载速度
    /// </summary>
    /// <param name="downloadId">下载任务ID</param>
    /// <returns>速度（字节/秒）</returns>
    Task<long> GetSpeedAsync(string downloadId);
}
