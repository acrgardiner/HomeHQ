using HomeHQ.Components.Entities.Common;
using HomeHQ.Entities;
using HomeHQ.Server.Components.Entities.Common;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HomeHQ.Server.Components.Entities.Assets;

public partial class AssetCreatePage
{
    [Parameter]
    [SupplyParameterFromQuery(Name = "returnState")]
    public string? ReturnState { get; set; }


    private Asset _asset = new() { PurchaseDate = DateTime.Today, WarrantyExpiration = DateTime.Today };
    private List<HomeHQ.Entities.Category> _categories = new();
    private List<HomeHQ.Entities.WarrantyType> _warrantyTypes = new();
    private static readonly Guid? NullGuid = null;

    private List<HomeHQ.Entities.Attribute> _attributes = new();
    private List<Note> _notes = new();
    private List<string> _validationErrors = new();

    private Guid _defaultWarrantyTypeId;

    private bool _IsMobile;

    private Attachment_Sidebar _attachmentSidebar;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _IsMobile = CurrentUser.IsMobile;

            _categories = (await CategoryService.GetAllAsync()).ToList();
            _warrantyTypes = ((await WarrantyTypeService.GetAllAsync()).OrderBy(x => x.SortOrder)).ToList();

            _defaultWarrantyTypeId = _warrantyTypes.Where(wt => wt.Default).First().Id;

            if (_asset.PurchaseDate == default)
            {
                _asset.PurchaseDate = DateTime.Today;
            }

            _asset.WarrantyTypeId = _defaultWarrantyTypeId;
            SetWarrantyExpiration();

            _attributes.Add(new HomeHQ.Entities.Attribute()); // Add initial empty attribute
            _notes.Add(new Note()); // Add initial empty note
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading data: {ex.Message}", Severity.Error);
            Logger.LogError(ex, "Error initializing CreateAsset components");
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

    private async Task SaveAttributes(Guid Id)
    {
        try
        {
            foreach (var attr in _attributes.Where(a => !string.IsNullOrWhiteSpace(a.Key)))
            {
                attr.ParentId = Id;
                attr.ParentType = nameof(Asset);

                await AttributeService.AddAsync(attr);
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
            foreach (var note in _notes.Where(n => !string.IsNullOrWhiteSpace(n.Title) || !string.IsNullOrWhiteSpace(n.Content)))
            {
                note.ParentId = Id;
                note.ParentType = nameof(Asset);

                await NoteService.AddAsync(note);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save notes");
            Snackbar.Add($"Failed to save notes: {ex.Message}", Severity.Error);
        }
    }

    private async Task HandleValidSubmit()
    {
        try
        {
            Snackbar.Add($"Saving...", Severity.Info);
            // Set null for empty Guid selections
            if (_asset.CategoryId == Guid.Empty)
            {
                _asset.CategoryId = null;
            }

            if (_asset.WarrantyTypeId == Guid.Empty)
            {
                _asset.WarrantyTypeId = null;
            }

            var newAsset = await AssetService.AddAsync(_asset);

            await SaveAttributes(newAsset.Id);
            await SaveNotes(newAsset.Id);
            await _attachmentSidebar.SaveAttachments(newAsset.Id);

            Snackbar.Add("Asset created successfully.", Severity.Success);
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
        if (_asset.WarrantyTypeId == NullGuid)
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
}
