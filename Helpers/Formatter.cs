using Microsoft.AspNetCore.Components;
using MudBlazor;
using HomeHQ.Entities;
using System.Globalization;

namespace HomeHQ.Helpers
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
            if (!date.HasValue)
                return "";

            return date.Value.Date.ToString("d", CultureInfo.CurrentCulture);
        }

        public static string FormatDate(DateTime? date, string format)
        {
            if (!date.HasValue)
                return "";

            return date.Value.Date.ToString(format, CultureInfo.CurrentCulture);
        }

        public static string FormatDateTime(DateTime? dateTime, string? format = null)
        {
            if (!dateTime.HasValue)
                return "";

            format ??= "g"; // General short date/time pattern
            return dateTime.Value.ToString(format, CultureInfo.CurrentCulture);
        }

        public static string FormatWarrantyStatus(DateTime? warrantyExpiration)
        {
            if (!warrantyExpiration.HasValue)
                return "No warranty";
            var today = DateTime.Today;
            var daysRemaining = (warrantyExpiration.Value - today).Days;
            return FormatWarrantyStatus(daysRemaining);
        }
        
        public static string FormatWarrantyStatus(int daysRemaining)
        {
            if (daysRemaining < 0)
                return "Expired";
            if (daysRemaining <= 30)
                return $"{daysRemaining} days remaining";
            if (daysRemaining > 20000)
                return "∞ days remaining";
            if (daysRemaining > 365)
                return $"{daysRemaining / 365} years remaining";
            if (daysRemaining > 30)
                return $"{daysRemaining / 30} months remaining";
            return $"{daysRemaining / 7} weeks remaining";
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
            builder.AddAttribute(10, "ChildContent", (RenderFragment)(b => b.AddContent(11, FormatDate(warrantyDate, "D"))));
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
            builder.AddAttribute(15, "ChildContent", (RenderFragment)(b => b.AddContent(16, FormatWarrantyStatus(daysRemaining))));
            builder.CloseComponent();

            builder.CloseElement(); // div
            builder.CloseElement(); // warranty-status div
        };
    }
}