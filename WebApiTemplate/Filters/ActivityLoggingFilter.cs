using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace WebApiTemplate.Filters
{
    /// <summary>
    /// Filter to log all API activity for audit purposes
    /// </summary>
    public class ActivityLoggingFilter : IAsyncActionFilter
    {
        private readonly ILogger<ActivityLoggingFilter> _logger;

        public ActivityLoggingFilter(ILogger<ActivityLoggingFilter> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Log before and after action execution
        /// </summary>
        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            // Extract user information
            var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? context.HttpContext.User.FindFirst("sub")?.Value 
                ?? "Anonymous";
            
            var userEmail = context.HttpContext.User.FindFirst(ClaimTypes.Email)?.Value 
                ?? context.HttpContext.User.Identity?.Name 
                ?? "Unknown";

            // Extract request information
            var controller = context.RouteData.Values["controller"];
            var action = context.RouteData.Values["action"];
            var method = context.HttpContext.Request.Method;
            var path = context.HttpContext.Request.Path;

            // Log before execution
            _logger.LogInformation(
                "API Request: {Method} {Path} | User: {UserId} ({Email}) | Action: {Controller}.{Action}",
                method, path, userId, userEmail, controller, action);

            // Execute the action
            var executedContext = await next();

            // Log after execution
            var statusCode = context.HttpContext.Response.StatusCode;
            var hasError = executedContext.Exception != null;

            if (hasError)
            {
                _logger.LogWarning(
                    "API Request Failed: {Method} {Path} | User: {UserId} | Status: {Status} | Error: {Error}",
                    method, path, userId, statusCode, executedContext.Exception?.Message);
            }
            else
            {
                _logger.LogInformation(
                    "API Response: {Method} {Path} | User: {UserId} | Status: {Status}",
                    method, path, userId, statusCode);
            }
        }
    }
}

