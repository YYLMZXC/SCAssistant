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
/// <summary>
/// Android 主 Activity — Avalonia 应用入口。
/// 注册 Android 原生 WebView 浏览器控件工厂。
/// </summary>
public class MainActivity : AvaloniaMainActivity
{
    /// <summary>
    /// 当前 Activity 实例（静态引用），供 WebViewBrowserControl 获取 Activity 上下文。
    /// </summary>
    public static MainActivity? CurrentActivity { get; private set; }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        CurrentActivity = this;

        // 注册 Android 平台浏览器控件工厂
        // 此工厂负责创建 Android.Webkit.WebView 控件并桥接所有事件到 BrowserProvider
        BrowserView.BrowserControlFactory = provider =>
        {
            var webView = new WebViewBrowserControl();
            if (provider is BrowserProvider browserProvider)
            {
                browserProvider.SetPlatformWebView(webView);

                // 等待平台 WebView 初始化完成后再标记就绪
                if (!webView.IsReady)
                {
                    webView.ReadyChanged += (_, _) =>
                    {
                        browserProvider.MarkPlatformReady();
                    };
                }

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