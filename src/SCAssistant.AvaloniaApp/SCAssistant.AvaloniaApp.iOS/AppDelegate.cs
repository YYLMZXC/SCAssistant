using Foundation;
using UIKit;
using Avalonia;
using Avalonia.Controls;
using Avalonia.iOS;
using Avalonia.Media;
using SCAssistant.AvaloniaApp.iOS.Services;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.iOS;

// The UIApplicationDelegate for the application. This class is responsible for launching the 
// User Interface of the application, as well as listening (and optionally responding) to 
// application events from iOS.
[Register("AppDelegate")]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public partial class AppDelegate : AvaloniaAppDelegate<App>
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        // 在 UI 初始化前注册 iOS 原生 WKWebView 浏览器
        ServiceLocator.BrowserProvider = new iOSBrowserProvider();
        ServiceLocator.DownloadHistory = new DownloadHistoryService();
        ServiceLocator.DownloadHistory.Load();

        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}
