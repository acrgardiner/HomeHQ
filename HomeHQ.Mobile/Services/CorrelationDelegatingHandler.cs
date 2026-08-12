using System.Diagnostics;
using System.Net.Http.Headers;
namespace HomeHQ.Mobile.Services;

public class CorrelationDelegatingHandler : DelegatingHandler
{
    private const string CORRELATION_HEADER = "X-Correlation-ID";
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Propagate correlation id from app settings or generate a new one
        if (!request.Headers.Contains(CORRELATION_HEADER))
        {
            var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            request.Headers.Add(CORRELATION_HEADER, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
