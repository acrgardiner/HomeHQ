using HomeHQ.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;

namespace HomeHQ.Server.Components.Entities.Common;

public partial class Attachment_Sidebar
{

    [Parameter] public Guid? ParentId { get; set; }
    [Parameter] public bool IsMobile { get; set; } = false;
    [Parameter] public string? ParentType { get; set; }
    [Parameter] public bool ReadOnly { get; set; } = false;

    private List<Attachment_wfile> _attachments = new();
    private const long MAX_PREVIEW_SIZE = 30 * 1024 * 1024;
    private bool _uploading = false;
    private bool _connectionLost = false;

    private List<AttachmentType> _attachmentTypes = new();
    private Guid _defaultAttachmentTypeId;
    private AttachmentType _defaultAttachmentType = new();

    private int _currentAttachmentIndex = 0;

    private bool _dialogVisible = false;
    private readonly DialogOptions _maxWidthOptions = new() { MaxWidth = MaxWidth.Large };
    private const string EDITOR_CONTAINER_ID = "image-editor-container";
    private bool _editorInitialized = false;
    private bool _cropModeEnabled = false;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _attachmentTypes = (await AttachmentTypeService.GetAsync()).ToList();

            if (_attachmentTypes.Count == 0)
            {
                Snackbar.Add("No attachment types found. Please create at least one attachment type.", Severity.Error);
                return;
            }

            _defaultAttachmentType = _attachmentTypes.Where(a => a.Default).First();
            _defaultAttachmentTypeId = _defaultAttachmentType.Id;

