using Microsoft.Maui.Platform.Linux.Hosting;

namespace SCAssistant.Maui.Linux
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseSharedMauiApp()
                .UseLinux();

            return builder.Build();
        }
    }
}
