using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaApplication1.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty] public partial string Greeting { get; set; } = "This is Settings";
}