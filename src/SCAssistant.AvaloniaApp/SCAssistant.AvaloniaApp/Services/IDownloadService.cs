using System.Threading.Tasks;

namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 文件下载服务接口。
/// </summary>
public interface IDownloadService
{
    /// <summary>开始下载文件。</summary>
    /// <param name="url">下载地址。</param>
    /// <param name="savePath">保存路径。</param>
    /// <returns>下载任务标识。</returns>
    Task<string> StartDownloadAsync(string url, string savePath);
}
