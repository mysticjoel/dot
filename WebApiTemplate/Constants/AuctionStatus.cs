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
    public const string Active = "Active";

    /// <summary>
    /// Auction is pending payment confirmation
    /// </summary>
    public const string PendingPayment = "pendingpayment";

    /// <summary>
    /// Auction completed successfully with confirmed payment
    /// </summary>
    public const string Completed = "completed";

    /// <summary>
    /// Auction failed (e.g., payment failed, no bids)
    /// </summary>
    public const string Failed = "failed";
    }
}
