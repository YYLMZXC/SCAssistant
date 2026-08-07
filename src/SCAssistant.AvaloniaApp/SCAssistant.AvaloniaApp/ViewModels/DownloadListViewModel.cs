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

    /// <summary>下载记录集合（UI 绑定数据源）。</summary>
    [ObservableProperty]
    private ObservableCollection<DownloadRecord> _records = new();

    /// <summary>当前下载 URL。</summary>
    [ObservableProperty]
    private string _downloadUrl = string.Empty;

    /// <summary>是否正在加载历史记录。</summary>
    [ObservableProperty]
    private bool _isLoading;

    public DownloadListViewModel(IDownloadHistoryService historyService)
    {
        _historyService = historyService;
    }

    /// <summary>从持久化存储加载下载历史记录。</summary>
    public async Task LoadAsync()
    {
        var records = await _historyService.GetRecordsAsync();
        Records = new ObservableCollection<DownloadRecord>(records);
    }

    /// <summary>清空所有下载历史记录（含持久化存储）。</summary>
    [RelayCommand]
    private async Task ClearAll()
    {
        await _historyService.ClearAllAsync();
        Records.Clear();
        LogHelper.Info("[DownloadListVM] 记录已清空");
    }
}
