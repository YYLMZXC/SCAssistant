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
    /// <summary>下载历史 JSON 文件存储路径。</summary>
    private static readonly string HistoryFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SCAssistant",
        "download_history.json");

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 从 JSON 文件加载所有下载记录。若文件不存在则返回空列表。
    /// 注意：文件 I/O 全部丢到 Task.Run 在线程池执行，避免阻塞 UI 消息循环。
    /// 此前为“伪异步”——同步读文件后返回 Task.FromResult，导致 await 并不让出 UI 线程。
    /// </summary>
    public async Task<List<DownloadRecord>> GetRecordsAsync()
    {
        try
        {
            return await Task.Run(() =>
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
                        return records;
                    }
                }
                else
                {
                    LogHelper.Debug($"[DownloadHistory] 历史文件不存在: {HistoryFilePath}");
                }
                return new List<DownloadRecord>();
            });
        }
        catch (Exception ex)
        {
            LogHelper.Error("[DownloadHistory] 加载下载历史失败", ex);
            return new List<DownloadRecord>();
        }
    }

    /// <summary>添加一条新的下载记录并持久化。</summary>
    public async Task AddRecordAsync(DownloadRecord record)
    {
        var records = await GetRecordsAsync();
        records.Add(record);
        await SaveRecordsAsync(records);
        LogHelper.Info($"[DownloadHistory] 添加记录: {record.FileName}");
    }

    /// <summary>根据 ID 查找并更新下载记录。</summary>
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

    /// <summary>清空所有下载历史（文件 I/O 丢到线程池，不阻塞 UI）。</summary>
    public async Task ClearAllAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                if (File.Exists(HistoryFilePath))
                    File.Delete(HistoryFilePath);
            });
        }
        catch (Exception ex)
        {
            LogHelper.Error("清空下载历史失败", ex);
        }

        LogHelper.Info("[DownloadHistory] 记录已清空");
    }

    /// <summary>持久化全部下载记录（文件 I/O 丢到线程池，不阻塞 UI）。</summary>
    private async Task SaveRecordsAsync(List<DownloadRecord> records)
    {
        try
        {
            await Task.Run(() =>
            {
                var dir = Path.GetDirectoryName(HistoryFilePath);
                if (dir != null) Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(records, _jsonOptions);
                File.WriteAllText(HistoryFilePath, json);
            });
        }
        catch (Exception ex)
        {
            LogHelper.Error("保存下载历史失败", ex);
        }
    }
}
