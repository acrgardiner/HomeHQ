using System.ComponentModel;
using HomeHQ.Mobile.ViewModels;

namespace HomeHQ.Mobile.Pages;

public partial class AttachmentViewerPage : ContentPage
{
    private AttachmentViewerViewModel? _viewModel;

    public AttachmentViewerPage(AttachmentViewerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Only reload if asset is not already loaded, or if signaled to refresh
            if (_viewModel?.Attachment is not null)
            {
                await _viewModel.LoadLinkedAttachmentContentAsync();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load asset: {ex.Message}", "OK");
        }
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = BindingContext as AttachmentViewerViewModel;
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel = null;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AttachmentViewerViewModel.IsCropMode) || sender is not AttachmentViewerViewModel vm)
        {
            return;
        }

        if (!vm.IsCropMode)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            ImageViewerContainer.ResetTransform();
            CropOverlay.RequestLayout();
        });
    }
}
