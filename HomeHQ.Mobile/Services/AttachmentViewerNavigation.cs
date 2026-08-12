using HomeHQ.Entities;
using HomeHQ.Mobile.Pages;
using HomeHQ.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace HomeHQ.Mobile.Services;

/// <summary>Presents <see cref="AttachmentViewerPage"/> modally (full-screen overlay).</summary>
public sealed class AttachmentViewerNavigation
{
    private readonly IServiceProvider _services;

    public AttachmentViewerNavigation(IServiceProvider services)
    {
        _services = services;
    }

    public async Task PresentAsync(string attachmentId, bool editable = false)
    {
        var page = _services.GetRequiredService<AttachmentViewerPage>();
        if (page.BindingContext is not AttachmentViewerViewModel vm)
        {
            throw new InvalidOperationException($"{nameof(AttachmentViewerPage)} expects {nameof(AttachmentViewerViewModel)}.");
        }

        vm.Editable = editable;
        vm.AttachmentId = attachmentId;
        await Shell.Current.Navigation.PushModalAsync(page);
    }

    /// <summary>Presents the viewer modally using the same <see cref="Attachment"/> instance as Asset Edit so edits sync to <see cref="Attachment.PendingUploadBytes"/>.</summary>
    public async Task PresentForAssetEditAsync(Attachment attachment, bool editable)
    {
        var page = _services.GetRequiredService<AttachmentViewerPage>();
        if (page.BindingContext is not AttachmentViewerViewModel vm)
        {
            throw new InvalidOperationException($"{nameof(AttachmentViewerPage)} expects {nameof(AttachmentViewerViewModel)}.");
        }

        await vm.InitializeForAssetEditAsync(attachment, editable);
        await Shell.Current.Navigation.PushModalAsync(page);
    }
}
