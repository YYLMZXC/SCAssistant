using System;
using System.Collections.Generic;
using SCAssistant.AvaloniaApp.Models;

namespace SCAssistant.AvaloniaApp.Services;

public interface IDownloadHistoryService
{
    List<DownloadRecord> Records { get; }
    event Action? HistoryChanged;
    void AddRecord(DownloadRecord record);
    void RemoveRecord(DownloadRecord record);
    void Load();
    void Save();
}
