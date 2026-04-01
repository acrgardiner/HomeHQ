using HomeHQ.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HomeHQ.Helpers;

public static class WarrantyFormatter
{
    public static RenderFragment RenderWarrantyInfo(Asset asset) => builder =>
    {
        if (!asset.WarrantyExpiration.HasValue)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "mud-text-secondary");
            builder.AddContent(2, "No warranty");
            builder.CloseElement();
            return;
        }

        var today = DateTime.Today;
        var warrantyDate = asset.WarrantyExpiration.Value;
        var daysRemaining = (warrantyDate - today).Days;

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "warranty-status");

        // Icon
        builder.OpenComponent<MudIcon>(2);
        builder.AddAttribute(3, "Size", Size.Small);

        if (daysRemaining < 0)
        {
            builder.AddAttribute(4, "Icon", Icons.Material.Filled.ErrorOutline);
            builder.AddAttribute(5, "Class", "warranty-expired");
        }
        else if (daysRemaining <= 30)
        {
            builder.AddAttribute(4, "Icon", Icons.Material.Filled.Warning);
            builder.AddAttribute(5, "Class", "warranty-expiring");
        }
        else
        {
            builder.AddAttribute(4, "Icon", Icons.Material.Filled.CheckCircleOutline);
            builder.AddAttribute(5, "Class", "warranty-valid");
        }

        builder.CloseComponent();

        // Text
        builder.OpenElement(7, "div");

        builder.OpenComponent<MudText>(8);
        builder.AddAttribute(9, "Typo", Typo.body2);
        builder.AddAttribute(10, "ChildContent", (RenderFragment)(b => b.AddContent(11, Formatter.FormatDate(warrantyDate, "D"))));
        builder.CloseComponent();

        builder.OpenComponent<MudText>(12);
        builder.AddAttribute(13, "Typo", Typo.caption);
        if (daysRemaining >= 30) {
            builder.AddAttribute(14, "Class", "warranty-valid");
        } 
        else if (daysRemaining > 0)
        {
            builder.AddAttribute(14, "Class", "warranty-expiring");
        }
        else
        {
            builder.AddAttribute(14, "Class", "warranty-expired");
        }
        builder.AddAttribute(15, "ChildContent", (RenderFragment)(b => b.AddContent(16, Formatter.FormatWarrantyStatus(daysRemaining))));
        builder.CloseComponent();

        builder.CloseElement(); // div
        builder.CloseElement(); // warranty-status div
    };
}
