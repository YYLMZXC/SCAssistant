using System;
using Avalonia;
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

    /// <summary>
    /// 安全区域边距（用于移动端刘海屏/灵动岛/底部指示条适配）。
    /// 桌面端此值为 (0,0,0,0)。
    /// </summary>
    public static readonly StyledProperty<Thickness> SafeAreaMarginProperty =
        AvaloniaProperty.Register<MainLayout, Thickness>(nameof(SafeAreaMargin));

    /// <summary>
    /// 获取或设置安全区域边距。
    /// 由 MainView（移动端）在布局加载完成后从平台 InsetsManager 读取并设置。
    /// </summary>
    public Thickness SafeAreaMargin
    {
        get => GetValue(SafeAreaMarginProperty);
        set => SetValue(SafeAreaMarginProperty, value);
    }

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

        ApplySafeAreaPadding();

        LogHelper.Info("[MainLayout] 布局加载 — 初始化浏览器区域");
        BrowserArea.Initialize(_browser);
        LogHelper.Info("[MainLayout] 浏览器区域初始化完成");
    }

    /// <summary>
    /// SafeArea 变化时，将安全区域边距应用到顶栏/底栏的 Padding（而非整个 Grid 的 Margin）。
    /// 这样顶栏/底栏背景延伸到屏幕边缘，填充刘海/home indicator 区域，仅其内容向内避开，
    /// 避免 Grid 整体缩进在顶部/底部留出大片空白。
    /// </summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SafeAreaMarginProperty)
        {
            ApplySafeAreaPadding();
        }
    }

    /// <summary>
    /// 将 SafeAreaMargin 叠加到顶栏（Top）与底栏（Bottom）的 Padding。
    /// 顶栏基础 Padding=(6,4)，底栏基础 Padding=(4,4)；
    /// 桌面端 SafeAreaMargin=(0,0,0,0)，Padding 保持基础值不变。
    /// </summary>
    private void ApplySafeAreaPadding()
    {
        if (TopBar == null || BottomBar == null) return;
        var s = SafeAreaMargin;
        TopBar.Padding = new Thickness(6 + s.Left, 4 + s.Top, 6 + s.Right, 4);
        BottomBar.Padding = new Thickness(4 + s.Left, 4, 4 + s.Right, 4 + s.Bottom);
        LogHelper.Info($"[MainLayout] 应用 SafeArea 到顶栏/底栏 Padding — SafeArea={s}");
    }
}
