using System;
using Avalonia.Controls;
using SCAssistant.AvaloniaApp.Services;
using SCAssistant.AvaloniaApp.ViewModels;

namespace SCAssistant.AvaloniaApp.Views;

/// <summary>
/// 共享主布局 — 桌面端和移动端复用同一套 UI（顶栏地址栏、中部内容区、底部标签栏）。
/// 使用编译期绑定确保所有按钮命令都能正确解析。
/// </summary>
public partial class MainLayout : UserControl
{
    private readonly IBrowserProvider _browser;

    public MainLayout(IBrowserProvider browser, SettingsViewModel settingsVm, AddressBarViewModel addressBarVm)
    {
        _browser = browser;
        InitializeComponent();
        SettingsPanel.DataContext = settingsVm;
        AddressBar.DataContext = addressBarVm;
        Loaded += OnLoaded;
        LogHelper.Info("[MainLayout] 布局构造完成");
    }

    /// <summary>布局加载完成后初始化浏览器区域。</summary>
    private void OnLoaded(object? sender, EventArgs e)
    {
        LogHelper.Info("[MainLayout] 布局加载 — 初始化浏览器区域");
        BrowserArea.Initialize(_browser);
        LogHelper.Info("[MainLayout] 浏览器区域初始化完成");
    }
}
