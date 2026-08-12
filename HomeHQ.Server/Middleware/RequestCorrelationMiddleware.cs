using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace HomeHQ.Server.Middleware;

public class RequestCorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationHeader = "X-Correlation-ID";

    public RequestCorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = context.Request.Headers.ContainsKey(CorrelationHeader)
            ? context.Request.Headers[CorrelationHeader].ToString()
            : Activity.Current?.Id ?? Guid.NewGuid().ToString();

        // Add to response header
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationHeader))
            {
                context.Response.Headers.Add(CorrelationHeader, correlationId);
            }
            return Task.CompletedTask;
        });

        // Push into Serilog context for structured logs
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            context.Items[CorrelationHeader] = correlationId;
            await _next(context);
        }
    }
}
