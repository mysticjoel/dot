namespace WebApiTemplate.Models
{
    /// <summary>
    /// DTO for bid information
    /// </summary>
    public class BidDto
    {
        /// <summary>
        /// Bid ID
        /// </summary>
        public int BidId { get; set; }

        /// <summary>
        /// Bidder user ID
        /// </summary>
        public int BidderId { get; set; }

        /// <summary>
        /// Bidder username
        /// </summary>
        public string BidderName { get; set; } = default!;

        /// <summary>
        /// Bid amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Timestamp when bid was placed
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}

