using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using Avalonia;
using Avalonia.Android;

namespace SCAssistant.AvaloniaApp.Android;

[Activity(
    Label = "SCAssistant.AvaloniaApp.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
/// <summary>
/// Android 主 Activity（薄壳文件）。
/// WebView 控件实现与工厂注册已迁移到共享项目，
/// 此处仅保留全面屏适配与 CurrentActivity 静态引用（供 WebViewBrowserControl 查找 Context）。
/// </summary>
public class MainActivity : AvaloniaMainActivity
{
    /// <summary>
    /// 当前 Activity 实例（静态引用），供共享项目中 WebViewBrowserControl 通过反射获取。
    /// </summary>
    public static MainActivity? CurrentActivity { get; private set; }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        CurrentActivity = this;
        EnableEdgeToEdge();
        // 工厂注册已由共享项目 App.axaml.cs 统一处理
    }

    private void EnableEdgeToEdge()
    {
        if (Window != null)
        {
            WindowCompat.SetDecorFitsSystemWindows(Window, false);
            Window.SetFlags(
                WindowManagerFlags.TranslucentStatus | WindowManagerFlags.TranslucentNavigation,
                WindowManagerFlags.TranslucentStatus | WindowManagerFlags.TranslucentNavigation);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
            }
            var controller = new WindowInsetsControllerCompat(Window, Window.DecorView);
            controller.AppearanceLightStatusBars = true;
            controller.AppearanceLightNavigationBars = true;
        }
    }

    protected override void OnDestroy()
    {
        if (CurrentActivity == this) CurrentActivity = null;
        base.OnDestroy();
    }
}
