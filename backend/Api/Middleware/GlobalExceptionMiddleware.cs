using System.Text.Json;
using Application.DTOs;
using Domain.Exceptions;

namespace Api.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception occurred while processing request.");
            await WriteErrorResponseAsync(context, exception);
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, Exception exception)
    {
        // 先把常见业务异常和系统异常映射成统一的 HTTP 语义，后续可继续扩展。
        var (statusCode, code, message) = exception switch
        {
            DomainException => (StatusCodes.Status400BadRequest, "DOMAIN_ERROR", exception.Message),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "NOT_FOUND", exception.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "INTERNAL_SERVER_ERROR",
                context.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() == true
                    ? exception.GetBaseException().Message
                    : "服务器内部错误，请稍后重试。")
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = ApiResponse<object>.Failure(code, message, context.TraceIdentifier);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
