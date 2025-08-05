using Microsoft.AspNetCore.Components;
using MudBlazor;
using projectaardvarkx2.Entities;

namespace projectaardvarkx2.Helpers
{
    public static class Formatter
    {
        public static string FormatFileSize(float bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public static string FormatDate(DateTime? date)
        {
            string result = "";

            if (date.HasValue)
            {
                result = date.Value.ToString("d");
            }

            return result;
        }

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
            builder.AddAttribute(10, "ChildContent", (RenderFragment)(b => b.AddContent(11, warrantyDate.ToString("MMM dd, yyyy"))));
            builder.CloseComponent();

            if (daysRemaining >= 0)
            {
                builder.OpenComponent<MudText>(12);
                builder.AddAttribute(13, "Typo", Typo.caption);
                builder.AddAttribute(14, "Class", daysRemaining <= 30 ? "warranty-expiring" : "mud-text-secondary");
                builder.AddAttribute(15, "ChildContent", (RenderFragment)(b => b.AddContent(16, $"{daysRemaining} days remaining")));
                builder.CloseComponent();
            }
            else
            {
                builder.OpenComponent<MudText>(17);
                builder.AddAttribute(18, "Typo", Typo.caption);
                builder.AddAttribute(19, "Class", "warranty-expired");
                builder.AddAttribute(20, "ChildContent", (RenderFragment)(b => b.AddContent(21, $"Expired {Math.Abs(daysRemaining)} days ago")));
                builder.CloseComponent();
            }

            builder.CloseElement(); // div
            builder.CloseElement(); // warranty-status div
        };
    }
}