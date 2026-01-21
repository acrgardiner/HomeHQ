using System.Net;
using System.Net.Http.Headers;

namespace HomeHQ.Mobile.Services;

/// <summary>
/// HTTP message handler that automatically adds bearer token authentication to outgoing requests.
/// </summary>
public class AuthDelegatingHandler(AuthService authService) : DelegatingHandler
{
    private static readonly string[] AnonymousEndpoints = ["api/auth/login", "api/identity/login", "api/identity/register", "api/identity/refresh"];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Skip auth for login/register endpoints
        var requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
        var isAnonymousEndpoint = AnonymousEndpoints.Any(e => requestPath.Contains(e, StringComparison.OrdinalIgnoreCase));

        if (!isAnonymousEndpoint)
        {
            var token = await authService.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        // If we get 401 Unauthorized, try refreshing the token and retry once
        if (response.StatusCode == HttpStatusCode.Unauthorized && !isAnonymousEndpoint)
        {
            if (await authService.RefreshTokenAsync())
            {
                // Clone the request (can't resend the same request)
                var newRequest = await CloneRequestAsync(request);
                var newToken = await authService.GetAccessTokenAsync();
                
                if (!string.IsNullOrEmpty(newToken))
                {
                    newRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                    response.Dispose();
                    response = await base.SendAsync(newRequest, cancellationToken);
                }
            }
        }

        return response;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        // Copy headers
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Copy content if present
        if (request.Content != null)
        {
            var content = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(content);

            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }
}
