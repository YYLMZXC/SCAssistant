using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using SCAssistant.AvaloniaApp.Services;
using SCAssistant.AvaloniaApp.ViewModels;

namespace SCAssistant.AvaloniaApp.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        LogHelper.Debug("[SettingsView] 设置页面构造");
    }

    private async void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            LogHelper.Debug("[SettingsView] DataContext 绑定 SettingsViewModel，开始加载");
            await vm.LoadAsync();
        }
    }
}
