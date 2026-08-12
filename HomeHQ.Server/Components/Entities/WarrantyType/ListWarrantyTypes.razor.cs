using HomeHQ.Server.Components.Entities.Common;

namespace HomeHQ.Server.Components.Entities.WarrantyType;

public partial class ListWarrantyTypes
{
    private EntityList<HomeHQ.Entities.WarrantyType>? entityList;

    protected override async Task OnInitializedAsync()
    {
        await Task.Yield();
    }

    protected override async Task OnParametersSetAsync()
    {
        await entityList?.ReloadData();
    }

    private async Task OpenCreate()
    {
        await DialogService.ShowAsync<CreateWarrantyType>("Add New Warranty Type");
    }
}
