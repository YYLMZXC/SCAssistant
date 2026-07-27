using System;
using System.Collections.Generic;
using AvaloniaApplication1.Models;

namespace AvaloniaApplication1.Services;

public interface IDownloadHistoryService
{
    List<DownloadRecord> Records { get; }
    event Action? HistoryChanged;
    void AddRecord(DownloadRecord record);
    void RemoveRecord(DownloadRecord record);
    void Load();
    void Save();
}
