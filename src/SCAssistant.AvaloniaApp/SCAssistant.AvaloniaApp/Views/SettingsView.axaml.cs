using Avalonia.Controls;
using SCAssistant.AvaloniaApp.ViewModels;

namespace SCAssistant.AvaloniaApp.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            return;

        Loaded += async (_, _) =>
        {
            if (DataContext is SettingsViewModel vm)
            {
                await vm.InitializeAsync();
            }
        };
    }
}
