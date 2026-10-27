namespace ReservasSaaS.Application.Common;

/// <summary>
/// Uniform response envelope for the API.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message
    };

    public static ApiResponse<T> Fail(string message, string? errorCode = null, IReadOnlyDictionary<string, string[]>? validationErrors = null) => new()
    {
        Success = false,
        Message = message,
        ErrorCode = errorCode ?? "OPERATION_FAILED",
        ValidationErrors = validationErrors
    };
}
