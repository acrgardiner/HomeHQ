using System.Net.Http.Headers;
using System.Net.Http.Json;
using HomeHQ.DTOs;

namespace HomeHQ.Mobile.Services;

public class AuthService
{
    private readonly SettingsService _settings;
    private readonly HttpClient _httpClient;

    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";
    private const string TokenExpiryKey = "token_expiry";
    private const string UsernameKey = "username";
    private const string RolesKey = "roles";
    private const string AdminRole = "Admin";

    public AuthService(SettingsService settings)
    {
        _settings = settings;
        _httpClient = new HttpClient(new HttpClientHandler
        {
#if DEBUG
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
#endif
        });
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    /// <summary>
    /// Authenticates the user and stores tokens securely.
    /// </summary>
    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        try
        {
            var uri = BuildUri("api/auth/login");
            var response = await _httpClient.PostAsJsonAsync(uri, new LoginRequest(username, password));

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();
                if (result is not null)
                {
                    await StoreTokensAsync(result);
                    await SecureStorage.Default.SetAsync(UsernameKey, username);
                    await RefreshUserInfoAsync();
                    return AuthResult.Success();
                }
            }

            return AuthResult.Failure(response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? "Invalid username or password"
                : $"Login failed: {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return AuthResult.Failure($"Connection error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return AuthResult.Failure($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Refreshes the access token using the stored refresh token.
    /// </summary>
    public async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
            if (string.IsNullOrEmpty(refreshToken))
                return false;

            var uri = BuildUri("api/identity/refresh");
            var response = await _httpClient.PostAsJsonAsync(uri, new RefreshRequest(refreshToken));

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();
                if (result is not null)
                {
                    await StoreTokensAsync(result);
                    return true;
                }
            }

            // Refresh failed - clear tokens and require re-login
            Logout();
            return false;
        }
        catch
        {
            return false;
        }
    }

    private Uri BuildUri(string endpoint) => new(new Uri(_settings.ApiBaseUrl), endpoint);

    /// <summary>
    /// Gets the current access token, refreshing if necessary.
    /// </summary>
    public async Task<string?> GetAccessTokenAsync()
    {
        var token = await SecureStorage.Default.GetAsync(AccessTokenKey);
        if (string.IsNullOrEmpty(token))
            return null;

        // Check if token is expired or about to expire (within 1 minute)
        var expiryString = await SecureStorage.Default.GetAsync(TokenExpiryKey);
        if (DateTime.TryParse(expiryString, out var expiry) && expiry <= DateTime.UtcNow.AddMinutes(1))
        {
            // Token expired or expiring soon - try to refresh
            if (await RefreshTokenAsync())
            {
                return await SecureStorage.Default.GetAsync(AccessTokenKey);
            }
            return null;
        }

        return token;
    }

    /// <summary>
    /// Checks if the user is currently authenticated.
    /// </summary>
    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetAccessTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    /// <summary>
    /// Logs out the user and clears all stored tokens.
    /// </summary>
    public void Logout()
    {
        SecureStorage.Default.Remove(AccessTokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
        SecureStorage.Default.Remove(TokenExpiryKey);
        SecureStorage.Default.Remove(UsernameKey);
        SecureStorage.Default.Remove(RolesKey);
    }

    private async Task StoreTokensAsync(AccessTokenResponse tokens)
    {
        await SecureStorage.Default.SetAsync(AccessTokenKey, tokens.AccessToken);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, tokens.RefreshToken);

        // Calculate and store expiry time
        var expiry = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn);
        await SecureStorage.Default.SetAsync(TokenExpiryKey, expiry.ToString("O"));
    }

    // Add a method to get the username
    public async Task<string?> GetUsernameAsync()
    {
        return await SecureStorage.Default.GetAsync(UsernameKey);
    }

    /// <summary>
    /// Returns true when the signed-in user has the Admin role.
    /// </summary>
    public async Task<bool> IsAdminAsync()
    {
        var roles = await GetRolesAsync();
        return roles.Contains(AdminRole, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync()
    {
        var stored = await SecureStorage.Default.GetAsync(RolesKey);
        if (string.IsNullOrWhiteSpace(stored))
        {
            return [];
        }

        return stored.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// Loads the current user's profile (including roles) from the API and caches them.
    /// </summary>
    public async Task<bool> RefreshUserInfoAsync()
    {
        try
        {
            var token = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, BuildUri("api/auth/me"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<CurrentUserDto>>();
            if (apiResponse?.Success != true || apiResponse.Data is null)
            {
                return false;
            }

            await SecureStorage.Default.SetAsync(UsernameKey, apiResponse.Data.UserName);
            await SecureStorage.Default.SetAsync(
                RolesKey,
                string.Join(',', apiResponse.Data.Roles));
            return true;
        }
        catch
        {
            return false;
        }
    }
}

#region DTOs

public record LoginRequest(string Username, string Password);

public record RefreshRequest(string RefreshToken);

public record AccessTokenResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public long ExpiresIn { get; init; }
    public string TokenType { get; init; } = "Bearer";
}

public record AuthResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static AuthResult Success() => new() { IsSuccess = true };
    public static AuthResult Failure(string message) => new() { IsSuccess = false, ErrorMessage = message };
}

#endregion
