using CommunityToolkit.Mvvm.ComponentModel;

namespace SCAssistant.AvaloniaApp.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    [ObservableProperty] public partial string Greeting { get; set; } = "Welcome to Avalonia!";
}