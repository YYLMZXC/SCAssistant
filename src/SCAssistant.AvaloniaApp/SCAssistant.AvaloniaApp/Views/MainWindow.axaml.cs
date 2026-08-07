using System;
using Avalonia.Controls;
using Avalonia.Input;
using SCAssistant.AvaloniaApp.Services;
using SCAssistant.AvaloniaApp.ViewModels;

namespace SCAssistant.AvaloniaApp.Views;

/// <summary>
/// 桌面端主窗口 — 用于 Windows/Linux/macOS 经典桌面生命周期。
/// 窗口加载完成后初始化浏览器视图区域。
/// 地址栏事件（回车导航）在后台代码中处理。
/// </summary>
public partial class MainWindow : Window
{
    private readonly IBrowserProvider _browser;
    private readonly MainViewModel? _mainVm;

    public MainWindow(IBrowserProvider browser, SettingsViewModel settingsVm)
    {
        _browser = browser;
        InitializeComponent();
        // 设置面板绑定到 SettingsViewModel
        SettingsPanel.DataContext = settingsVm;
        Loaded += OnLoaded;
        LogHelper.Info("[MainWindow] 桌面窗口构造完成");
    }

    /// <summary>窗口加载完成后初始化浏览器区域。</summary>
    private void OnLoaded(object? sender, EventArgs e)
    {
        LogHelper.Info("[MainWindow] 窗口加载 — 初始化浏览器区域");
        BrowserArea.Initialize(_browser);
        LogHelper.Info("[MainWindow] 浏览器区域初始化完成");
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
