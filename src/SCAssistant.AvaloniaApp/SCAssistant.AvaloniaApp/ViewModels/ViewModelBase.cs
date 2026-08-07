using CommunityToolkit.Mvvm.ComponentModel;

namespace SCAssistant.AvaloniaApp.ViewModels;

/// <summary>
/// ViewModel基类 - 使用 CommunityToolkit.Mvvm
/// </summary>
public partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;
}
