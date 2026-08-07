using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.ViewModels;

/// <summary>
/// 主页 ViewModel — 显示欢迎页及常用快捷链接。
/// </summary>
public partial class HomeViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    [RelayCommand]
    private void Search(string? query)
    {
        LogHelper.Info($"[HomeVM] 搜索: {query}");
    }

    [RelayCommand]
    private void NavigateQuickLink(string? url)
    {
        if (!string.IsNullOrWhiteSpace(url))
        {
            LogHelper.Info($"[HomeVM] 快速导航: {url}");
        }
    }
}
