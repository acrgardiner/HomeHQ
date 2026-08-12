using HomeHQ.Entities;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace HomeHQ.Server.Components.Entities.TestArea;

public partial class UploadFiles
{
    private List<Attachment_wfile> _attachments = new();
    private long maxFileSize = 1024 * 1024 * 15;
    //private int maxAllowedFiles = 3;
    private bool _uploading = false;

    private List<AttachmentType> AttachmentTypes = new();
    private Guid DefaultAttachmentTypeId;

    protected override async Task OnInitializedAsync()
    {
        AttachmentTypes = (await AttachmentTypeService.GetAllAsync()).ToList();

        if (AttachmentTypes.Count == 0)
        {
            Snackbar.Add("No attachment types found. Please create at least one attachment type.", Severity.Error);
            return;
        }

        DefaultAttachmentTypeId = AttachmentTypes.First().Id;
    }

    private void AddAttachment(IReadOnlyList<IBrowserFile> files)
    {
        _uploading = true;

        try
        {

            foreach (var file in files)
            {
                try
                {
                    var fileExtension = Path.GetExtension(file.Name);
                    var localFileName = Guid.NewGuid().ToString() + fileExtension;

                    // Save Attachment entity
                    var attachment = new Attachment
                    {
                        OriginFileName = file.Name,
                        LocalFileName = localFileName,
                        Extension = fileExtension,
                        ContentType = file.ContentType,
                        FileSize = file.Size,
                        AttachmentTypeId = DefaultAttachmentTypeId
                    };

                    Attachment_wfile attachmentWithFile = new Attachment_wfile
                    {
                        attachment = attachment,
                        file = file
                    };

                    _attachments.Add(attachmentWithFile);
                    Snackbar.Add($"File {file.Name} added for upload.", Severity.Success);
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Failed to upload {file.Name}: {ex.Message}", Severity.Error);
                }
            }
        }
        finally
        {
            _uploading = false;
            StateHasChanged();
        }
    }

    private async Task Save()
    {
        try
        {
            foreach (var item in _attachments)
            {
                // Save file to disk or database as needed
                var filePath = Path.Combine("appdata", "attachments", item.attachment.LocalFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await item.file.OpenReadStream(maxFileSize).CopyToAsync(stream);
                }

                var uploadedAttachment = await AttachmentService.AddAsync(item.attachment);
            }
            _attachments.Clear();
            Snackbar.Add($"Attachments Saved", Severity.Success);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save attachments");
            Snackbar.Add($"Failed to save attachments: {ex.Message}", Severity.Error);
        }
        finally
        {
            _uploading = false;
            StateHasChanged();
        }
    }

    private void Remove(Attachment_wfile item)
    {
        _attachments.Remove(item);
    }

    private struct Attachment_wfile
    {
        public Attachment attachment;
        public IBrowserFile file;
    }

    private string FormatFileSize(float bytes)
    {
        if (bytes >= 1024 * 1024)
            return $"{bytes / (1024 * 1024):0.##} MB";
        if (bytes >= 1024)
            return $"{bytes / 1024:0.##} KB";
        return $"{bytes:0} B";
    }
}
