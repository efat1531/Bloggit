
using System.Diagnostics;

namespace Bloggit.API.Middleware;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<RequestLoggingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        // Start timing the request
        var stopwatch = Stopwatch.StartNew();
        
        // Log the incoming request
        _logger.LogInformation(
            "🔵 Incoming Request: {Method} {Path} from {RemoteIp}",
            context.Request.Method,
            context.Request.Path,
            context.Connection.RemoteIpAddress);

        try
        {
            // Call the next middleware in the pipeline
            await _next(context);
            
            stopwatch.Stop();

            // Log the response based on status code
            var statusCode = context.Response.StatusCode;
            var emoji = GetStatusEmoji(statusCode);

            _logger.LogInformation(
                "{Emoji} Response: {Method} {Path} - Status: {StatusCode} - Duration: {Duration}ms",
                emoji,
                context.Request.Method,
                context.Request.Path,
                statusCode,
                stopwatch.ElapsedMilliseconds);

            // Special logging for 404 Not Found
            if (statusCode == 404)
            {
                _logger.LogWarning(
                    "❌ 404 Not Found: {Method} {Path} - Route not matched. Remote IP: {RemoteIp}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Connection.RemoteIpAddress);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(
                ex,
                "💥 Unhandled Exception: {Method} {Path} - Duration: {Duration}ms",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds);
            
            throw;
        }
    }

    private static string GetStatusEmoji(int statusCode)
    {
        return statusCode switch
        {
            >= 200 and < 300 => "✅",  // Success
            >= 300 and < 400 => "🔄",  // Redirect
            404 => "🔍",                // Not Found
            >= 400 and < 500 => "⚠️",   // Client Error
            >= 500 => "💥",             // Server Error
            _ => "ℹ️"
        };
    }
}