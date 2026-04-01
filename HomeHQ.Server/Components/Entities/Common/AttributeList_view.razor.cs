using HomeHQ.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HomeHQ.Server.Components.Entities.Common;

public partial class AttributeList_view
{
    [Parameter] public Guid? ParentId { get; set; }

    private List<AttributeValue> _attributes { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadAttributes();
    }

    private async Task ReloadData()
    {
        await LoadAttributes();
    }


    private async Task LoadAttributes()
    {
        try
        {
            _attributes = (await AttributeService.GetAsync(
                filter: x => x.ParentId == ParentId,
                orderby: x => x.CreatedOn,
                descending: true,
                take: 10
            )).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load attributes");
            Snackbar.Add("Failed to load", Severity.Error);
        }
        finally
        {
            StateHasChanged();
        }
    }
}
