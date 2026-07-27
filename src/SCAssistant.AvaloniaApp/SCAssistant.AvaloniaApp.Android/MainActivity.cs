using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using SCAssistant.AvaloniaApp.Android.Services;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.Android;

[Activity(
    Label = "SCAssistant.AvaloniaApp.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    protected override void OnCreate(global::Android.OS.Bundle? savedInstanceState)
    {
        // 在 UI 初始化前注册 Android 原生 WebView 浏览器
        ServiceLocator.BrowserProvider = new AndroidBrowserProvider();
        ServiceLocator.DownloadHistory = new DownloadHistoryService();
        ServiceLocator.DownloadHistory.Load();

        base.OnCreate(savedInstanceState);
    }
}
