using System.Collections.ObjectModel;
using Newtonsoft.Json;
using SCAssistant.Maui.Models;

namespace SCAssistant.Maui.Services;

public class DownloadHistoryService
{
    private static DownloadHistoryService? _instance;
    private static readonly object _lock = new();

    public static DownloadHistoryService Instance
    {
        get
        {
            lock (_lock)
            {
                _instance ??= new DownloadHistoryService();
                return _instance;
            }
        }
    }

    private readonly ObservableCollection<DownloadRecord> _history = new();
    private string? _historyFilePath;

    public event EventHandler? HistoryChanged;

    public ObservableCollection<DownloadRecord> History => _history;

    private DownloadHistoryService() { }

    private string HistoryFilePath
    {
        get
        {
            if (_historyFilePath == null)
            {
                var basePath = FileSystem.AppDataDirectory;
                _historyFilePath = Path.Combine(basePath, "download_history.json");
            }
            return _historyFilePath;
        }
    }

    public async Task LoadHistoryAsync()
    {
        try
        {
            if (File.Exists(HistoryFilePath))
            {
                var json = await File.ReadAllTextAsync(HistoryFilePath);
                var records = JsonConvert.DeserializeObject<List<DownloadRecord>>(json);
                if (records != null)
                {
                    _history.Clear();
                    foreach (var record in records)
                    {
                        _history.Add(record);
                    }
                    HistoryChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        catch (Exception)
        {
            _history.Clear();
        }
    }

    public async Task SaveHistoryAsync()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_history.ToList(), Formatting.Indented);
            var dir = Path.GetDirectoryName(HistoryFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            await File.WriteAllTextAsync(HistoryFilePath, json);
        }
        catch (Exception)
        {
        }
    }

    public async Task AddRecordAsync(string fileName, string url, string localPath)
    {
        if (_history.Any(r => r.Url == url && r.LocalPath == localPath))
            return;

        var record = new DownloadRecord
        {
            FileName = fileName,
            Url = url,
            LocalPath = localPath,
            DownloadTime = DateTime.Now
        };

        _history.Add(record);
        HistoryChanged?.Invoke(this, EventArgs.Empty);
        await SaveHistoryAsync();
    }

    public async Task RemoveRecordAsync(DownloadRecord record)
    {
        _history.Remove(record);
        HistoryChanged?.Invoke(this, EventArgs.Empty);
        await SaveHistoryAsync();
    }

    public async Task ClearHistoryAsync()
    {
        _history.Clear();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
        await SaveHistoryAsync();
    }
}
