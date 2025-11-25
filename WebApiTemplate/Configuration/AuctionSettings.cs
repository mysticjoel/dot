namespace WebApiTemplate.Configuration
{
    /// <summary>
    /// Configuration settings for auction behavior
    /// </summary>
    public class AuctionSettings
    {
        /// <summary>
        /// Time window (in minutes) before auction expiry that triggers automatic extension
        /// Default: 1 minute
        /// </summary>
        public int ExtensionThresholdMinutes { get; set; } = 1;

        /// <summary>
        /// Duration (in minutes) to extend the auction when a bid is placed within threshold
        /// Default: 1 minute
        /// </summary>
        public int ExtensionDurationMinutes { get; set; } = 1;

        /// <summary>
        /// Interval (in seconds) at which the background service checks for expired auctions
        /// Default: 30 seconds
        /// </summary>
        public int MonitoringIntervalSeconds { get; set; } = 30;
    }
}

