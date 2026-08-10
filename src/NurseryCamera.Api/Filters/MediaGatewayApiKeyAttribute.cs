using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using NurseryCamera.Application.Common.Models;
using NurseryCamera.Application.Common.Options;

namespace NurseryCamera.Api.Filters;

/// <summary>
/// Machine-to-machine guard for the internal media-gateway endpoints (spec section 15).
/// The media gateway is not an end-user/parent, so it cannot present a JWT; instead it must
/// present the shared secret configured under MediaGateway:ApiKey via the
/// <c>X-Media-Gateway-Key</c> header. Fails closed: a missing/misconfigured key always denies.
/// </summary>
public sealed class MediaGatewayApiKeyAttribute : ActionFilterAttribute
{
    public const string HeaderName = "X-Media-Gateway-Key";

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<MediaGatewayOptions>>().Value;

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            context.Result = new ObjectResult(new ApiError(
                "MEDIA_GATEWAY_NOT_CONFIGURED",
                "Media gateway authentication is not configured.",
                context.HttpContext.TraceIdentifier))
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
            return;
        }

        var providedKey = context.HttpContext.Request.Headers[HeaderName].ToString();
        if (string.IsNullOrEmpty(providedKey) || providedKey != options.ApiKey)
        {
            context.Result = new ObjectResult(new ApiError(
                "UNAUTHORIZED_GATEWAY",
                "Invalid or missing media gateway credentials.",
                context.HttpContext.TraceIdentifier))
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        base.OnActionExecuting(context);
    }
}
