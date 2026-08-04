using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCAssistant.UnoApp.Models;
using SCAssistant.UnoApp.Services;

namespace SCAssistant.UnoApp.ViewModels;

public partial class DownloadListViewModel : ViewModelBase
{
    private readonly IDownloadHistoryService _historyService;

    public ObservableCollection<DownloadRecord> Records { get; } = [];

    [ObservableProperty]
    public partial DownloadRecord? SelectedRecord { get; set; }

    public DownloadListViewModel(IDownloadHistoryService historyService)
    {
        LogHelper.Info("[下载列表] 构造函数 - 初始化");
        _historyService = historyService;
        _historyService.HistoryChanged += OnHistoryChanged;
        Refresh();
    }

    private void OnHistoryChanged()
    {
        LogHelper.Info("[下载列表] 历史记录变更，刷新列表");
        Refresh();
    }

    private void Refresh()
    {
        Records.Clear();
        foreach (var r in _historyService.Records)
            Records.Add(r);
        LogHelper.Info($"[下载列表] 刷新完成，共 {Records.Count} 条记录");
    }

    [RelayCommand]
    private void OpenFolder()
    {
        if (SelectedRecord == null)
        {
            LogHelper.Warn("[下载列表] OpenFolder - 未选中任何记录");
            return;
        }
        var localPath = SelectedRecord.LocalPath;

        if (File.Exists(localPath))
        {
            LogHelper.Info($"[下载列表] 打开文件所在位置: {localPath}");
            RevealFileInFolder(localPath);
        }
        else if (!string.IsNullOrWhiteSpace(localPath))
        {
            try
            {
                var folderPath = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                {
                    LogHelper.Info($"[下载列表] 文件不存在，打开父目录: {folderPath}");
                    OpenFolderInExplorer(folderPath);
                }
                else
                {
                    LogHelper.Warn($"[下载列表] 文件路径无效或目录不存在: {localPath}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[下载列表] 打开文件夹失败: {ex.Message}", ex);
            }
        }
    }

    private static void RevealFileInFolder(string filePath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            LogHelper.Info($"[下载列表] 资源管理器定位文件(Windows): {filePath}");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", $"-R \"{filePath}\"");
            LogHelper.Info($"[下载列表] Finder 定位文件(macOS): {filePath}");
        }
        else
        {
            var folder = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(folder))
                OpenFolderInExplorer(folder);
        }
    }

    private static void OpenFolderInExplorer(string folderPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start("explorer.exe", $"\"{folderPath}\"");
            LogHelper.Info($"[下载列表] 资源管理器打开目录(Windows): {folderPath}");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", $"\"{folderPath}\"");
            LogHelper.Info($"[下载列表] Finder 打开目录(macOS): {folderPath}");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", $"\"{folderPath}\"");
            LogHelper.Info($"[下载列表] 文件管理器打开目录(Linux): {folderPath}");
        }
        else
        {
            Process.Start(new ProcessStartInfo(folderPath) { UseShellExecute = true });
            LogHelper.Info($"[下载列表] 打开目录(其他平台): {folderPath}");
        }
    }

    [RelayCommand]
    private void DeleteRecord()
    {
        if (SelectedRecord == null)
        {
            LogHelper.Warn("[下载列表] 删除记录 - 未选中任何记录");
            return;
        }
        LogHelper.Info($"[下载列表] 删除记录: Id={SelectedRecord.Id}, Name={SelectedRecord.FileName}");
        _historyService.RemoveRecord(SelectedRecord);
        SelectedRecord = null;
    }
}
