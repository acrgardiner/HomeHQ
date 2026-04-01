using HomeHQ.Entities;
using MudBlazor;

namespace HomeHQ.Server.Components.Entities.TestArea;

public partial class ListAttachments
{
    private List<Attachment> _attachments = new();
    private bool _loading = true;

    private string SearchString
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                StateHasChanged();
            }
        }
    } = string.Empty;


    protected override async Task OnInitializedAsync()
    {
        await LoadAttachments();
    }

    private async Task ReloadData()
    {
        await LoadAttachments();
    }


    private async Task LoadAttachments()
    {
        try
        {
            _loading = true;
            _attachments = (await AttachmentService.GetAllAsync(new() { x => x.AttachmentType })).ToList();
            //_attachments = (await AttachmentService.GetAllAsync()).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load attachments");
            Snackbar.Add("Failed to load", Severity.Error);
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    private Func<Attachment, bool> _quickFilter => x =>
    {
        if (string.IsNullOrWhiteSpace(SearchString))
            return true;

        if (
            x.OriginFileName.Contains(SearchString, StringComparison.OrdinalIgnoreCase)
        )
            return true;

        return false;
    };

}
