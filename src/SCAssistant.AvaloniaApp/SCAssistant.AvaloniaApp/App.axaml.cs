using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using SCAssistant.AvaloniaApp.Services;
using SCAssistant.AvaloniaApp.ViewModels;
using SCAssistant.AvaloniaApp.Views;

namespace SCAssistant.AvaloniaApp;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _serviceProvider = ConfigureServices();

        // 设置全局 DI 容器
        Ioc.Default.ConfigureServices(_serviceProvider);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 桌面端 — 浏览器 MainWindow
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
            desktop.MainWindow = mainWindow;

            // 初始化 MainViewModel
            _ = InitializeMainViewModelAsync();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            // 移动端 — 浏览器 MainView
            var mainView = _serviceProvider.GetRequiredService<MainView>();
            mainView.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
            singleView.MainView = mainView;

            // 初始化 MainViewModel
            _ = InitializeMainViewModelAsync();
        }

        base.OnFrameworkInitializationCompleted();

        LogHelper.Info("[App] 框架初始化完成");
    }

    private async Task InitializeMainViewModelAsync()
    {
        try
        {
            var vm = _serviceProvider?.GetService<MainViewModel>();
            if (vm != null)
                await vm.InitializeAsync();
        }
        catch (Exception ex)
        {
            LogHelper.Error("[App] MainViewModel 初始化失败", ex);
        }
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // 服务注册
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IDownloadHistoryService, DownloadHistoryService>();
        services.AddSingleton<IDownloadService, DownloadService>();
        services.AddSingleton<IBrowserProvider, BrowserProvider>();

        // ViewModel 注册
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<DownloadListViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddSingleton<MainViewModel>();

        // View 注册
        services.AddTransient<MainWindow>();
        services.AddTransient<MainView>();
        services.AddTransient<SettingsView>();
        services.AddTransient<HomeView>();

        return services.BuildServiceProvider();
    }
}
