using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace SCAssistant.AvaloniaApp.Android
{
    /// <summary>
    /// Android Application 类 — Avalonia 应用入口。
    /// 配置 Inter 字体和 Android 平台初始化。
    /// </summary>
    [Application]
    public class Application : AvaloniaAndroidApplication<App>
    {
        protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            return base.CustomizeAppBuilder(builder)
                .WithInterFont();
        }
    }
}