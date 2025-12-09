using System.Net;
using System.Text.Json;
using FreeStays.Domain.Exceptions;

namespace FreeStays.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";
        
        var errorResponse = new ErrorResponse
        {
            TraceId = context.TraceIdentifier
        };
        
        switch (exception)
        {
            case ValidationException validationEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = "Validation failed";
                errorResponse.Errors = validationEx.Errors;
                _logger.LogWarning(exception, "Validation error: {Message}", validationEx.Message);
                break;
            
            case NotFoundException notFoundEx:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Message = notFoundEx.Message;
                _logger.LogWarning(exception, "Not found: {Message}", notFoundEx.Message);
                break;
            
            case UnauthorizedException unauthorizedEx:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.Message = unauthorizedEx.Message;
                _logger.LogWarning(exception, "Unauthorized: {Message}", unauthorizedEx.Message);
                break;
            
            case ForbiddenException forbiddenEx:
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                errorResponse.Message = forbiddenEx.Message;
                _logger.LogWarning(exception, "Forbidden: {Message}", forbiddenEx.Message);
                break;
            
            case ExternalServiceException externalEx:
                response.StatusCode = (int)HttpStatusCode.BadGateway;
                errorResponse.Message = $"External service error: {externalEx.ServiceName}";
                _logger.LogError(exception, "External service error: {Service} - {Message}", 
                    externalEx.ServiceName, externalEx.Message);
                break;
            
            case BusinessRuleException businessEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = businessEx.Message;
                _logger.LogWarning(exception, "Business rule violation: {Message}", businessEx.Message);
                break;
            
            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Message = "An unexpected error occurred";
                _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
                break;
        }
        
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        await response.WriteAsJsonAsync(errorResponse, options);
    }
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string? TraceId { get; set; }
    public IDictionary<string, string[]>? Errors { get; set; }
}
