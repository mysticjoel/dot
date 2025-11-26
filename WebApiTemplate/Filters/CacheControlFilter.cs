using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebApiTemplate.Filters
{
    /// <summary>
    /// Sets appropriate cache control headers for API responses
    /// </summary>
    public class CacheControlFilter : IActionFilter
    {
        private readonly int _cacheDurationSeconds;

        public CacheControlFilter(int cacheDurationSeconds = 0)
        {
            _cacheDurationSeconds = cacheDurationSeconds;
        }

        /// <summary>
        /// Set cache headers before action execution
        /// </summary>
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // No action needed before execution
        }

        /// <summary>
        /// Set cache headers after action execution
        /// </summary>
        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Result is ObjectResult)
            {
                if (_cacheDurationSeconds > 0)
                {
                    // Allow caching for specified duration
                    context.HttpContext.Response.Headers["Cache-Control"] = 
                        $"public, max-age={_cacheDurationSeconds}";
                }
                else
                {
                    // No caching for dynamic data (default for auction system)
                    context.HttpContext.Response.Headers["Cache-Control"] = 
                        "no-cache, no-store, must-revalidate";
                    context.HttpContext.Response.Headers["Pragma"] = "no-cache";
                    context.HttpContext.Response.Headers["Expires"] = "0";
                }
            }
        }
    }
}

