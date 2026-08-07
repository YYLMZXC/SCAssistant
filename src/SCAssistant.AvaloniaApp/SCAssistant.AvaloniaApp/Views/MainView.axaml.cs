using Avalonia.Controls;
using SCAssistant.AvaloniaApp.ViewModels;

namespace SCAssistant.AvaloniaApp.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    public MainView(MainViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
