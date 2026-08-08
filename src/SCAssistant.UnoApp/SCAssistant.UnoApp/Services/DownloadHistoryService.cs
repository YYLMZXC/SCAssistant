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
        LogHelper.Info($"[下载历史] 初始化，存储路径: {_filePath}");
    }

    public void Load()
    {
        if (!File.Exists(_filePath))
        {
            LogHelper.Info("[下载历史] 文件不存在，初始化为空列表");
            _records = new List<DownloadRecord>();
            return;
        }

        var json = File.ReadAllText(_filePath);
        _records = JsonConvert.DeserializeObject<List<DownloadRecord>>(json) ?? new List<DownloadRecord>();
        LogHelper.Info($"[下载历史] 加载成功，共 {_records.Count} 条记录");
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (dir is not null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
            LogHelper.Info($"[下载历史] 创建目录: {dir}");
        }

        var json = JsonConvert.SerializeObject(_records, Formatting.Indented);
        File.WriteAllText(_filePath, json);
        LogHelper.Info($"[下载历史] 保存成功，共 {_records.Count} 条记录 -> {_filePath}");
    }

    public void AddRecord(DownloadRecord record)
    {
        _records.Add(record);
        LogHelper.Info($"[下载历史] 添加记录: Id={record.Id}, Name={record.FileName}");
        Save();
        HistoryChanged?.Invoke();
    }

    public void UpdateRecord(DownloadRecord record)
    {
        var index = _records.FindIndex(r => r.Id == record.Id);
        if (index >= 0)
        {
            _records[index] = record;
            LogHelper.Info($"[下载历史] 更新记录: Id={record.Id}, Name={record.FileName}");
            Save();
            HistoryChanged?.Invoke();
        }
        else
        {
            LogHelper.Warn($"[下载历史] 更新失败，未找到记录: Id={record.Id}");
        }
    }

    public void RemoveRecord(DownloadRecord record)
    {
        _records.RemoveAll(r => r.Id == record.Id);
        LogHelper.Info($"[下载历史] 删除记录: Id={record.Id}，当前共 {_records.Count} 条");
        Save();
        HistoryChanged?.Invoke();
    }

    public void ClearHistory()
    {
        var count = _records.Count;
        _records.Clear();
        if (File.Exists(_filePath))
            File.Delete(_filePath);
        LogHelper.Info($"[下载历史] 清空所有记录，已清除 {count} 条");
        HistoryChanged?.Invoke();
    }
}
