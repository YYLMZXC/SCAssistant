using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;

namespace SCAssistant.UnoApp.Droid;

[Activity(
    MainLauncher = true,
    ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = SoftInput.AdjustNothing | SoftInput.StateHidden
)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
    /// <summary>
    /// 刘海屏安全区顶部高度 (pixels)，由 WindowInsets 回调更新。
    /// </summary>
    public static float SafeAreaTopPixels { get; private set; }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // Android WebView 空白页排查：启用远程调试
        // 可通过 Chrome 地址栏 chrome://inspect 连接到设备实时查看 WebView 状态
#if DEBUG
        Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
        Android.Util.Log.Info("SCAssistant", "WebView 远程调试已启用");
#endif

        global::AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);

        base.OnCreate(savedInstanceState);

        // 刘海屏 / 挖孔屏 安全区适配：通过 WindowInsets 获取真实顶部安全高度
        SetupSafeAreaTracking();
    }

    private void SetupSafeAreaTracking()
    {
        try
        {
            var decorView = Window?.DecorView;
            if (decorView == null) return;

            // 立即同步读取当前 WindowInsets（可能已经就绪）
            var currentInsets = ViewCompat.GetRootWindowInsets(decorView);
            if (currentInsets != null)
            {
                UpdateSafeArea(currentInsets);
            }

            // 注册监听器处理后续 insets 变化
            ViewCompat.SetOnApplyWindowInsetsListener(decorView, new InsetsListener());
        }
        catch (System.Exception ex)
        {
            Android.Util.Log.Warn("SCAssistant", $"SafeArea 监听设置失败: {ex.Message}");
        }
    }

    private static void UpdateSafeArea(WindowInsetsCompat insets)
    {
        var statusBars = insets.GetInsets(WindowInsetsCompat.Type.StatusBars());
        var cutout = insets.GetInsets(WindowInsetsCompat.Type.DisplayCutout());
        var systemBars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());

        SafeAreaTopPixels = Math.Max(Math.Max(statusBars.Top, cutout.Top), systemBars.Top);
        Android.Util.Log.Info("SCAssistant",
            $"SafeArea: statusBars={statusBars.Top}, cutout={cutout.Top}, systemBars={systemBars.Top}, final={SafeAreaTopPixels}px");
    }

    private class InsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        public WindowInsetsCompat? OnApplyWindowInsets(View? v, WindowInsetsCompat? insets)
        {
            if (insets != null)
                UpdateSafeArea(insets);
            return insets;
        }
    }
}
