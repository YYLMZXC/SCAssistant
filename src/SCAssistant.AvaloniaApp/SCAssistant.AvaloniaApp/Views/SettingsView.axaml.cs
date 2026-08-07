using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using SCAssistant.AvaloniaApp.Services;
using SCAssistant.AvaloniaApp.ViewModels;

namespace SCAssistant.AvaloniaApp.Views;

/// <summary>
/// 设置页面视图 — DataContext 变更为 SettingsViewModel 时自动加载持久化设置。
/// </summary>
public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        LogHelper.Debug("[SettingsView] 设置页面构造");
    }

    /// <summary>当 DataContext 绑定到 SettingsViewModel 时，自动加载设置。</summary>
    private async void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            LogHelper.Debug("[SettingsView] DataContext 绑定 SettingsViewModel，开始加载");
            await vm.LoadAsync();
        }
    }
}
