using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SCAssistant.AvaloniaApp.ViewModels;

namespace SCAssistant.AvaloniaApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 地址栏回车键处理 — 导航到输入的 URL。
    /// </summary>
    private void AddressBar_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is MainViewModel vm && !string.IsNullOrWhiteSpace(vm.AddressBarUrl))
        {
            vm.NavigateToUrlCommand.Execute(vm.AddressBarUrl);
        }
    }

    /// <summary>
    /// 点击设置面板遮罩层关闭设置。
    /// </summary>
    private void BackgroundOverlay_Tap(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ToggleSettingsCommand.Execute(null);
        }
    }
}
