using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace SCAssistant.UnoApp.Droid;

[Activity(
    MainLauncher = true,
    ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = SoftInput.AdjustNothing | SoftInput.StateHidden
)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
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
    }
}
