using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FreeStays.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }
    
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        _logger.LogInformation("FreeStays Request: {Name} {@Request}", requestName, request);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var response = await next();
            
            stopwatch.Stop();
            
            if (stopwatch.ElapsedMilliseconds > 500)
            {
                _logger.LogWarning("FreeStays Long Running Request: {Name} ({ElapsedMilliseconds} ms) {@Request}",
                    requestName, stopwatch.ElapsedMilliseconds, request);
            }
            
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "FreeStays Request Error: {Name} ({ElapsedMilliseconds} ms) {@Request}",
                requestName, stopwatch.ElapsedMilliseconds, request);
            throw;
        }
    }
}
