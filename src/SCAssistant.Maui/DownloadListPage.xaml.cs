using SCAssistant.Maui.Models;
using SCAssistant.Maui.Services;

namespace SCAssistant.Maui;

public partial class DownloadListPage : ContentPage
{
    private DownloadRecord? _selectedRecord;

    public DownloadListPage()
    {
        InitializeComponent();
        DownloadHistoryService.Instance.HistoryChanged += OnHistoryChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadHistoryAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        DownloadHistoryService.Instance.HistoryChanged -= OnHistoryChanged;
    }

    private async Task LoadHistoryAsync()
    {
        await DownloadHistoryService.Instance.LoadHistoryAsync();
        UpdateCollection();
    }

    private void OnHistoryChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(UpdateCollection);
    }

    private void UpdateCollection()
    {
        var history = DownloadHistoryService.Instance.History;
        DownloadCollection.ItemsSource = history;
        EmptyState.IsVisible = !history.Any();
        DownloadCollection.IsVisible = history.Any();
    }

    private void OnDownloadSelected(object? sender, SelectionChangedEventArgs e)
    {
        _selectedRecord = DownloadCollection.SelectedItem as DownloadRecord;
        OpenFileBtn.IsEnabled = _selectedRecord != null && !string.IsNullOrEmpty(_selectedRecord.LocalPath);
        DeleteBtn.IsEnabled = _selectedRecord != null;
    }

    private async void OnOpenFileClicked(object? sender, EventArgs e)
    {
        if (_selectedRecord == null || string.IsNullOrEmpty(_selectedRecord.LocalPath))
            return;

        try
        {
            if (File.Exists(_selectedRecord.LocalPath))
            {
                await Launcher.Default.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(_selectedRecord.LocalPath)
                });
            }
            else
            {
                await DisplayAlert("提示", "文件不存在", "确定");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("错误", $"无法打开文件: {ex.Message}", "确定");
        }
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (_selectedRecord == null)
            return;

        var result = await DisplayAlert("确认", "确定要删除选中的下载记录吗？", "删除", "取消");
        if (result)
        {
            await DownloadHistoryService.Instance.RemoveRecordAsync(_selectedRecord);
            _selectedRecord = null;
            OpenFileBtn.IsEnabled = false;
            DeleteBtn.IsEnabled = false;
        }
    }
}
