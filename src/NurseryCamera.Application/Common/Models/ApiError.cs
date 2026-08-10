namespace NurseryCamera.Application.Common.Models;

/// <summary>
/// Consistent error payload shape returned by the API for any failed request.
/// See spec section 26 (Error Model) for the recommended error codes.
/// </summary>
public sealed record ApiError(string Code, string Message, string? TraceId, IReadOnlyDictionary<string, string[]>? Errors = null);
