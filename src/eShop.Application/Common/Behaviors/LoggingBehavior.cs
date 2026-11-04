using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace eShop.Application.Common.Behaviors;

/// <summary>
/// Pipeline Behavior ghi log và đo thời gian xử lý của từng Request trong MediatR.
/// Cảnh báo (Warning) nếu một Request chạy quá 500ms.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("eShop Request Bắt đầu: {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

        if (elapsedMilliseconds > 500)
        {
            _logger.LogWarning("eShop Request Chạy chậm: {RequestName} ({ElapsedMilliseconds} ms)", 
                requestName, elapsedMilliseconds);
        }
        else
        {
            _logger.LogInformation("eShop Request Hoàn tất: {RequestName} trong {ElapsedMilliseconds} ms", 
                requestName, elapsedMilliseconds);
        }

        return response;
    }
}

