namespace WebApiTemplate.Extensions
{
    /// <summary>
    /// Extension methods for DateTime operations, especially for auctions
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Check if auction/payment has expired
        /// </summary>
        /// <param name="expiryTime">The expiry timestamp</param>
        /// <returns>True if expired, false otherwise</returns>
        public static bool HasExpired(this DateTime expiryTime)
        {
            return expiryTime < DateTime.UtcNow;
        }

        /// <summary>
        /// Get time remaining in human-readable format
        /// </summary>
        /// <param name="expiryTime">The expiry timestamp</param>
        /// <returns>Formatted string like "5m remaining" or "Expired"</returns>
        public static string GetTimeRemaining(this DateTime expiryTime)
        {
            var timeLeft = expiryTime - DateTime.UtcNow;

            if (timeLeft.TotalSeconds < 0)
                return "Expired";

            if (timeLeft.TotalMinutes < 1)
                return $"{(int)timeLeft.TotalSeconds}s remaining";

            if (timeLeft.TotalHours < 1)
                return $"{(int)timeLeft.TotalMinutes}m remaining";

            if (timeLeft.TotalDays < 1)
                return $"{(int)timeLeft.TotalHours}h remaining";

            return $"{(int)timeLeft.TotalDays}d remaining";
        }

        /// <summary>
        /// Check if time is within the last N minutes (useful for anti-snipe extension)
        /// </summary>
        /// <param name="expiryTime">The expiry timestamp</param>
        /// <param name="minutes">Number of minutes to check</param>
        /// <returns>True if within the specified window</returns>
        public static bool IsWithinLastMinutes(this DateTime expiryTime, int minutes)
        {
            var threshold = expiryTime.AddMinutes(-minutes);
            return DateTime.UtcNow >= threshold && DateTime.UtcNow <= expiryTime;
        }

        /// <summary>
        /// Check if timestamp is recent (within last N seconds)
        /// </summary>
        /// <param name="timestamp">The timestamp to check</param>
        /// <param name="seconds">Number of seconds</param>
        /// <returns>True if recent</returns>
        public static bool IsRecent(this DateTime timestamp, int seconds = 30)
        {
            return (DateTime.UtcNow - timestamp).TotalSeconds <= seconds;
        }

        /// <summary>
        /// Get seconds until expiry
        /// </summary>
        /// <param name="expiryTime">The expiry timestamp</param>
        /// <returns>Seconds remaining (0 if expired)</returns>
        public static int GetSecondsUntilExpiry(this DateTime expiryTime)
        {
            var seconds = (int)(expiryTime - DateTime.UtcNow).TotalSeconds;
            return seconds > 0 ? seconds : 0;
        }
    }
}

