using Foundation;
using UIKit;
using Avalonia;
using Avalonia.iOS;

namespace SCAssistant.AvaloniaApp.iOS;

/// <summary>
/// iOS AppDelegate — Avalonia 应用代理。
/// 配置 Inter 字体和 iOS 平台初始化。
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
}