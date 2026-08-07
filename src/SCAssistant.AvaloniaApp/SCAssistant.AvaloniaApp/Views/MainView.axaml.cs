using System;
using Avalonia.Controls;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.Views;

/// <summary>
/// 移动端主视图 — 用于 Android/iOS 单视图生命周期。
/// 实际的 UI 布局由共享的 MainLayout 提供。
/// 负责从平台 InsetsManager 获取安全区域（刘海屏/底部指示条），传递给 MainLayout。
/// </summary>
public partial class MainView : UserControl
{
    private readonly MainLayout _mainLayout;

    public MainView(MainLayout mainLayout)
    {
        InitializeComponent();
        _mainLayout = mainLayout;
        Content = mainLayout;

        Loaded += OnLoaded;
        LogHelper.Info("[MainView] 移动端视图构造完成");
    }

    /// <summary>
    /// 视图加载完成后，从平台 InsetsManager 获取安全区域边距，
    /// 并监听变化以自动适配刘海屏、灵动岛、底部指示条等。
    /// </summary>
    private void OnLoaded(object? sender, EventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            // 禁用 Avalonia 内置的自动 SafeArea 缩进（AutoSafeAreaPadding 默认 true）。
            // 否则 Avalonia 给 TopLevel 内容加的 safe area padding 会与 MainLayout 自身的
            // SafeAreaMargin 叠加（双重缩进）。在 iOS 上这会导致原生 WKWebView 的 Frame
            // 坐标与 Avalonia 视觉树错位：WKWebView 上移覆盖顶部地址栏，并在底部留出空白。
            // 改为由 MainLayout 通过 SafeAreaMargin 统一单次管理安全区域。
            topLevel.SetValue(TopLevel.AutoSafeAreaPaddingProperty, false);

            var insetsManager = topLevel.InsetsManager;
            if (insetsManager != null)
            {
                insetsManager.SafeAreaChanged += OnSafeAreaChanged;
                LogHelper.Info($"[MainView] 已订阅 SafeAreaChanged — 当前安全区域={insetsManager.SafeAreaPadding}");
            }
        }

        ApplySafeArea();
    }

    /// <summary>
    /// 安全区域变化时，重新读取并传递给 MainLayout。
    /// 旋转屏幕、状态栏变化等场景均会触发此回调。
    /// </summary>
    private void OnSafeAreaChanged(object? sender, EventArgs e)
    {
        ApplySafeArea();
    }

    /// <summary>
    /// 从 TopLevel.InsetsManager 读取安全区域边距，设置到 MainLayout 的 SafeAreaMargin。
    /// SafeAreaPadding 返回的 Thickness 中，Top 对应状态栏/刘海区域，Bottom 对应底部指示条区域。
    /// </summary>
    private void ApplySafeArea()
    {
        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            var insetsManager = topLevel.InsetsManager;
            if (insetsManager != null)
            {
                _mainLayout.SafeAreaMargin = insetsManager.SafeAreaPadding;
                LogHelper.Info($"[MainView] 更新安全区域边距 — {_mainLayout.SafeAreaMargin}");
            }
        }
    }
}
