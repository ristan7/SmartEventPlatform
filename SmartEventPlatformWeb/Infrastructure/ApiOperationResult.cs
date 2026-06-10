using System.Net;

namespace SmartEventPlatformWeb.Infrastructure;

public sealed class ApiOperationResult
{
    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    public HttpStatusCode StatusCode { get; init; }

    public static ApiOperationResult Ok(HttpStatusCode statusCode)
    {
        return new ApiOperationResult
        {
            Success = true,
            StatusCode = statusCode
        };
    }

    public static ApiOperationResult Fail(HttpStatusCode statusCode, string errorMessage)
    {
        return new ApiOperationResult
        {
            Success = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }
}

public sealed class ApiOperationResult<T>
{
    public bool Success { get; init; }

    public T? Value { get; init; }

    public string? ErrorMessage { get; init; }

    public HttpStatusCode StatusCode { get; init; }

    public static ApiOperationResult<T> Ok(HttpStatusCode statusCode, T? value)
    {
        return new ApiOperationResult<T>
        {
            Success = true,
            StatusCode = statusCode,
            Value = value
        };
    }

    public static ApiOperationResult<T> Fail(HttpStatusCode statusCode, string errorMessage)
    {
        return new ApiOperationResult<T>
        {
            Success = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }
}