using HomeHQ.Components.Entities.Common;
using HomeHQ.Entities;
using HomeHQ.Server.Components.Entities.Common;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Linq.Expressions;

namespace HomeHQ.Server.Components.Entities.Assets;

public partial class AssetEditPage
{
    [Parameter]
    public Guid Id { get; set; }
    [Parameter]
    [SupplyParameterFromQuery(Name = "returnState")]
    public string? ReturnState { get; set; }

    private HomeHQ.Entities.Asset _asset = new();
    private List<HomeHQ.Entities.Category> _categories = new();
    private List<HomeHQ.Entities.WarrantyType> _warrantyTypes = new();
    private static readonly Guid? nullGuid = null;

    private List<HomeHQ.Entities.Attribute> _attributes = new();
    private List<HomeHQ.Entities.Note> _notes = new();
    private List<string> _validationErrors = new();

    private Guid _defaultWarrantyTypeId;

    private Attachment_Sidebar _attachmentSidebar;
    private bool _IsMobile;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _IsMobile = CurrentUser.IsMobile;

            _categories = (await CategoryService.GetAllAsync()).ToList();
            _warrantyTypes = ((await WarrantyTypeService.GetAllAsync()).OrderBy(x => x.SortOrder)).ToList();

            _defaultWarrantyTypeId = _warrantyTypes.Where(wt => wt.Default).First().Id;

            var loadedAsset = await AssetService.GetByIdAsync(Id);
            if (loadedAsset == null)
            {
                Snackbar.Add("Asset not found.", Severity.Error);
                GoBack();
                return;
            }
            _asset = loadedAsset;

            _attributes = (await AttributeService.GetAsync(
                filter: x => x.ParentId == Id && x.ParentType == nameof(Asset)
            )).ToList();

            _notes = (await NoteService.GetAsync(
                filter: x => x.ParentId == Id && x.ParentType == nameof(Asset)
            )).ToList();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading data: {ex.Message}", Severity.Error);
            Logger.LogError(ex, "Error initializing EditAsset components");
        }
    }

    private async Task Remove(HomeHQ.Entities.Attribute item)
    {
        _attributes.Remove(item);
        if (item.Id != Guid.Empty)
        {
            await AttributeService.DeleteAsync(item.Id);
        }
    }

    private async Task Remove(Note item)
    {
        _notes.Remove(item);
        if (item.Id != Guid.Empty)
        {
            await NoteService.DeleteAsync(item.Id);
        }
    }

    private void AddAttribute()
    {
        _attributes.Add(new HomeHQ.Entities.Attribute());
    }

    private void AddNote()
    {
        _notes.Add(new Note());
    }

    private async Task HandleValidSubmit()
    {
        try
        {
            Snackbar.Add($"Saving...", Severity.Info);

            if (_asset.CategoryId == Guid.Empty)
            {
                _asset.CategoryId = null;
            }

            if (_asset.WarrantyTypeId == Guid.Empty)
            {
                _asset.WarrantyTypeId = null;
            }

            await AssetService.UpdateAsync(_asset);
            await SaveAttributes(_asset.Id);
            await SaveNotes(_asset.Id);
            await _attachmentSidebar.SaveAttachments(_asset.Id);

            Snackbar.Add("Asset updated successfully.", Severity.Success);
            GoBack(); // Use GoBack to preserve navigation state
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
    }

    private void GoBack()
    {
        Navigation.GoBack(ReturnState, "/assets");
    }

    private void OnPurchaseDateChanged(DateTime? date)
    {
        _asset.PurchaseDate = date;
        SetWarrantyExpiration();
    }

    private void SetWarrantyExpiration()
    {
        if (_asset.WarrantyTypeId == nullGuid)
        {
            _asset.WarrantyExpiration = null;
        }
        else
        {
            if (_asset.PurchaseDate.HasValue)
            {
                var warrantyType = _warrantyTypes.Where(w => w.Id == _asset.WarrantyTypeId).FirstOrDefault();
                if (warrantyType != null)
                {
                    if (warrantyType.Years is not null)
                    {
                        _asset.WarrantyExpiration = _asset.PurchaseDate.Value.AddYears(warrantyType.Years.Value);
                    }

                    if (warrantyType.Months is not null)
                    {
                        _asset.WarrantyExpiration = _asset.PurchaseDate.Value.AddMonths(warrantyType.Months.Value);
                    }

                    if (warrantyType.Days is not null)
                    {
                        _asset.WarrantyExpiration = _asset.PurchaseDate.Value.AddDays(warrantyType.Days.Value);
                    }

                    StateHasChanged();
                }
            }
        }
    }

    private async Task SaveAttributes(Guid Id)
    {
        try
        {
            foreach (var attr in _attributes.Where(a => !string.IsNullOrWhiteSpace(a.Key)))
            {
                if (attr.Id == Guid.Empty)
                {
                    attr.ParentId = Id;
                    attr.ParentType = nameof(Asset);
                    await AttributeService.AddAsync(attr);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save attributes");
            Snackbar.Add($"Failed to save attributes: {ex.Message}", Severity.Error);
        }
    }

    private async Task SaveNotes(Guid Id)
    {
        try
        {
            foreach (var note in _notes.Where(a => !string.IsNullOrWhiteSpace(a.Title) || !string.IsNullOrWhiteSpace(a.Content)))
            {
                if (note.Id == Guid.Empty)
                {
                    note.ParentId = Id;
                    note.ParentType = nameof(Asset);
                    await NoteService.AddAsync(note);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save notes");
            Snackbar.Add($"Failed to save notes: {ex.Message}", Severity.Error);
        }
    }
}
