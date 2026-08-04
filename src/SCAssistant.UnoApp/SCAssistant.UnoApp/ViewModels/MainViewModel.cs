using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCAssistant.UnoApp.Services;

namespace SCAssistant.UnoApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IBrowserProvider _browser;

    [ObservableProperty]
    public partial string CurrentUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CurrentTitle { get; set; } = "SCAssistant";

    [ObservableProperty]
    public partial bool IsBrowserLoading { get; set; }

    [ObservableProperty]
    public partial bool IsDownloadListVisible { get; set; }

    [ObservableProperty]
    public partial bool IsInitialized { get; set; }

    public DownloadListViewModel DownloadList { get; }

    private const string HomeUrl = "https://test.suancaixianyu.cn/";
    private const string SCKeyUrl = "https://www.sckey.net";
    private const string SCWZUrl = "https://scwz.top/";

    public MainViewModel(IBrowserProvider browser, IDownloadHistoryService historyService)
    {
        _browser = browser;
        DownloadList = new DownloadListViewModel(historyService);

        _browser.AddressChanged += (_, url) =>
        {
            CurrentUrl = url;
        };
        _browser.TitleChanged += (_, title) =>
        {
            CurrentTitle = title;
        };
        _browser.LoadingStateChanged += (_, loading) =>
        {
            IsBrowserLoading = loading;
        };
    }

    public void NavigateToHome()
    {
        _browser.Initialize(HomeUrl);
    }

    public void InitializeBrowser(object window)
    {
        if (IsInitialized) return;
        IsInitialized = true;
        NavigateToHome();
    }

    [RelayCommand]
    private void NavigateHome()
    {
        _browser.Navigate(HomeUrl);
    }

    [RelayCommand]
    private void NavigateSCKey()
    {
        _browser.Navigate(SCKeyUrl);
    }

    [RelayCommand]
    private void NavigateSCWZ()
    {
        _browser.Navigate(SCWZUrl);
    }

    [RelayCommand]
    private void OpenDownloadList()
    {
        IsDownloadListVisible = !IsDownloadListVisible;
    }
}
