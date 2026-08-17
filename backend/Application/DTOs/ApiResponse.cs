namespace Application.DTOs;

public sealed class ApiResponse<T>
{
    // Code/Message/Data/TraceId 是当前后端统一返回结构的最小约定。
    public string Code { get; init; } = "SUCCESS";
    public string Message { get; init; } = "OK";
    public T? Data { get; init; }
    public string? TraceId { get; init; }

    public static ApiResponse<T> Success(T? data, string? traceId = null, string message = "OK")
    {
        return new ApiResponse<T>
        {
            Data = data,
            TraceId = traceId,
            Message = message
        };
    }

    public static ApiResponse<T> Failure(string code, string message, string? traceId = null)
    {
        return new ApiResponse<T>
        {
            Code = code,
            Message = message,
            TraceId = traceId
        };
    }
}


