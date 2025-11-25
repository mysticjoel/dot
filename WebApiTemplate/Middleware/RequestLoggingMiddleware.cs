using System.Diagnostics;

namespace WebApiTemplate.Middleware
{
    /// <summary>
    /// Middleware for logging HTTP requests and responses with correlation IDs
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Generate correlation ID
            var correlationId = Guid.NewGuid().ToString();
            context.Items["CorrelationId"] = correlationId;

            // Add correlation ID to response headers
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.Append("X-Correlation-ID", correlationId);
                return Task.CompletedTask;
            });

            // Start timing
            var stopwatch = Stopwatch.StartNew();

            // Log request
            _logger.LogInformation(
                "HTTP {Method} {Path} started - CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                correlationId);

            try
            {
                // Call the next middleware
                await _next(context);

                stopwatch.Stop();

                // Log response
                _logger.LogInformation(
                    "HTTP {Method} {Path} completed with {StatusCode} in {Duration}ms - CorrelationId: {CorrelationId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    correlationId);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Log error
                _logger.LogError(
                    ex,
                    "HTTP {Method} {Path} failed with exception after {Duration}ms - CorrelationId: {CorrelationId}",
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds,
                    correlationId);

                throw;
            }
        }
    }

    /// <summary>
    /// Extension method to register the middleware
    /// </summary>
    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}

