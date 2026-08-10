namespace NurseryCamera.Application.Common.Exceptions;

/// <summary>
/// Base exception for every application-layer failure. Carries a stable machine-readable
/// <see cref="Code"/> (see spec section 26 for the recommended catalog) plus the HTTP status
/// the API layer should map it to. Handlers should prefer the named factory methods below
/// over calling the constructor directly so codes/status codes stay consistent.
/// </summary>
public class AppException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; }

    public AppException(string code, string message, int statusCode = 400, IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
        Errors = errors;
    }

    public static AppException AuthenticationRequired(string message = "Authentication is required.")
        => new("AUTHENTICATION_REQUIRED", message, 401);

    public static AppException InvalidCredentials(string message = "Invalid email or password.")
        => new("INVALID_CREDENTIALS", message, 401);

    public static AppException Forbidden(string code = "FORBIDDEN", string message = "You are not authorized to perform this action.")
        => new(code, message, 403);

    public static AppException NotFound(string code, string message)
        => new(code, message, 404);

    public static AppException Conflict(string code, string message)
        => new(code, message, 409);

    public static AppException Validation(string message)
        => new("VALIDATION_ERROR", message, 422);

    public static AppException ValidationFailed(IReadOnlyDictionary<string, string[]> errors)
        => new("VALIDATION_ERROR", "One or more validation errors occurred.", 422, errors);

    public static AppException RateLimitExceeded(string message = "Too many requests.")
        => new("RATE_LIMIT_EXCEEDED", message, 429);
}
