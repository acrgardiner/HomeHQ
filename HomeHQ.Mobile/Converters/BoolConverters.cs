using System.Globalization;
using HomeHQ.Helpers;

namespace HomeHQ.Mobile.Converters;

/// <summary>
/// Inverts a boolean value.
/// </summary>
public class InvertedBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool boolValue ? !boolValue : value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool boolValue ? !boolValue : value;
    }
}

/// <summary>
/// Converts boolean to "Hide"/"Show" or "▲"/"▼" text.
/// </summary>
public class BoolToExpandTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool isExpanded ? isExpanded ? "Hide ▲" : "Edit ▼" : "Edit";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a <see cref="float"/> byte count to a human-readable size string
/// (e.g. 1 572 864 → "1.5 MB") using <see cref="Formatter.FormatFileSize"/>.
/// </summary>
public class FileSizeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is float bytes ? Formatter.FormatFileSize(bytes) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts boolean IsBusy to different text values.
/// Parameter format: "NormalText|BusyText"
/// </summary>
public class BusyToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool isBusy)
        {
            return parameter?.ToString()?.Split('|').FirstOrDefault() ?? "Action";
        }

        var texts = parameter?.ToString()?.Split('|') ?? ["Action", "Loading..."];
        return isBusy ? (texts.Length > 1 ? texts[1] : "Loading...") : texts[0];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
