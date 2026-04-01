using System.Globalization;

namespace HomeHQ.Helpers;

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
}
