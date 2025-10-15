using Microsoft.AspNetCore.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace projectaardvarkx2.Components.Entities.Common;

public static class NavigationStateHelper
{
    public static JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Encode(ReturnState state)
    {
        if (state == null)
            return string.Empty;

        var json = JsonSerializer.Serialize(state, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes);
    }

    public static ReturnState? Decode(string encodedState)
    {
        if (string.IsNullOrWhiteSpace(encodedState))
            return null;

        try
        {
            var bytes = Convert.FromBase64String(encodedState);
            var json = Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<ReturnState>(json);
        }
        catch
        {
            return null;
        }
    }

    public static string? DecodeUrl(ReturnState state)
    {
        string query = string.Empty;

        query = state?.Destination ?? string.Empty;

        string paramString = string.Empty;

        foreach (var param in state?.Parameters?.GetType().GetProperties() ?? Array.Empty<System.Reflection.PropertyInfo>())
        {
            var value = param.GetValue(state.Parameters);
            if (value != null)
            {
                if (paramString.Length > 0)
                    paramString += "&";
                else
                    paramString += "?";
                paramString += $"{param.Name.ToLower()}={Uri.EscapeDataString(value.ToString() ?? string.Empty)}";
            }
        }

        return query + paramString;
    }

    public static string? DecodeUrl(string? stateStr, string? defaultDestination = null)
    {
        var state = Decode(stateStr ?? string.Empty);

        if (state is null)
            return defaultDestination;

        string? query = state?.Destination ?? defaultDestination;

        string paramString = string.Empty;

        foreach (var param in state?.Parameters?.GetType().GetProperties() ?? Array.Empty<System.Reflection.PropertyInfo>())
        {
            var value = param.GetValue(state?.Parameters);
            if (value != null)
            {
                if (paramString.Length > 0)
                    paramString += "&";
                else
                    paramString += "?";
                paramString += $"{param.Name.ToLower()}={Uri.EscapeDataString(value.ToString() ?? string.Empty)}";
            }
        }

        return query + paramString;
    }

    public static ReturnState? CreateListState(string destination, int? page, string? search, int? pageSize = null)
    {
        // Only create state if there's meaningful data to preserve
        if ((page == null || page == 0) && string.IsNullOrWhiteSpace(search) && (pageSize == null || pageSize == 10))
            return null;

        if (page == 0)
            page = null;

        if (search == string.Empty)
            search = null;

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
}

