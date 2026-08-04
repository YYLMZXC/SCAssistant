using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using SCAssistant.UnoApp.Services;
using SCAssistant.UnoApp.ViewModels;
using SCAssistant.UnoApp.Views;
using Uno.Resizetizer;

namespace SCAssistant.UnoApp;

public partial class App : Application
{
    private Window? _mainWindow;

    public App()
    {
        this.InitializeComponent();
        LogHelper.Info("[应用] 构造函数 - 正在配置服务");
        ConfigureServices();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        LogHelper.Info("[应用] OnLaunched - 正在创建窗口");
        _mainWindow = new Window();

        var mainPage = new MainPage();
        _mainWindow.Content = mainPage;
        _mainWindow.Title = "SCAssistant - 生存战争助手";
        _mainWindow.SetWindowIcon();

        _mainWindow.Activate();
        LogHelper.Info("[应用] 窗口已激活");
    }

    private static void ConfigureServices()
    {
        var services = new ServiceCollection();

        // Core services
        var downloadHistory = new DownloadHistoryService();
        downloadHistory.Load();
        services.AddSingleton<IDownloadHistoryService>(downloadHistory);

        var browserProvider = new BrowserProvider();
        services.AddSingleton<IBrowserProvider>(browserProvider);

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<DownloadListViewModel>();

        var provider = services.BuildServiceProvider();
        Ioc.Default.ConfigureServices(provider);

        // Populate static ServiceLocator
        ServiceLocator.BrowserProvider = browserProvider;
        ServiceLocator.DownloadHistory = downloadHistory;
        ServiceLocator.ServiceLocatorObj = new ServiceLocatorInstance();

        LogHelper.Info("[应用] 服务配置完成");
    }
}
