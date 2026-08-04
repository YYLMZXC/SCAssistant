using System;
using System.Collections.Generic;
using SCAssistant.UnoApp.Models;

namespace SCAssistant.UnoApp.Services;

public interface IDownloadHistoryService
{
    event Action? HistoryChanged;
    IReadOnlyList<DownloadRecord> Records { get; }
    void AddRecord(DownloadRecord record);
    void UpdateRecord(DownloadRecord record);
    void RemoveRecord(DownloadRecord record);
    void ClearHistory();
    void Load();
    void Save();
}
