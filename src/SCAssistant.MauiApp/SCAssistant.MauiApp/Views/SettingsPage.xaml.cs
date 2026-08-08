using SCAssistant.Maui.ViewModels;

namespace SCAssistant.Maui.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        BtnBrowse.Clicked += async (_, _) =>
        {
#if WINDOWS || MACCATALYST
            try
            {
                var result = await FolderPicker.Default.PickAsync(default);
                if (result.IsSuccessful)
                    _viewModel.DownloadDirectory = result.Folder.Path;
            }
            catch
            {
                // FolderPicker not supported on this platform
            }
#endif
            await Task.CompletedTask;
        };
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _viewModel.SaveAsync();
    }
}
