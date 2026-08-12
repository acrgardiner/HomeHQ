using HomeHQ.Mobile.ViewModels;
using HomeHQ.Entities;

namespace HomeHQ.Mobile.Pages;

public partial class AssetEditPage : ContentPage
{
    private readonly AssetEditViewModel _viewModel;

    public AssetEditPage(AssetEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Refresh carousel preview after attachment viewer (rotate/crop updates PendingUploadBytes).
            await _viewModel.LoadCurrentAttachmentPreviewAsync();
            _viewModel.NotifyAttachmentCarouselChanged();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load asset: {ex.Message}", "OK");
        }
    }


    // Unfocused handlers removed — Note is now observable and CollectionView updates directly via bindings.
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }

    private void OnAttributeTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry)
        {
            var ctx = entry.BindingContext;
            if (ctx is HomeHQ.Mobile.Models.AttributeViewModel vm)
            {
                vm.Key = entry.Text;
            }
            else if (ctx is HomeHQ.Entities.Attribute attr)
            {
                attr.Key = entry.Text;
            }
        }
    }

    private void OnAttributeValueTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry)
        {
            var ctx = entry.BindingContext;
            if (ctx is HomeHQ.Mobile.Models.AttributeViewModel vm)
            {
                vm.Value = entry.Text;
            }
            else if (ctx is HomeHQ.Entities.Attribute attr)
            {
                attr.Value = entry.Text;
            }
        }
    }

    private void OnNoteTitleTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry)
        {
            var ctx = entry.BindingContext;
            if (ctx is HomeHQ.Mobile.Models.NoteViewModel vm)
            {
                vm.Title = entry.Text ?? string.Empty;
            }
            else if (ctx is HomeHQ.Entities.Note note)
            {
                note.Title = entry.Text ?? string.Empty;
            }
        }
    }

    private void OnNoteContentTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is Editor editor)
        {
            var ctx = editor.BindingContext;
            if (ctx is HomeHQ.Mobile.Models.NoteViewModel vm)
            {
                vm.Content = editor.Text ?? string.Empty;
            }
            else if (ctx is HomeHQ.Entities.Note note)
            {
                note.Content = editor.Text ?? string.Empty;
            }
        }
    }

    private async void OnSaveButtonClicked(object sender, EventArgs e)
    {
        try
        {
            // Move focus briefly so any focused Entry/Editor commits its binding
            this.Focus();
            await Task.Yield();

            // Execute the Save command on the ViewModel
            if (_viewModel?.SaveCommand != null && _viewModel.SaveCommand.CanExecute(null))
            {
                _viewModel.SaveCommand.Execute(null);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to save: {ex.Message}", "OK");
        }
    }
}
