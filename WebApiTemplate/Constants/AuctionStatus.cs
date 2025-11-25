namespace WebApiTemplate.Constants
{
    /// <summary>
    /// Constants for auction status values
    /// </summary>
    public static class AuctionStatus
    {
        /// <summary>
        /// Auction is currently active and accepting bids
        /// </summary>
        public const string Active = "active";

        /// <summary>
        /// Auction has expired but not yet finalized
        /// </summary>
        public const string Expired = "expired";

        /// <summary>
        /// Auction completed successfully with payment
        /// </summary>
        public const string Success = "success";

        /// <summary>
        /// Auction failed (e.g., payment failed, no bids)
        /// </summary>
        public const string Failed = "failed";
    }
}

