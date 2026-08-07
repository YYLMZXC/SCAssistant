using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCAssistant.AvaloniaApp.Models;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.ViewModels;

/// <summary>
/// 下载列表 ViewModel — 管理下载记录集合。
/// </summary>
public partial class DownloadListViewModel : ViewModelBase
{
    private readonly IDownloadHistoryService _historyService;

    [ObservableProperty]
    private ObservableCollection<DownloadRecord> _records = new();

    [ObservableProperty]
    private string _downloadUrl = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public DownloadListViewModel(IDownloadHistoryService historyService)
    {
        _historyService = historyService;
    }

    public async Task LoadAsync()
    {
        var records = await _historyService.GetRecordsAsync();
        Records = new ObservableCollection<DownloadRecord>(records);
    }

    [RelayCommand]
    private async Task ClearAll()
    {
        await _historyService.ClearAllAsync();
        Records.Clear();
        LogHelper.Info("[DownloadListVM] 记录已清空");
    }
}
