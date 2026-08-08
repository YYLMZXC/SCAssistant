using Microsoft.Extensions.Logging;
using SCAssistant.Maui.Services;
using SCAssistant.Maui.ViewModels;
using SCAssistant.Maui.Views;

namespace SCAssistant.Maui;

public static class MauiProgramExtensions
{
    public static MauiAppBuilder UseSharedMauiApp(this MauiAppBuilder builder)
    {
        // 注册 App
        builder.UseMauiApp<App>()
               .ConfigureFonts(fonts =>
               {
                   fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                   fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
               });

        // 注册服务
        builder.Services.AddSingleton<ILogService, DebugLogService>();
        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<IDownloadHistoryService, DownloadHistoryService>();
        builder.Services.AddSingleton<IDownloadService, DownloadService>();
        builder.Services.AddSingleton<IBrowserProvider, BrowserProvider>();
        builder.Services.AddSingleton<SystemBrowserProvider>();

        // 注册 ViewModels
        builder.Services.AddSingleton<AddressBarViewModel>();
        builder.Services.AddSingleton<HomeViewModel>();
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddSingleton<DownloadListViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // 注册 Views
        builder.Services.AddSingleton<BrowserPage>();
        builder.Services.AddTransient<DownloadListPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder;
    }
}
