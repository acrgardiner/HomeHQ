using HomeHQ.Entities;
using HomeHQ.Helpers;

namespace HomeHQ.Mobile.Models;

/// <summary>
/// Display model for the Assets list, aligned with Blazor AssetList columns:
/// Name, Category, From, Purchase Date, Warranty.
/// </summary>
public sealed class AssetListItem
{
    public required Asset Asset { get; init; }

    public Guid Id => Asset.Id;
    public string Name => Asset.Name;
    public string? PurchasedFrom => Asset.PurchasedFrom;

    public bool HasPurchasedFrom => !string.IsNullOrWhiteSpace(PurchasedFrom);

    public string CategoryTitle => Asset.Category?.Title?.Trim() is { Length: > 0 } title
        ? title
        : "Uncategorized";

    public string? CategoryIcon => string.IsNullOrWhiteSpace(Asset.Category?.Icon)
        ? null
        : Asset.Category.Icon;

    public string CategoryName => string.IsNullOrWhiteSpace(Asset.Category?.Name)
        ? "Uncategorized"
        : Asset.Category.Name;

    public string PurchaseDateFormatted => Asset.PurchaseDate.HasValue
        ? Formatter.FormatDate(Asset.PurchaseDate, "MMM dd, yyyy")
        : string.Empty;

    public string PurchaseDateDisplay => HasPurchaseDate
        ? $"Purchased {PurchaseDateFormatted}"
        : string.Empty;

    public bool HasPurchaseDate => Asset.PurchaseDate.HasValue;

    public string WarrantyDateFormatted => Asset.WarrantyExpiration.HasValue
        ? Formatter.FormatDate(Asset.WarrantyExpiration, "D")
        : string.Empty;

    public string WarrantyStatusText => Formatter.FormatWarrantyStatus(Asset.WarrantyExpiration);

    public bool HasWarranty => Asset.WarrantyExpiration.HasValue;

    public Color WarrantyStatusColor
    {
        get
        {
            if (!Asset.WarrantyExpiration.HasValue)
            {
                return Colors.Gray;
            }

            var daysRemaining = (Asset.WarrantyExpiration.Value.Date - DateTime.Today).Days;
            return daysRemaining switch
            {
                < 0 => Colors.Gray,
                <= 30 => Colors.Orange,
                _ => Colors.Green
            };
        }
    }
}
