namespace WebApiTemplate.Configuration
{
    /// <summary>
    /// Configuration settings for payment processing
    /// </summary>
    public class PaymentSettings
    {
        /// <summary>
        /// Payment window duration in minutes (default: 1)
        /// </summary>
        public int WindowMinutes { get; set; } = 1;

        /// <summary>
        /// Maximum number of retry attempts for payment (default: 3)
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Interval in seconds to check for expired payment attempts (default: 30)
        /// </summary>
        public int RetryCheckIntervalSeconds { get; set; } = 30;
    }
}

