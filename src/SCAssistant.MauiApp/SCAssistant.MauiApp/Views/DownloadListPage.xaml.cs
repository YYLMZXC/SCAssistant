using SCAssistant.Maui.ViewModels;

namespace SCAssistant.Maui.Views;

public partial class DownloadListPage : ContentPage
{
    private readonly DownloadListViewModel _viewModel;

    public DownloadListPage(DownloadListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDownloadsCommand.ExecuteAsync(null);
    }
}
