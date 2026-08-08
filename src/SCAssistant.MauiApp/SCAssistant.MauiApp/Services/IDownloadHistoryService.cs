namespace SCAssistant.Maui.Services;

using SCAssistant.Maui.Models;

/// <summary>
/// IDownloadHistoryService — 下载历史记录管理接口。
/// </summary>
public interface IDownloadHistoryService
{
    Task<List<DownloadRecord>> GetAllAsync();
    Task AddOrUpdateAsync(DownloadRecord record);
    Task DeleteAsync(string id);
    Task ClearAsync();
    event EventHandler<DownloadRecord>? RecordChanged;
}
