using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCAssistant.Maui.Models;
using SCAssistant.Maui.Services;

namespace SCAssistant.Maui.ViewModels;

/// <summary>
/// DownloadListViewModel — 下载列表管理。
/// </summary>
public partial class DownloadListViewModel : ViewModelBase
{
    private readonly IDownloadHistoryService _history;
    private readonly IDownloadService _downloadService;

    [ObservableProperty]
    private ObservableCollection<DownloadRecord> _downloads = new();

    public DownloadListViewModel(IDownloadHistoryService history, IDownloadService downloadService)
    {
        _history = history;
        _downloadService = downloadService;

        _downloadService.DownloadProgressChanged += OnDownloadProgress;
        _downloadService.DownloadCompleted += OnDownloadCompleted;
        _history.RecordChanged += OnRecordChanged;
    }

    [RelayCommand]
    private async Task LoadDownloadsAsync()
    {
        var records = await _history.GetAllAsync();
        Downloads = new ObservableCollection<DownloadRecord>(records);
    }

    private void OnDownloadProgress(object? sender, DownloadRecord record)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var existing = Downloads.FirstOrDefault(d => d.Id == record.Id);
            if (existing != null)
            {
                var idx = Downloads.IndexOf(existing);
                if (idx >= 0)
                    Downloads[idx] = record;
            }
            else if (!Downloads.Any(d => d.Id == record.Id))
            {
                Downloads.Insert(0, record);
            }
        });
    }

    private void OnDownloadCompleted(object? sender, DownloadRecord record)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var existing = Downloads.FirstOrDefault(d => d.Id == record.Id);
            if (existing != null)
            {
                var idx = Downloads.IndexOf(existing);
                if (idx >= 0)
                    Downloads[idx] = record;
            }
        });
    }

    private void OnRecordChanged(object? sender, DownloadRecord record)
    {
        MainThread.BeginInvokeOnMainThread(async () => await LoadDownloadsAsync());
    }

    [RelayCommand]
    private async Task DeleteDownloadAsync(DownloadRecord? record)
    {
        if (record == null) return;
        await _history.DeleteAsync(record.Id);
    }

    [RelayCommand]
    private async Task ClearAllAsync()
    {
        await _history.ClearAsync();
        Downloads.Clear();
    }
}
