using Newtonsoft.Json;
using SCAssistant.AvaloniaApp.Models;

namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 下载历史服务实现 - 使用JSON文件存储
/// </summary>
public class DownloadHistoryService : IDownloadHistoryService
{
    private readonly string _storagePath;
    private List<DownloadRecord> _records = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public DownloadHistoryService()
    {
        _storagePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SCAssistant",
            "download_history.json");

        LoadRecords();
    }

    public async Task<List<DownloadRecord>> GetRecordsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return new List<DownloadRecord>(_records);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task AddRecordAsync(DownloadRecord record)
    {
        await _lock.WaitAsync();
        try
        {
            _records.Add(record);
            await SaveRecordsAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UpdateRecordAsync(DownloadRecord record)
    {
        await _lock.WaitAsync();
        try
        {
            var index = _records.FindIndex(r => r.Url == record.Url);
            if (index >= 0)
            {
                record.CreatedAt = _records[index].CreatedAt;
                _records[index] = record;
            }
            else
            {
                _records.Add(record);
            }
            await SaveRecordsAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteRecordAsync(string url)
    {
        await _lock.WaitAsync();
        try
        {
            _records.RemoveAll(r => r.Url == url);
            await SaveRecordsAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ClearAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _records.Clear();
            await SaveRecordsAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    private void LoadRecords()
    {
        try
        {
            var dir = Path.GetDirectoryName(_storagePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            if (File.Exists(_storagePath))
            {
                var json = File.ReadAllText(_storagePath);
                _records = JsonConvert.DeserializeObject<List<DownloadRecord>>(json) ?? new List<DownloadRecord>();
            }
        }
        catch
        {
            _records = new List<DownloadRecord>();
        }
    }

    private async Task SaveRecordsAsync()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_records, Formatting.Indented);
            await File.WriteAllTextAsync(_storagePath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }
}
