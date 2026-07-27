using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AvaloniaApplication1.Models;
using AvaloniaApplication1.Services;

namespace AvaloniaApplication1.ViewModels;

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
            Process.Start("explorer.exe", $"/select,\"{localPath}\"");
        }
        else if (!string.IsNullOrWhiteSpace(localPath))
        {
            try
            {
                var folderPath = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                    Process.Start("explorer.exe", $"\"{folderPath}\"");
            }
            catch { }
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
