namespace NurseryCamera.Application.Common.Models;

/// <summary>
/// Lightweight result wrapper for operations where throwing is undesirable
/// (e.g. best-effort background/notification calls). Most request handlers in
/// this layer prefer throwing <see cref="Exceptions.AppException"/> so that the
/// <c>ValidationBehavior</c>/global exception handling pipeline can translate
/// failures into a consistent <see cref="ApiError"/> response.
/// </summary>
public readonly struct Result
{
    public bool IsSuccess { get; }
    public string? Code { get; }
    public string? Error { get; }

    private Result(bool isSuccess, string? code, string? error)
    {
        IsSuccess = isSuccess;
        Code = code;
        Error = error;
    }

    public static Result Success() => new(true, null, null);
    public static Result Failure(string code, string error) => new(false, code, error);
}

public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public string? Code { get; }
    public string? Error { get; }
    public T? Value { get; }

    private Result(bool isSuccess, T? value, string? code, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Code = code;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null, null);
    public static Result<T> Failure(string code, string error) => new(false, default, code, error);
}