            if (ParentId != Guid.Empty)
            {
                _attachments = (await AttachmentService.GetAsync(
                    filter: x => x.ParentId == ParentId,
                    orderby: x => x.CreatedOn,
                    descending: true,
                    take: 10
                )).Select(a => new Attachment_wfile { Attachment = a }).ToList();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load attachments");
            Snackbar.Add("Failed to load", Severity.Error);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_editorInitialized)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("ImageEditorInterop.destroy", EDITOR_CONTAINER_ID);
            }
            catch
            {
                // Circuit may already be disposed
            }
        }
    }

    private void NextAttachment()
    {
        if (_currentAttachmentIndex < _attachments.Count - 1)
        {
            _currentAttachmentIndex++;
        }
    }

    private void PreviousAttachment()
    {
        if (_currentAttachmentIndex > 0)
        {
            _currentAttachmentIndex--;
        }
    }

    private void SetCurrentAttachment(int index)
    {
        if (index >= 0 && index < _attachments.Count)
        {
            _currentAttachmentIndex = index;
        }
    }

    private async Task AddAttachment(IReadOnlyList<IBrowserFile> files)
    {
        _uploading = true;

        try
        {
            Snackbar.Add($"Adding attachments...", Severity.Info);
            foreach (var file in files)
            {
                try
                {
                    var fileExtension = Path.GetExtension(file.Name);

                    // Save Attachment entity
                    var attachment = new Attachment
                    {
                        OriginFileName = file.Name,
                        Extension = fileExtension,
                        ContentType = file.ContentType,
                        FileSize = file.Size,
                        AttachmentTypeId = _defaultAttachmentTypeId,
                        AttachmentType = _defaultAttachmentType
                    };

                    using var stream = file.OpenReadStream(MAX_PREVIEW_SIZE);
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);

                    var bytes = ms.ToArray();

                    Attachment_wfile attachmentWithFile = new Attachment_wfile(attachment, bytes) { IsNew = true };

                    _attachments.Add(attachmentWithFile);
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Failed to upload {file.Name}: {ex.Message}", Severity.Error);
                }

                Snackbar.Add($"Attachments loaded", Severity.Success);
            }
        }
        finally
        {
            _uploading = false;
            StateHasChanged();
        }
    }

    public async Task<bool> SaveAttachments(Guid Id)
    {
        try
        {
            foreach (var item in _attachments)
            {
                byte[]? bytes = null;

                if (item.FileBytes != null)
                {
                    bytes = item.FileBytes;
                }

                if (item.IsNew && bytes != null)
                {
                    item.Attachment.ParentId = Id;
                    item.Attachment.ParentType = ParentType!;
                    await FileStorageService.UploadAsync<Asset>(bytes, item.Attachment);

                    var uploadedAttachment = await AttachmentService.AddAsync(item.Attachment);
                }

                if (!item.IsNew && bytes != null)
                {
                    // If edited, flag old attachment for deletion and re-upload
                    var newAttachment = new Attachment(item.Attachment);
                    await AttachmentService.DeleteAsync(item.Attachment.Id);

                    await FileStorageService.UploadAsync<Asset>(bytes, newAttachment);

                    var uploadedAttachment = await AttachmentService.AddAsync(newAttachment);
                }
            }

            _attachments.Clear();
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save attachments");
            Snackbar.Add($"Failed to save attachments: {ex.Message}", Severity.Error);
            return false;
        }
        finally
        {
            StateHasChanged();
        }
    }

    private async Task Remove(Attachment_wfile item)
    {
        _attachments.Remove(item);

        // Adjust index if needed
        if (_currentAttachmentIndex >= _attachments.Count && _attachments.Count > 0)
        {
            _currentAttachmentIndex = _attachments.Count - 1;
        }
    }

    /// <summary>
    /// DTO for receiving data from IndexedDB
    /// </summary>
    private class ClientStorageItem
    {
        public string Id { get; set; } = "";
        public ClientStorageMetadata? Metadata { get; set; }
        public string? DataUrl { get; set; }
    }

    private class ClientStorageMetadata
    {
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public long Size { get; set; }
        public string? Extension { get; set; }
    }

    private class Attachment_wfile
    {
        public Attachment Attachment;
        public byte[]? FileBytes;
        public bool IsNew = false;

        public Attachment_wfile()
        {
            Attachment = new Attachment();
            FileBytes = null;
        }

        public Attachment_wfile(Attachment attachment, byte[]? fileBytes = null)
        {
            Attachment = attachment;
            FileBytes = fileBytes;
        }

        public string GetImageUrl()
        {
            if (FileBytes != null)
            {
                var base64 = Convert.ToBase64String(FileBytes);
                return $"data:{Attachment.ContentType};base64,{base64}";
            }

            // Attachment is on server, use URL
            return $"/api/attachments/{Attachment!.Id}/data";
        }

        public string GetImagePreviewUrl()
        {
            if (FileBytes != null)
            {
                var base64 = Convert.ToBase64String(FileBytes);
                return $"data:{Attachment!.ContentType};base64,{base64}";
            }

            // Attachment is on server, use preview URL
            return $"/api/attachments/{Attachment!.Id}/previewdata";
        }
    }

    private bool IsImageAttachment(Attachment attachment) //TODO: Make common
    {
        return !string.IsNullOrEmpty(attachment.ContentType) && attachment.ContentType.StartsWith("image/");
    }

    private string GetAttachmentIcon(Attachment attachment)
    {
        if (string.IsNullOrEmpty(attachment.ContentType))
        {
            return Icons.Material.Filled.AttachFile;
        }

        return attachment.ContentType.StartsWith("image/") switch
        {
            true => Icons.Material.Filled.Image,
            false when attachment.ContentType.Contains("pdf") => Icons.Material.Filled.PictureAsPdf,
            false when attachment.ContentType.Contains("document") => Icons.Material.Filled.Description,
            false when attachment.ContentType.Contains("spreadsheet") => Icons.Material.Filled.GridOn,
            _ => Icons.Material.Filled.AttachFile
        };
    }

    private async Task ImageDialogClose()
    {
        if (_editorInitialized)
        {
            await JSRuntime.InvokeVoidAsync("ImageEditorInterop.destroy", EDITOR_CONTAINER_ID);
            _editorInitialized = false;
        }

        _cropModeEnabled = false;
        _dialogVisible = false;
    }

    private async Task ImageDialogOpen()
    {
        _dialogVisible = true;
        _cropModeEnabled = false;

        if (!ReadOnly && IsImageAttachment(_attachments[_currentAttachmentIndex].Attachment))
        {
            // Wait for dialog to render
            await Task.Delay(100);

            var imageSrc = _attachments[_currentAttachmentIndex].GetImageUrl();
            _editorInitialized = await JSRuntime.InvokeAsync<bool>("ImageEditorInterop.initialize", EDITOR_CONTAINER_ID, imageSrc);
        }
    }

    private async Task ToggleCropMode()
    {
        if (!_editorInitialized)
        {
            return;
        }

        _cropModeEnabled = !_cropModeEnabled;

        if (_cropModeEnabled)
        {
            await JSRuntime.InvokeVoidAsync("ImageEditorInterop.enableCrop", EDITOR_CONTAINER_ID);
        }
        else
        {
            await JSRuntime.InvokeVoidAsync("ImageEditorInterop.disableCrop", EDITOR_CONTAINER_ID);
        }
    }

    private async Task Rotate(decimal degree)
    {
        if (_editorInitialized)
        {
            await JSRuntime.InvokeVoidAsync("ImageEditorInterop.rotate", EDITOR_CONTAINER_ID, (int)degree);
        }
    }

    public async Task AcceptCrop()
    {
        if (_editorInitialized)
        {
            try
            {
                var imageDataArray = await JSRuntime.InvokeAsync<int[]>("ImageEditorInterop.getCroppedImageData", EDITOR_CONTAINER_ID);

                if (imageDataArray != null)
                {
                    var attachment = _attachments[_currentAttachmentIndex];

                    var croppedCanvasData = imageDataArray.Select(i => (byte)i).ToArray();
                    attachment.FileBytes = croppedCanvasData;

                    Snackbar.Add("Image saved successfully", Severity.Success);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to save cropped image");
                Snackbar.Add("Failed to save image", Severity.Error);
            }
        }

        await ImageDialogClose();
    }
}
