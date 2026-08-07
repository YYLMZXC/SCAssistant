using Avalonia;
using Avalonia.Controls;
using SCAssistant.AvaloniaApp.ViewModels;
using System;

namespace SCAssistant.AvaloniaApp.Views;

public partial class MainView : NavigationPage
{
    public MainView()
    {
        InitializeComponent();
    }

    protected override async void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (CurrentPage == null)
            await PushAsync(new HomeView()
            {
                DataContext = new HomeViewModel()
            });
    }
}