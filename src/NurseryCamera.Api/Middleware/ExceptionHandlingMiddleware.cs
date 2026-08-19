using System.Net;
using System.Text.Json;
using NurseryCamera.Application.Common.Exceptions;
using NurseryCamera.Application.Common.Models;
using NurseryCamera.Domain.Exceptions;

namespace NurseryCamera.Api.Middleware;

/// <summary>
/// Single place where every unhandled exception is translated into the consistent
/// <see cref="ApiError"/> JSON shape (spec section 26). <see cref="AppException"/> already
/// carries a machine-readable code/status; <see cref="DomainException"/> is mapped to a
/// best-effort status via <see cref="MapDomainExceptionStatusCode"/>; anything else is
/// logged in full server-side and reduced to a generic 500 so internal details never leak.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions ErrorSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            _logger.LogWarning(ex, "Request failed with application error {Code}.", ex.Code);
            await WriteResponseAsync(context, ex.StatusCode, new ApiError(ex.Code, ex.Message, GetTraceId(context), ex.Errors));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Request failed with domain error {Code}.", ex.Code);
            await WriteResponseAsync(context, MapDomainExceptionStatusCode(ex.Code), new ApiError(ex.Code, ex.Message, GetTraceId(context)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing request {Path}.", context.Request.Path);
            await WriteResponseAsync(
                context,
                (int)HttpStatusCode.InternalServerError,
                new ApiError("INTERNAL_SERVER_ERROR", "An unexpected error occurred.", GetTraceId(context)));
        }
    }

    private static int MapDomainExceptionStatusCode(string code) => code switch
    {
        "CAMERA_ACCESS_DENIED" => StatusCodes.Status403Forbidden,
        "CHILD_NOT_PRESENT" => StatusCodes.Status409Conflict,
        "VIEWING_LIMIT_REACHED" => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest
    };

    private static string GetTraceId(HttpContext context) =>
        context.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var value) && value is string correlationId
            ? correlationId
            : context.TraceIdentifier;

    private static Task WriteResponseAsync(HttpContext context, int statusCode, ApiError error)
    {
        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(error, ErrorSerializerOptions);

        return context.Response.WriteAsync(json);
    }
}
