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
        LogHelper.Info($"[MainLayout] 构造函数开始 — addressBarVm 类型={addressBarVm.GetType().Name}");

        InitializeComponent();
        LogHelper.Info($"[MainLayout] InitializeComponent 完成 — AddressBar 控件={(AddressBar != null ? "存在" : "NULL!")}");

        // 设置 AddressBarView 的 DataContext
        AddressBar.DataContext = addressBarVm;
        LogHelper.Info($"[MainLayout] AddressBar.DataContext 已设置为 AddressBarViewModel — " +
            $"AddressBar.IsVisible={AddressBar.IsVisible}, Bounds={AddressBar.Bounds}");

        SettingsPanel.DataContext = settingsVm;
        LogHelper.Info("[MainLayout] SettingsPanel.DataContext 已设置");

        Loaded += OnLoaded;
        LogHelper.Info("[MainLayout] 布局构造完成");
    }

    /// <summary>布局加载完成后初始化浏览器区域。</summary>
    private void OnLoaded(object? sender, EventArgs e)
    {
        LogHelper.Info($"[MainLayout] Loaded — AddressBar.IsVisible={AddressBar.IsVisible}, " +
            $"AddressBar.Bounds={AddressBar.Bounds}, " +
            $"AddressBar.DataContext={(AddressBar.DataContext != null ? AddressBar.DataContext.GetType().Name : "null")}, " +
            $"自己的 DataContext={(DataContext != null ? DataContext.GetType().Name : "null")}");

        LogHelper.Info("[MainLayout] 布局加载 — 初始化浏览器区域");
        BrowserArea.Initialize(_browser);
        LogHelper.Info("[MainLayout] 浏览器区域初始化完成");
    }
}
