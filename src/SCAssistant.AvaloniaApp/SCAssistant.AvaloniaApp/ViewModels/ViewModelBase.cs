using CommunityToolkit.Mvvm.ComponentModel;

namespace SCAssistant.AvaloniaApp.ViewModels;

/// <summary>
/// ViewModel 基类 — 继承自 ObservableObject。
/// </summary>
public partial class ViewModelBase : ObservableObject
{
    /// <summary>View 标题。</summary>
    [ObservableProperty]
    private string _title = "SCAssistant";
}
