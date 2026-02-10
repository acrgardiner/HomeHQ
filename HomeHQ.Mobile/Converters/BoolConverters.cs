using System.Globalization;

namespace HomeHQ.Mobile.Converters;

/// <summary>
/// Inverts a boolean value.
/// </summary>
public class InvertedBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        return value;
    }
}

/// <summary>
/// Converts boolean to "Hide"/"Show" or "▲"/"▼" text.
/// </summary>
public class BoolToExpandTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isExpanded)
            return isExpanded ? "Hide ▲" : "Edit ▼";
        return "Edit";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
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
            return parameter?.ToString()?.Split('|').FirstOrDefault() ?? "Action";

        var texts = parameter?.ToString()?.Split('|') ?? ["Action", "Loading..."];
        return isBusy ? (texts.Length > 1 ? texts[1] : "Loading...") : texts[0];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}