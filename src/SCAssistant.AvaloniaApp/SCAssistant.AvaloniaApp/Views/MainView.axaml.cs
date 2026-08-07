using SCAssistant.AvaloniaApp.ViewModels;
using Avalonia.Controls;

namespace SCAssistant.AvaloniaApp.Views;

public partial class MainView : UserControl
{
    public MainView(SettingsViewModel settingsVm)
    {
        InitializeComponent();
        SettingsPanel.DataContext = settingsVm;
    }
}
