namespace NurseryCamera.Api.Middleware;

/// <summary>
/// Ensures every request carries a stable X-Correlation-Id: reuses the caller-supplied
/// value when present (so a client/media-gateway can trace a request across services),
/// otherwise generates a new one. The id is stashed on <see cref="HttpContext.Items"/> so
/// downstream middleware (e.g. <see cref="ExceptionHandlingMiddleware"/>) and logging can
/// use it as the ApiError TraceId, and it is always echoed back on the response.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) &&
                             !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items[ItemKey] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
