using Avalonia;
using Avalonia.Controls;
using AvaloniaApplication1.ViewModels;
using System;

namespace AvaloniaApplication1.Views;

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