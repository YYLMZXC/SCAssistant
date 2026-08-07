using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using Avalonia.Controls;
using SCAssistant.AvaloniaApp.Services;
using SCAssistant.AvaloniaApp.Views;

namespace SCAssistant.AvaloniaApp.Android;

[Activity(
    Label = "SCAssistant.AvaloniaApp.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    /// <summary>
    /// 当前 Activity 实例，供 WebViewBrowserControl 使用。
    /// </summary>
    public static MainActivity? CurrentActivity { get; private set; }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        CurrentActivity = this;

        // 注册 Android 平台浏览器控件工厂
        BrowserView.BrowserControlFactory = provider =>
        {
            var webView = new WebViewBrowserControl();
            if (provider is BrowserProvider browserProvider)
            {
                browserProvider.SetPlatformWebView(webView);
                webView.AddressChanged += (_, url) => browserProvider.HandlePlatformAddressChanged(url);
                webView.TitleChanged += (_, title) => browserProvider.HandlePlatformTitleChanged(title);
                webView.LoadingStateChanged += (_, loading) => browserProvider.HandlePlatformLoadingStateChanged(loading);
                webView.DownloadRequested += (_, url) => browserProvider.HandlePlatformDownloadRequested(url);
                webView.NavigationHistoryChanged += (_, _) => browserProvider.HandlePlatformNavigationHistoryChanged();
            }
            return webView;
        };
    }

    protected override void OnDestroy()
    {
        if (CurrentActivity == this)
        {
            CurrentActivity = null;
        }
        base.OnDestroy();
    }
}