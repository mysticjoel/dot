using WebApiTemplate.Repository.Database.Entities;

namespace WebApiTemplate.Service.Helpers
{
    /// <summary>
    /// Helper methods for auction-related operations
    /// </summary>
    public static class AuctionHelpers
    {
        /// <summary>
        /// Gets display name for a user (Name or Email fallback)
        /// </summary>
        /// <param name="user">User entity</param>
        /// <returns>Display name</returns>
        public static string GetUserDisplayName(User? user)
        {
            if (user == null) return "Unknown";
            return user.Name ?? user.Email;
        }

        /// <summary>
        /// Calculates time remaining in minutes
        /// </summary>
        /// <param name="expiryTime">Expiry time</param>
        /// <param name="currentTime">Current time (optional, defaults to UtcNow)</param>
        /// <returns>Minutes remaining or null if expired</returns>
        public static int? CalculateTimeRemainingMinutes(DateTime? expiryTime, DateTime? currentTime = null)
        {
            if (!expiryTime.HasValue) return null;

            var now = currentTime ?? DateTime.UtcNow;
            if (expiryTime <= now) return null;

            return (int)(expiryTime.Value - now).TotalMinutes;
        }

        /// <summary>
        /// Calculates expiry time from duration in minutes
        /// </summary>
        /// <param name="durationMinutes">Duration in minutes</param>
        /// <param name="startTime">Start time (optional, defaults to UtcNow)</param>
        /// <returns>Expiry time</returns>
        public static DateTime CalculateExpiryTime(int durationMinutes, DateTime? startTime = null)
        {
            var start = startTime ?? DateTime.UtcNow;
            return start.AddMinutes(durationMinutes);
        }

        /// <summary>
        /// Checks if auction is active based on expiry time and status
        /// </summary>
        /// <param name="expiryTime">Expiry time</param>
        /// <param name="status">Auction status</param>
        /// <param name="currentTime">Current time (optional, defaults to UtcNow)</param>
        /// <returns>True if auction is active</returns>
        public static bool IsAuctionActive(DateTime expiryTime, string status, DateTime? currentTime = null)
        {
            var now = currentTime ?? DateTime.UtcNow;
            return status == "Active" && expiryTime > now;
        }
    }
}

