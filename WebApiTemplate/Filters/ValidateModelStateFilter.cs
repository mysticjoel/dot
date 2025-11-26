using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebApiTemplate.Filters
{
    /// <summary>
    /// Automatically validates model state and returns 400 Bad Request if invalid
    /// </summary>
    public class ValidateModelStateFilter : IActionFilter
    {
        /// <summary>
        /// Validate model state before action execution
        /// </summary>
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    );

                context.Result = new BadRequestObjectResult(new
                {
                    message = "Model validation failed",
                    errors = errors
                });
            }
        }

        /// <summary>
        /// Called after action execution (no-op for this filter)
        /// </summary>
        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No action needed after execution
        }
    }
}

