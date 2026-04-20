using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Attachment = HomeHQ.Entities.Attachment;

namespace HomeHQ.Mobile.Services;

/// <summary>
/// HTTP client wrapper that uses the user-configurable API base URL.
/// </summary>
public class ApiClient
{
    private readonly SettingsService _settings;
    private readonly AuthService _authService;
    private readonly HttpClient _httpClient;

    private static readonly string[] AnonymousEndpoints = 
        ["api/auth/login", "api/identity/login", "api/identity/register", "api/identity/refresh"];

    public ApiClient(SettingsService settings, AuthService authService)
    {
        _settings = settings;
        _authService = authService;

        _httpClient = new HttpClient(new HttpClientHandler
        {
#if DEBUG
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
#endif
        });

        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    /// <summary>
    /// Gets the current API base URL.
    /// </summary>
    public string BaseUrl => _settings.ApiBaseUrl;

    /// <summary>
    /// Sends a GET request to the specified endpoint.
    /// </summary>
    public async Task<HttpResponseMessage> GetAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Get, endpoint);
        return await SendAsync(request, endpoint, cancellationToken);
    }

    /// <summary>
    /// Sends a GET request and deserializes the response.
    /// </summary>
    public async Task<T?> GetFromJsonAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        var response = await GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    /// <summary>
    /// Sends a POST request with JSON content.
    /// </summary>
    public async Task<HttpResponseMessage> PostAsJsonAsync<T>(string endpoint, T content, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Post, endpoint);
        request.Content = JsonContent.Create(content);
        return await SendAsync(request, endpoint, cancellationToken);
    }

    /// <summary>
    /// Sends a PUT request with JSON content.
    /// </summary>
    public async Task<HttpResponseMessage> PutAsJsonAsync<T>(string endpoint, T content, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Put, endpoint);
        request.Content = JsonContent.Create(content);
        return await SendAsync(request, endpoint, cancellationToken);
    }

    /// <summary>
    /// Sends a DELETE request.
    /// </summary>
    public async Task<HttpResponseMessage> DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Delete, endpoint);
        return await SendAsync(request, endpoint, cancellationToken);
    }

    /// <summary>
    /// Sends a POST request with multipart form data (for file uploads).
    /// </summary>
    public async Task<HttpResponseMessage> PostMultipartAsync(string endpoint, MultipartFormDataContent content, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Post, endpoint);
        request.Content = content;
        return await SendAsync(request, endpoint, cancellationToken);
    }

    /// <summary>
    /// Uploads an attachment to the server's AttachmentsController/Upload endpoint.
    /// Sends the file along with metadata fields expected by the server.
    /// </summary>
    public async Task<HttpResponseMessage> UploadAttachmentAsync(
        byte[] fileBytes,
        Attachment attachment,
        CancellationToken cancellationToken = default)
    {
        var multipart = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(fileBytes);
        if (!string.IsNullOrEmpty(attachment.ContentType))
        {
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(attachment.ContentType);
        }

        // 'file' is the parameter name expected by the controller
        multipart.Add(fileContent, "file", attachment.OriginFileName);
        // Add metadata fields expected by AttachmentUploadDto
        multipart.Add(new StringContent(attachment.ParentId.ToString() ?? string.Empty), "ParentId");
        multipart.Add(new StringContent(attachment.ParentType), "ParentType");
        multipart.Add(new StringContent(attachment.OriginFileName ?? attachment.LocalFileName), "OriginFileName");
        if (attachment.AttachmentTypeId.HasValue)
        {
            multipart.Add(new StringContent(attachment.AttachmentTypeId.Value.ToString()), "AttachmentTypeId");
        }

        if (!string.IsNullOrEmpty(attachment.ContentType))
        {
            multipart.Add(new StringContent(attachment.ContentType), "ContentType");
        }

        multipart.Add(new StringContent(Path.GetExtension(attachment.OriginFileName) ?? string.Empty), "Extension");

        return await PostMultipartAsync("api/attachments/upload", multipart, cancellationToken);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint)
    {
        var uri = new Uri(new Uri(_settings.ApiBaseUrl), endpoint);
        return new HttpRequestMessage(method, uri);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, string endpoint, CancellationToken cancellationToken)
    {
        var isAnonymous = AnonymousEndpoints.Any(e => endpoint.Contains(e, StringComparison.OrdinalIgnoreCase));

        // Add auth header if not anonymous
        if (!isAnonymous)
        {
            var token = await _authService.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);

        // Retry on 401 with token refresh
        if (response.StatusCode == HttpStatusCode.Unauthorized && !isAnonymous)
        {
            if (await _authService.RefreshTokenAsync())
            {
                var newRequest = CreateRequest(request.Method, endpoint);
                if (request.Content != null)
                {
                    var content = await request.Content.ReadAsByteArrayAsync(cancellationToken);
                    newRequest.Content = new ByteArrayContent(content);
                    foreach (var header in request.Content.Headers)
                    {
                        newRequest.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                var newToken = await _authService.GetAccessTokenAsync();
                if (!string.IsNullOrEmpty(newToken))
                {
                    newRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                    response.Dispose();
                    response = await _httpClient.SendAsync(newRequest, cancellationToken);
                }
            }
        }

        return response;
    }

}
