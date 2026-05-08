using HomeHQ.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HomeHQ.Server.Components.Entities.Common;

public partial class AttributeGrid_view
{
    [Parameter] public Guid? ParentId { get; set; }

    private List<HomeHQ.Entities.Attribute> _attributes { get; set; } = new();

    private bool _loading = true;

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
            _loading = true;
            _attributes = (await AttributeService.GetAsync(
                filter: x => x.ParentId == ParentId,
                orderby: x => x.CreatedOn,
                descending: true,
                take: 10
            )).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading attributes");
            Snackbar.Add("Failed to load", Severity.Error);
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }
}
