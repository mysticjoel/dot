namespace WebApiTemplate.Models
{
    /// <summary>
    /// DTO for active auctions with current bid information
    /// </summary>
    public class ActiveAuctionDto
    {
        /// <summary>
        /// Product ID
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Product name
        /// </summary>
        public string Name { get; set; } = default!;

        /// <summary>
        /// Product description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Product category
        /// </summary>
        public string Category { get; set; } = default!;

        /// <summary>
        /// Starting price
        /// </summary>
        public decimal StartingPrice { get; set; }

        /// <summary>
        /// Current highest bid amount
        /// </summary>
        public decimal? HighestBidAmount { get; set; }

        /// <summary>
        /// Name of the highest bidder
        /// </summary>
        public string? HighestBidderName { get; set; }

        /// <summary>
        /// Auction expiry time
        /// </summary>
        public DateTime ExpiryTime { get; set; }

        /// <summary>
        /// Time remaining in minutes
        /// </summary>
        public int TimeRemainingMinutes { get; set; }

        /// <summary>
        /// Auction status
        /// </summary>
        public string AuctionStatus { get; set; } = default!;
    }
}

