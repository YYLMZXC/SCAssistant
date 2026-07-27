using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCAssistant.AvaloniaApp.Models;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.ViewModels;

public partial class DownloadListViewModel : ViewModelBase
{
    private readonly IDownloadHistoryService _historyService;

    public ObservableCollection<DownloadRecord> Records { get; } = [];

    [ObservableProperty]
    public partial DownloadRecord? SelectedRecord { get; set; }

    public DownloadListViewModel() : this(ServiceLocator.DownloadHistory) { }

    public DownloadListViewModel(IDownloadHistoryService historyService)
    {
        _historyService = historyService;
        _historyService.HistoryChanged += OnHistoryChanged;
        Refresh();
    }

    private void OnHistoryChanged()
    {
        Refresh();
    }

    private void Refresh()
    {
        Records.Clear();
        foreach (var r in _historyService.Records)
            Records.Add(r);
    }

    [RelayCommand]
    private void OpenFolder()
    {
        if (SelectedRecord == null) return;
        var localPath = SelectedRecord.LocalPath;

        if (File.Exists(localPath))
        {
            RevealFileInFolder(localPath);
        }
        else if (!string.IsNullOrWhiteSpace(localPath))
        {
            try
            {
                var folderPath = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                    OpenFolderInExplorer(folderPath);
            }
            catch { }
        }
    }

    private static void RevealFileInFolder(string filePath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", $"-R \"{filePath}\"");
        }
        else
        {
            // Linux: open the containing folder
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
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", $"\"{folderPath}\"");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", $"\"{folderPath}\"");
        }
        else
        {
            Process.Start(new ProcessStartInfo(folderPath) { UseShellExecute = true });
        }
    }

    [RelayCommand]
    private void DeleteRecord()
    {
        if (SelectedRecord == null) return;
        _historyService.RemoveRecord(SelectedRecord);
        SelectedRecord = null;
    }
}
