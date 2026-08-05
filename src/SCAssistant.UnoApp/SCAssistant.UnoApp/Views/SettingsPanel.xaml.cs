using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SCAssistant.UnoApp.Services;
using SCAssistant.UnoApp.ViewModels;

namespace SCAssistant.UnoApp.Views;

public partial class SettingsPanel : UserControl
{
    /// <summary>外部订阅此事件以响应关闭操作。</summary>
    public event EventHandler? CloseRequested;

    private readonly SolidColorBrush _tabSelectedBg;
    private readonly SolidColorBrush _tabUnselectedBg;
    private readonly SolidColorBrush _tabSelectedFg;
    private readonly SolidColorBrush _tabUnselectedFg;

    public SettingsPanel()
    {
        InitializeComponent();
        LogHelper.Info("[设置面板] 已构造");

        _tabSelectedBg = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
        _tabUnselectedBg = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        _tabSelectedFg = new SolidColorBrush(Microsoft.UI.Colors.White);
        var mediumColor = (Windows.UI.Color)Application.Current.Resources["SystemBaseMediumColor"];
        _tabUnselectedFg = new SolidColorBrush(mediumColor);

        // 监听 DataContext 变化以绑定 ViewModel 的 SelectedTabIndex
        DataContextChanged += OnDataContextChanged;
    }

    private SettingsViewModel? _currentVm;

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        // 解除旧 ViewModel 的绑定
        if (_currentVm is not null)
            _currentVm.PropertyChanged -= OnViewModelPropertyChanged;

        _currentVm = DataContext as SettingsViewModel;

        // 绑定新 ViewModel
        if (_currentVm is not null)
        {
            _currentVm.PropertyChanged += OnViewModelPropertyChanged;
            // 同步当前标签状态
            SetActiveTab(_currentVm.SelectedTabIndex);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.SelectedTabIndex) &&
            sender is SettingsViewModel vm)
        {
            SetActiveTab(vm.SelectedTabIndex);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        LogHelper.Info("[设置面板] 关闭按钮点击");
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void BrowserSettingsTab_Click(object sender, RoutedEventArgs e)
    {
        LogHelper.Info("[设置面板] 切换到浏览器设置标签");
        if (DataContext is SettingsViewModel vm)
            vm.SelectedTabIndex = 0;
    }

    private void DownloadTab_Click(object sender, RoutedEventArgs e)
    {
        LogHelper.Info("[设置面板] 切换到下载管理标签");
        if (DataContext is SettingsViewModel vm)
            vm.SelectedTabIndex = 1;
    }

    private void SetActiveTab(int index)
    {
        var isBrowser = index == 0;

        BrowserSettingsPanel.Visibility = isBrowser ? Visibility.Visible : Visibility.Collapsed;
        DownloadSettingsPanel.Visibility = isBrowser ? Visibility.Collapsed : Visibility.Visible;

        BrowserSettingsTab.Background = isBrowser ? _tabSelectedBg : _tabUnselectedBg;
        BrowserSettingsTab.Foreground = isBrowser ? _tabSelectedFg : _tabUnselectedFg;

        DownloadTab.Background = isBrowser ? _tabUnselectedBg : _tabSelectedBg;
        DownloadTab.Foreground = isBrowser ? _tabUnselectedFg : _tabSelectedFg;
    }
}
