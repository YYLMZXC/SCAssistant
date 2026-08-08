using SCAssistant.Maui.Services;
using SCAssistant.Maui.ViewModels;
using SCAssistant.Maui.Views;

namespace SCAssistant.Maui;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        Services = serviceProvider;

        // 初始化日志系统
        var logService = Services.GetRequiredService<ILogService>();
        LogHelper.Initialize(logService);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var shell = Services.GetRequiredService<AppShell>();
        return new Window(shell);
    }
}
