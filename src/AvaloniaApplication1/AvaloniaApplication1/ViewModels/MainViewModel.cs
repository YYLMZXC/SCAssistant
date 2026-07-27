using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AvaloniaApplication1.Services;

namespace AvaloniaApplication1.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IBrowserProvider _browser;

    [ObservableProperty]
    public partial string CurrentUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CurrentTitle { get; set; } = "SCAssistant";

    [ObservableProperty]
    public partial bool IsBrowserLoading { get; set; }

    private const string HomeUrl = "https://test.suancaixianyu.cn/";
    private const string SCKeyUrl = "https://www.sckey.net";
    private const string SCWZUrl = "https://scwz.top/";

    public MainViewModel()
    {
        _browser = ServiceLocator.BrowserProvider;
        _browser.AddressChanged += (_, url) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => CurrentUrl = url);
        };
        _browser.TitleChanged += (_, title) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => CurrentTitle = title);
        };
        _browser.LoadingStateChanged += (_, loading) =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => IsBrowserLoading = loading);
        };
    }

    public void NavigateToHome()
    {
        _browser.Initialize(HomeUrl);
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
        var win = new Views.DownloadListWindow
        {
            DataContext = new DownloadListViewModel()
        };
        win.Show();
    }
}
