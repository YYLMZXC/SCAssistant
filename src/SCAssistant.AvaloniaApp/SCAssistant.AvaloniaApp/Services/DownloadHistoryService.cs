using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SCAssistant.AvaloniaApp.Models;

namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 下载历史持久化服务 — 将下载记录保存为本地 JSON 文件。
/// </summary>
public class DownloadHistoryService : IDownloadHistoryService
{
    private static readonly string HistoryFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SCAssistant",
        "download_history.json");

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public Task<List<DownloadRecord>> GetRecordsAsync()
    {
        try
        {
            var dir = Path.GetDirectoryName(HistoryFilePath);
            if (dir != null) Directory.CreateDirectory(dir);

            if (File.Exists(HistoryFilePath))
            {
                var json = File.ReadAllText(HistoryFilePath);
                var records = JsonSerializer.Deserialize<List<DownloadRecord>>(json, _jsonOptions);
                if (records != null)
                {
                    LogHelper.Debug($"[DownloadHistory] 加载历史: {records.Count} 条记录");
                    return Task.FromResult(records);
                }
            }
            else
            {
                LogHelper.Debug($"[DownloadHistory] 历史文件不存在: {HistoryFilePath}");
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error("[DownloadHistory] 加载下载历史失败", ex);
        }

        return Task.FromResult(new List<DownloadRecord>());
    }

    public async Task AddRecordAsync(DownloadRecord record)
    {
        var records = await GetRecordsAsync();
        records.Add(record);
        await SaveRecordsAsync(records);
        LogHelper.Info($"[DownloadHistory] 添加记录: {record.FileName}");
    }

    public async Task UpdateRecordAsync(DownloadRecord record)
    {
        var records = await GetRecordsAsync();
        for (int i = 0; i < records.Count; i++)
        {
            if (records[i].Id == record.Id)
            {
                records[i] = record;
                await SaveRecordsAsync(records);
                LogHelper.Info($"[DownloadHistory] 更新记录: {record.FileName}");
                return;
            }
        }
    }

    public Task ClearAllAsync()
    {
        try
        {
            if (File.Exists(HistoryFilePath))
                File.Delete(HistoryFilePath);
        }
        catch (Exception ex)
        {
            LogHelper.Error("清空下载历史失败", ex);
        }

        LogHelper.Info("[DownloadHistory] 记录已清空");
        return Task.CompletedTask;
    }

    private Task SaveRecordsAsync(List<DownloadRecord> records)
    {
        try
        {
            var dir = Path.GetDirectoryName(HistoryFilePath);
            if (dir != null) Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(records, _jsonOptions);
            File.WriteAllText(HistoryFilePath, json);
        }
        catch (Exception ex)
        {
            LogHelper.Error("保存下载历史失败", ex);
        }

        return Task.CompletedTask;
    }
}
