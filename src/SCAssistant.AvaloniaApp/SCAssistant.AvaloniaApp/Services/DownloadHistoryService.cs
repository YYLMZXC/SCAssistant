using System;
using System.Collections.Generic;
using System.IO;
using SCAssistant.AvaloniaApp.Models;
using Newtonsoft.Json;

namespace SCAssistant.AvaloniaApp.Services;

public class DownloadHistoryService : IDownloadHistoryService
{
    private static readonly string HistoryFilePath =
        Path.Combine(AppContext.BaseDirectory, "download_history.json");

    public List<DownloadRecord> Records { get; private set; } = [];
    public event Action? HistoryChanged;

    public void Load()
    {
        if (File.Exists(HistoryFilePath))
        {
            try
            {
                var json = File.ReadAllText(HistoryFilePath);
                Records = JsonConvert.DeserializeObject<List<DownloadRecord>>(json) ?? [];
            }
            catch
            {
                Records = [];
            }
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonConvert.SerializeObject(Records, Formatting.Indented);
            File.WriteAllText(HistoryFilePath, json);
            HistoryChanged?.Invoke();
        }
        catch
        {
            // silently ignore save errors
        }
    }

    public void AddRecord(DownloadRecord record)
    {
        Records.Add(record);
        Save();
    }

    public void RemoveRecord(DownloadRecord record)
    {
        Records.Remove(record);
        Save();
    }
}
