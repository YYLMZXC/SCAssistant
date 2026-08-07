using CommunityToolkit.Mvvm.ComponentModel;

namespace SCAssistant.AvaloniaApp.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty] public partial string Greeting { get; set; } = "This is Settings";
}