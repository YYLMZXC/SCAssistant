using System.Text.Json;
using SCAssistant.Maui.Models;

namespace SCAssistant.Maui.Services;

/// <summary>
/// DownloadHistoryService — 基于 JSON 文件的下载历史持久化。
/// </summary>
public class DownloadHistoryService : IDownloadHistoryService
{
    private readonly string _filePath;
    private List<DownloadRecord> _records = new();

    public event EventHandler<DownloadRecord>? RecordChanged;

    public DownloadHistoryService()
    {
        _filePath = Path.Combine(FileSystem.AppDataDirectory, "download_history.json");
    }

    public async Task<List<DownloadRecord>> GetAllAsync()
    {
        if (_records.Count == 0)
            await LoadAsync();
        return _records.OrderByDescending(r => r.CreatedAt).ToList();
    }

    public async Task AddOrUpdateAsync(DownloadRecord record)
    {
        var existing = _records.FirstOrDefault(r => r.Id == record.Id);
        if (existing != null)
        {
            _records.Remove(existing);
        }
        _records.Add(record);

        await SaveAsync();
        RecordChanged?.Invoke(this, record);
    }

    public async Task DeleteAsync(string id)
    {
        _records.RemoveAll(r => r.Id == id);
        await SaveAsync();
    }

    public async Task ClearAsync()
    {
        _records.Clear();
        await SaveAsync();
    }

    private async Task LoadAsync()
    {
        if (File.Exists(_filePath))
        {
            var json = await File.ReadAllTextAsync(_filePath);
            _records = JsonSerializer.Deserialize<List<DownloadRecord>>(json) ?? new();
        }
    }

    private async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(_records, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_filePath, json);
    }
}
