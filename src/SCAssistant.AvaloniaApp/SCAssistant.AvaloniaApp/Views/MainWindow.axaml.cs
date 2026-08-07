using System;
using Avalonia.Controls;
using Avalonia.Input;
using SCAssistant.AvaloniaApp.Services;
using SCAssistant.AvaloniaApp.ViewModels;

namespace SCAssistant.AvaloniaApp.Views;

public partial class MainWindow : Window
{
    private readonly IBrowserProvider _browser;

    public MainWindow(IBrowserProvider browser, SettingsViewModel settingsVm)
    {
        _browser = browser;
        InitializeComponent();
        SettingsPanel.DataContext = settingsVm;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        BrowserArea.Initialize(_browser);
    }

    private void AddressBar_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is MainViewModel vm)
        {
            vm.NavigateToUrlCommand.Execute(null);
        }
    }
}
