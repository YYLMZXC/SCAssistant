using SCAssistant.AvaloniaApp.Services;
using SCAssistant.AvaloniaApp.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;

namespace SCAssistant.AvaloniaApp.Views;

/// <summary>
/// 移动端主视图 — 用于 Android/iOS 单视图生命周期。
/// 视图加载完成后初始化浏览器区域。
/// 地址栏事件（回车导航）在后台代码中处理。
/// </summary>
public partial class MainView : UserControl
{
    private readonly IBrowserProvider _browser;

    public MainView(IBrowserProvider browser, SettingsViewModel settingsVm)
    {
        _browser = browser;
        InitializeComponent();
        // 设置面板绑定到 SettingsViewModel
        SettingsPanel.DataContext = settingsVm;
        Loaded += OnLoaded;
        LogHelper.Info("[MainView] 移动端视图构造完成");
    }

    /// <summary>视图加载完成后初始化浏览器区域。</summary>
    private void OnLoaded(object? sender, System.EventArgs e)
    {
        LogHelper.Info("[MainView] 视图加载 — 初始化浏览器区域");
        BrowserArea.Initialize(_browser);
        LogHelper.Info("[MainView] 浏览器区域初始化完成");
    }

    /// <summary>地址栏回车键 — 触发导航。</summary>
    private void AddressTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.NavigateToUrlCommand.Execute(AddressTextBox.Text);
            }
            e.Handled = true;
        }
    }
}
