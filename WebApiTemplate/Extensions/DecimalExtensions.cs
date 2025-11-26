namespace WebApiTemplate.Extensions
{
    /// <summary>
    /// Extension methods for decimal operations, especially for currency and bids
    /// </summary>
    public static class DecimalExtensions
    {
        /// <summary>
        /// Format amount as currency string
        /// </summary>
        /// <param name="amount">The amount to format</param>
        /// <returns>Formatted currency string (e.g., "$1,234.56")</returns>
        public static string ToCurrency(this decimal amount)
        {
            return $"${amount:N2}";
        }

        /// <summary>
        /// Check if new bid meets minimum increment requirement
        /// </summary>
        /// <param name="newBid">The new bid amount</param>
        /// <param name="currentBid">The current highest bid</param>
        /// <param name="minimumIncrement">Required minimum increment</param>
        /// <returns>True if bid is valid</returns>
        public static bool IsValidIncrement(
            this decimal newBid,
            decimal currentBid,
            decimal minimumIncrement)
        {
            return newBid >= currentBid + minimumIncrement;
        }

        /// <summary>
        /// Calculate platform fee based on percentage
        /// </summary>
        /// <param name="amount">The base amount</param>
        /// <param name="feePercentage">Fee percentage (e.g., 5 for 5%)</param>
        /// <returns>Calculated fee amount</returns>
        public static decimal CalculateFee(this decimal amount, decimal feePercentage)
        {
            return Math.Round(amount * (feePercentage / 100), 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Calculate total including fee
        /// </summary>
        /// <param name="amount">The base amount</param>
        /// <param name="feePercentage">Fee percentage</param>
        /// <returns>Total amount including fee</returns>
        public static decimal WithFee(this decimal amount, decimal feePercentage)
        {
            return amount + amount.CalculateFee(feePercentage);
        }

        /// <summary>
        /// Check if amount is positive
        /// </summary>
        /// <param name="amount">The amount to check</param>
        /// <returns>True if greater than zero</returns>
        public static bool IsPositive(this decimal amount)
        {
            return amount > 0;
        }

        /// <summary>
        /// Round to nearest cent
        /// </summary>
        /// <param name="amount">The amount to round</param>
        /// <returns>Rounded amount</returns>
        public static decimal RoundToCent(this decimal amount)
        {
            return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        }
    }
}

