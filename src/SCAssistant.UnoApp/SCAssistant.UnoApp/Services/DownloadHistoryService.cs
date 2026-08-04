using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using SCAssistant.UnoApp.Models;

namespace SCAssistant.UnoApp.Services;

public class DownloadHistoryService : IDownloadHistoryService
{
    private readonly string _filePath;
    private List<DownloadRecord> _records = new();

    public event Action? HistoryChanged;

    public IReadOnlyList<DownloadRecord> Records => _records.AsReadOnly();

    public DownloadHistoryService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _filePath = Path.Combine(appData, "SCAssistant", "download_history.json");
    }

    public void Load()
    {
        if (!File.Exists(_filePath))
        {
            _records = new List<DownloadRecord>();
            return;
        }

        var json = File.ReadAllText(_filePath);
        _records = JsonConvert.DeserializeObject<List<DownloadRecord>>(json) ?? new List<DownloadRecord>();
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (dir is not null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonConvert.SerializeObject(_records, Formatting.Indented);
        File.WriteAllText(_filePath, json);
    }

    public void AddRecord(DownloadRecord record)
    {
        _records.Add(record);
        Save();
        HistoryChanged?.Invoke();
    }

    public void UpdateRecord(DownloadRecord record)
    {
        var index = _records.FindIndex(r => r.Id == record.Id);
        if (index >= 0)
        {
            _records[index] = record;
            Save();
            HistoryChanged?.Invoke();
        }
    }

    public void RemoveRecord(DownloadRecord record)
    {
        _records.RemoveAll(r => r.Id == record.Id);
        Save();
        HistoryChanged?.Invoke();
    }

    public void ClearHistory()
    {
        _records.Clear();
        if (File.Exists(_filePath))
            File.Delete(_filePath);
        HistoryChanged?.Invoke();
    }
}
