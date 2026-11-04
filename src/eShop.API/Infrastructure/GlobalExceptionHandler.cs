using eShop.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace eShop.API.Infrastructure;

/// <summary>
/// Chuẩn .NET 8 IExceptionHandler:
/// Tự động bắt mọi Exception không được xử lý trong pipeline HTTP
/// và chuyển thành định dạng JSON chuẩn quốc tế ProblemDetails (RFC 7807).
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Đã xảy ra ngoại lệ chưa được xử lý: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };

        switch (exception)
        {
            case ValidationException validationException:
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Dữ liệu yêu cầu không hợp lệ";
                problemDetails.Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1";
                problemDetails.Extensions["errors"] = validationException.Errors;
                break;

            case NotFoundException notFoundException:
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                problemDetails.Status = StatusCodes.Status404NotFound;
                problemDetails.Title = "Không tìm thấy tài nguyên";
                problemDetails.Detail = notFoundException.Message;
                problemDetails.Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4";
                break;

            case UnauthorizedAccessException:
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                problemDetails.Status = StatusCodes.Status401Unauthorized;
                problemDetails.Title = "Chưa được xác thực hoặc không có quyền";
                problemDetails.Type = "https://datatracker.ietf.org/doc/html/rfc7235#section-3.1";
                break;

            case BadHttpRequestException badHttpRequestException:
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Yêu cầu HTTP không hợp lệ";
                problemDetails.Detail = badHttpRequestException.Message;
                problemDetails.Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1";
                break;

            default:
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "Lỗi máy chủ nội bộ";
                problemDetails.Detail = "Đã xảy ra lỗi bất ngờ trên máy chủ. Vui lòng liên hệ quản trị viên.";
                problemDetails.Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1";
                break;
        }

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}

