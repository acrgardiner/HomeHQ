using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeHQ.Components.Entities.Common;

public static class NavigationStateHelper
{
    public static JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Encode<T>(T obj)
    {
        if (obj == null)
        {
            return string.Empty;
        }

        var json = JsonSerializer.Serialize(obj, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes);
    }

    public static T? Decode<T>(string encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded))
        {
            return default;
        }

        try
        {
            var bytes = Convert.FromBase64String(encoded);
            var json = Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default(T);
        }
    }

    public static string? DecodeUrl(ReturnState state)
    {
        string query = state?.Destination ?? string.Empty;

        string paramString = string.Empty;

        foreach (var param in state?.Parameters?.GetType().GetProperties() ?? Array.Empty<System.Reflection.PropertyInfo>())
        {
            var value = param.GetValue(state?.Parameters);
            if (value != null)
            {
                if (paramString.Length > 0)
                {
                    paramString += "&";
                }
                else
                {
                    paramString += "?";
                }

                paramString += $"{param.Name.ToLower()}={Uri.EscapeDataString(value.ToString() ?? string.Empty)}";
            }
        }

        return query + paramString;
    }

    public static string? DecodeReturnUrl(string? stateStr, string? defaultDestination = null)
    {
        var state = Decode<ReturnState>(stateStr ?? string.Empty);

        if (state is null)
        {
            return defaultDestination;
        }

        string? query = state?.Destination ?? defaultDestination;

        string paramString = string.Empty;

        if (state?.Parameters is not null)
        {
            paramString += "?state=" + Encode(state.Parameters);
        }

        return query + paramString;
    }

    public static ReturnState? CreateListState(string destination, int? page, string? search, int? pageSize = null)
    {
        // Only create state if there's meaningful data to preserve
        if ((page == null || page == 0) && string.IsNullOrWhiteSpace(search) && (pageSize == null || pageSize == 10))
        {
            return null;
        }

        if (page == 0)
        {
            page = null;
        }

        if (search == string.Empty)
        {
            search = null;
        }

        return new ReturnState
        {
            Destination = destination,
            Parameters = new NavigationParameters
            {
                Page = page,
                Search = search,
                PageSize = pageSize
            }
        };
    }

    public static ReturnState CreateState(string destination)
    {
        return new ReturnState
        {
            Destination = destination,
            Parameters = null
        };
    }

    /// <summary>
    /// Navigates back to the return state destination or a default destination
    /// </summary>
    /// <param name="navigation">The NavigationManager instance</param>
    /// <param name="returnState">The encoded return state string</param>
    /// <param name="defaultDestination">The default destination if no return state is provided</param>
    public static void GoBack(this Microsoft.AspNetCore.Components.NavigationManager navigation, string? returnState, string defaultDestination)
    {
        // Decode state to get destination
        string destination = DecodeReturnUrl(returnState) ?? defaultDestination;
        navigation.NavigateTo(destination);
    }
}

