using System.Collections.Generic;
using System.Threading.Tasks;
using SCAssistant.AvaloniaApp.Models;

namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 下载历史记录服务接口。
/// </summary>
public interface IDownloadHistoryService
{
    /// <summary>获取所有下载记录。</summary>
    Task<List<DownloadRecord>> GetRecordsAsync();

    /// <summary>添加下载记录。</summary>
    Task AddRecordAsync(DownloadRecord record);

    /// <summary>更新下载记录。</summary>
    Task UpdateRecordAsync(DownloadRecord record);

    /// <summary>清空所有记录。</summary>
    Task ClearAllAsync();
}
