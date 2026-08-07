using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
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
/// 注册 Android 原生 WebView 浏览器控件工厂，并启用全面屏/刘海屏适配。
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

        // 启用边到边显示 — 内容延伸到状态栏和导航栏区域
        // 配合 MainView 中的 SafeAreaMargin 处理，确保顶栏和底栏不被系统UI遮挡
        EnableEdgeToEdge();

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

    /// <summary>
    /// 启用 Android 边到边显示模式。
    /// 让应用内容绘制在系统栏（状态栏、导航栏）下方，
    /// Avalonia 的 InsetsManager 会根据 WindowInsets 提供安全区域边距。
    /// </summary>
    private void EnableEdgeToEdge()
    {
        if (Window != null)
        {
            // 让窗口内容延伸到系统栏区域
            WindowCompat.SetDecorFitsSystemWindows(Window, false);

            // 设置系统栏为透明，让应用内容可见于系统栏之下
            Window.SetFlags(
                WindowManagerFlags.TranslucentStatus | WindowManagerFlags.TranslucentNavigation,
                WindowManagerFlags.TranslucentStatus | WindowManagerFlags.TranslucentNavigation);

            // 允许内容延伸到刘海屏/挖孔区域（Android 9+）
            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
            }

            // 确保系统栏图标在浅色背景上可读（暗色图标）
            var controller = new WindowInsetsControllerCompat(Window, Window.DecorView);
            controller.AppearanceLightStatusBars = true;
            controller.AppearanceLightNavigationBars = true;
        }
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