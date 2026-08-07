using Foundation;
using UIKit;
using Avalonia;
using Avalonia.iOS;

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

    /// <summary>
    /// 应用启动完成后，确保窗口延伸到状态栏和底部指示条区域，
    /// 以便 Avalonia 的 InsetsManager 能正确读取安全区域边距。
    /// </summary>
    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        var result = base.FinishedLaunching(application, launchOptions);

        // 确保窗口内容延伸到安全区域之外，由布局层通过 Margin 避开
        if (Window != null)
        {
            Window.Frame = UIScreen.MainScreen.Bounds;
            Window.BackgroundColor = UIColor.White;
        }

        return result;
    }
}