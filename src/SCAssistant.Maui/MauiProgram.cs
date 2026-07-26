using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCAssistant.Maui.Services;

namespace SCAssistant.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<DownloadHistoryService>();
        builder.Services.AddSingleton<DownloadManager>();
        builder.Services.AddSingleton<CrossPlatformDownloadService>();

        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<DownloadListPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
