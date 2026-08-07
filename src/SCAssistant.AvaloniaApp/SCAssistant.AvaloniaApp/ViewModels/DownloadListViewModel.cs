using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCAssistant.AvaloniaApp.Models;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.ViewModels;

/// <summary>
/// 下载列表面板视图模型
/// </summary>
public partial class DownloadListViewModel : ViewModelBase
{
    private readonly IDownloadHistoryService _downloadHistoryService;
    private readonly IDownloadService _downloadService;

    [ObservableProperty]
    private ObservableCollection<DownloadRecord> _records = new();

    [ObservableProperty]
    private bool _isEmpty = true;

    public DownloadListViewModel(
        IDownloadHistoryService downloadHistoryService,
        IDownloadService downloadService)
    {
        _downloadHistoryService = downloadHistoryService;
        _downloadService = downloadService;
        Title = "下载列表";
    }

    public async Task InitializeAsync()
    {
        await LoadRecordsAsync();
    }

    [RelayCommand]
    private async Task LoadRecordsAsync()
    {
        try
        {
            var items = await _downloadHistoryService.GetRecordsAsync();
            Records = new ObservableCollection<DownloadRecord>(items);
            IsEmpty = Records.Count == 0;
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex, "DownloadList");
        }
    }

    [RelayCommand]
    private async Task ClearAll()
    {
        await _downloadHistoryService.ClearAllAsync();
        Records.Clear();
        IsEmpty = true;
    }

    [RelayCommand]
    private async Task DeleteRecord(DownloadRecord? record)
    {
        if (record == null) return;

        await _downloadHistoryService.DeleteRecordAsync(record.Url);
        Records.Remove(record);
        IsEmpty = Records.Count == 0;
    }

    [RelayCommand]
    private async Task CancelDownload(DownloadRecord? record)
    {
        if (record == null) return;

        await _downloadService.CancelDownloadAsync(record.Url);
        record.Status = "Cancelled";
        await _downloadHistoryService.UpdateRecordAsync(record);
    }

    [RelayCommand]
    private static async Task OpenFile(DownloadRecord? record)
    {
        if (record == null || string.IsNullOrEmpty(record.FilePath)) return;
        await SystemBrowserProvider.OpenUrlAsync(record.FilePath);
    }
}
