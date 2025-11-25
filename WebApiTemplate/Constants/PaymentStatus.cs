namespace WebApiTemplate.Constants
{
    /// <summary>
    /// Constants for payment attempt status values
    /// </summary>
    public static class PaymentStatus
    {
        /// <summary>
        /// Payment attempt is pending confirmation
        /// </summary>
        public const string Pending = "Pending";

        /// <summary>
        /// Payment confirmed successfully
        /// </summary>
        public const string Success = "Success";

        /// <summary>
        /// Payment attempt failed
        /// </summary>
        public const string Failed = "Failed";
    }
}

