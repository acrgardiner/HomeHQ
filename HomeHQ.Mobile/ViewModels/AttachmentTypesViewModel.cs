using HomeHQ.DTOs;
using HomeHQ.Mobile.Models;
using HomeHQ.Mobile.Services;

namespace HomeHQ.Mobile.ViewModels;

public sealed class AttachmentTypesViewModel : SetupListViewModel<AttachmentTypeDto>
{
    public AttachmentTypesViewModel(ApiClient apiClient, CacheService<AttachmentTypeDto> cache)
        : base(apiClient, cache)
    {
        PageTitle = "Attachment Types";
        SearchPlaceholder = "Search attachment types...";
        EmptyTitle = "No attachment types found";
        EmptyMessage = "Add your first attachment type to get started";
        AddButtonText = "Add Attachment Type";
    }

    protected override string Endpoint => "api/attachmenttypes";
    protected override string EditKind => SetupEditViewModel.KindAttachmentType;

    protected override SetupListItem MapItem(AttachmentTypeDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name
    };
}
