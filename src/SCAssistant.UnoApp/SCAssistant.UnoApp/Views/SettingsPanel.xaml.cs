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
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        LogHelper.Info("[设置面板] 关闭按钮点击");
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void BrowserSettingsTab_Click(object sender, RoutedEventArgs e)
    {
        LogHelper.Info("[设置面板] 切换到浏览器设置标签");
        SetActiveTab(0);
        if (DataContext is SettingsViewModel vm)
            vm.SelectedTabIndex = 0;
    }

    private void DownloadTab_Click(object sender, RoutedEventArgs e)
    {
        LogHelper.Info("[设置面板] 切换到下载管理标签");
        SetActiveTab(1);
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
