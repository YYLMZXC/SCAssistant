using System;
using Avalonia;
using Avalonia.Controls;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.Views;

/// <summary>
/// 移动端主视图 — 用于 Android/iOS 单视图生命周期。
/// 实际的 UI 布局由共享的 MainLayout 提供。
/// 
/// 安全区域策略说明（2026-08-07 调整）：
/// - 保留 AutoSafeAreaPadding=false，防止 Avalonia TopLevel 自动给内容施加一次 SafeArea Padding。
/// - 不再将 InsetsManager.SafeAreaPadding 全量传递给 MainLayout.SafeAreaMargin，
///   因为 iOS 平台的 AvaloniaView 通常已在 UIKit 层受 SafeAreaLayoutGuide 约束（内容已避开刘海/指示条），
///   若再叠加一次 SafeArea Padding 会导致双重缩进，顶部/底部出现大片空白。
/// - 仅对 Top/Bottom 做小幅裁剪避让（最多 10px/8px），兼顾避免状态栏轻微遮挡与不出现过大空白。
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
    /// 视图加载完成后，关闭 Avalonia 自动 SafeArea 缩进，
    /// 并按"小幅避让"策略计算 SafeAreaMargin，避免双重缩进带来的大片空白。
    /// </summary>
    private void OnLoaded(object? sender, EventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            // 保留：禁用 Avalonia 内置的自动 SafeArea 缩进，避免与 UIKit 约束/我们自定义策略叠加。
            topLevel.SetValue(TopLevel.AutoSafeAreaPaddingProperty, false);

            var insetsManager = topLevel.InsetsManager;
            if (insetsManager != null)
            {
                insetsManager.SafeAreaChanged += OnSafeAreaChanged;
                LogHelper.Info($"[MainView] 已订阅 SafeAreaChanged — 原始安全区域={insetsManager.SafeAreaPadding}");
            }
        }

        ApplySafeArea();
    }

    /// <summary>
    /// 安全区域变化时（旋转屏幕、状态栏变化等），重新计算裁剪后的边距。
    /// </summary>
    private void OnSafeAreaChanged(object? sender, EventArgs e)
    {
        ApplySafeArea();
    }

    /// <summary>
    /// 从 InsetsManager 读取原始 SafeArea，按"小幅避让"策略裁剪后赋值给 MainLayout.SafeAreaMargin：
    /// - Left/Right：原样保留（横屏时两侧可能有轻微避让需求）
    /// - Top：仅取 Math.Min(原始值, 10)，避免全量 SafeArea.Top(≈59px) 导致顶栏上方巨大空白
    /// - Bottom：仅取 Math.Min(原始值, 8)，避免全量 SafeArea.Bottom(≈34px) 导致底栏下方巨大空白
    /// 如果平台 AvaloniaView 已在 UIKit 层避开 SafeArea，则原始 SafeArea 通常接近 0，裁剪后仍为 0（无影响）。
    /// </summary>
    private void ApplySafeArea()
    {
        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            var insetsManager = topLevel.InsetsManager;
            if (insetsManager != null)
            {
                var raw = insetsManager.SafeAreaPadding;
                var clipped = new Thickness(
                    raw.Left,
                    Math.Min(raw.Top, 10),
                    raw.Right,
                    Math.Min(raw.Bottom, 8));
                _mainLayout.SafeAreaMargin = clipped;
                LogHelper.Info($"[MainView] 应用安全区域（已裁剪防空白）— 原始={raw}，裁剪后={clipped}");
            }
        }
    }
}
