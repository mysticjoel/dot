using System.Security.Claims;

namespace WebApiTemplate.Extensions
{
    /// <summary>
    /// Extension methods for ClaimsPrincipal to simplify user claim extraction
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Get user ID from claims (handles both NameIdentifier and "sub")
        /// </summary>
        /// <param name="principal">The claims principal</param>
        /// <returns>User ID as integer, or null if not found</returns>
        public static int? GetUserId(this ClaimsPrincipal principal)
        {
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? principal.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return null;

            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        /// <summary>
        /// Get user ID from claims (throws if not found)
        /// </summary>
        /// <param name="principal">The claims principal</param>
        /// <returns>User ID as integer</returns>
        /// <exception cref="InvalidOperationException">If user ID not found in claims</exception>
        public static int GetUserIdOrThrow(this ClaimsPrincipal principal)
        {
            var userId = principal.GetUserId();
            if (!userId.HasValue)
            {
                throw new InvalidOperationException("User ID not found in claims");
            }
            return userId.Value;
        }

        /// <summary>
        /// Get user email from claims
        /// </summary>
        /// <param name="principal">The claims principal</param>
        /// <returns>User email, or null if not found</returns>
        public static string? GetUserEmail(this ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.Email)?.Value
                ?? principal.FindFirst("email")?.Value;
        }

        /// <summary>
        /// Get user role from claims
        /// </summary>
        /// <param name="principal">The claims principal</param>
        /// <returns>User role, or null if not found</returns>
        public static string? GetUserRole(this ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.Role)?.Value
                ?? principal.FindFirst("role")?.Value;
        }

        /// <summary>
        /// Check if user is in admin role
        /// </summary>
        /// <param name="principal">The claims principal</param>
        /// <returns>True if user is admin</returns>
        public static bool IsAdmin(this ClaimsPrincipal principal)
        {
            return principal.IsInRole("Admin");
        }

        /// <summary>
        /// Check if user is authenticated
        /// </summary>
        /// <param name="principal">The claims principal</param>
        /// <returns>True if authenticated</returns>
        public static bool IsAuthenticated(this ClaimsPrincipal principal)
        {
            return principal.Identity?.IsAuthenticated ?? false;
        }
    }
}

