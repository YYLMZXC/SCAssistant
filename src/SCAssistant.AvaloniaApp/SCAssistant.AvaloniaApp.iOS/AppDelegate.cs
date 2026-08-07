using Avalonia;
using Avalonia.iOS;
using Foundation;

namespace SCAssistant.AvaloniaApp.iOS;

/// <summary>
/// iOS AppDelegate — Avalonia 应用代理。
/// 配置 Inter 字体和 iOS 平台初始化，确保刘海屏/灵动岛安全区域适配。
/// </summary>
[Register("AppDelegate")]
#pragma warning disable CA1711
public partial class AppDelegate : AvaloniaAppDelegate<App>
#pragma warning restore CA1711
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }

    // 注意（Avalonia 12 迁移）：
    // Avalonia 12 的 AvaloniaAppDelegate<TApp>.FinishedLaunching 不再是 virtual/override，
    // 且 iOS 启动改为基于 scenes 的生命周期——启动后 AvaloniaAppDelegate.Window 保持为 null，
    // 此前在此处设置 Window.Frame / Window.BackgroundColor 已不再生效。
    // 基类 FinishedLaunching 会自动调用上面的 CustomizeAppBuilder 完成初始化。
    // 安全区域（刘海屏/灵动岛/底部指示条）由 MainView 通过 TopLevel.InsetsManager
    // 读取 SafeAreaPadding 并应用到 MainLayout.SafeAreaMargin，无需在此处处理。
}