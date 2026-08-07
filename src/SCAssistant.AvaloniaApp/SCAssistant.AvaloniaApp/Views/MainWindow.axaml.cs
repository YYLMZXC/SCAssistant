using System;
using Avalonia.Controls;
using Avalonia.Input;
using SCAssistant.AvaloniaApp.Services;
using SCAssistant.AvaloniaApp.ViewModels;

namespace SCAssistant.AvaloniaApp.Views;

public partial class MainWindow : Window
{
    private readonly IBrowserProvider _browser;

    public MainWindow(IBrowserProvider browser, SettingsViewModel settingsVm)
    {
        _browser = browser;
        InitializeComponent();
        SettingsPanel.DataContext = settingsVm;
        Loaded += OnLoaded;
        LogHelper.Info("[MainWindow] 桌面窗口构造完成");
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        LogHelper.Info("[MainWindow] 窗口加载 — 初始化浏览器区域");
        BrowserArea.Initialize(_browser);
        LogHelper.Info("[MainWindow] 浏览器区域初始化完成");
    }

    private void AddressBar_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is MainViewModel vm)
        {
            LogHelper.Debug("[MainWindow] 地址栏回车");
            vm.NavigateToUrlCommand.Execute(null);
        }
    }
}
