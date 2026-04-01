using HomeHQ.Server.Components.Entities.Common;

namespace HomeHQ.Server.Components.Entities.Category;

public partial class ListCategories
{
    private EntityList<HomeHQ.Entities.Category>? _entityList;

    protected override async Task OnInitializedAsync()
    {
        await Task.Yield();
    }

    protected override async Task OnParametersSetAsync()
    {
        await _entityList?.ReloadData();
    }

    private async Task OpenCreate()
    {
        await DialogService.ShowAsync<CreateCategory>("Add New Category");
    }
}
