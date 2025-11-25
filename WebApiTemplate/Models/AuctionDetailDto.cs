namespace WebApiTemplate.Models
{
    /// <summary>
    /// DTO for detailed auction information including all bids
    /// </summary>
    public class AuctionDetailDto
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
        /// Auction duration in minutes
        /// </summary>
        public int AuctionDuration { get; set; }

        /// <summary>
        /// Owner user ID
        /// </summary>
        public int OwnerId { get; set; }

        /// <summary>
        /// Owner username
        /// </summary>
        public string? OwnerName { get; set; }

        /// <summary>
        /// Auction expiry time
        /// </summary>
        public DateTime? ExpiryTime { get; set; }

        /// <summary>
        /// Current highest bid amount
        /// </summary>
        public decimal? HighestBidAmount { get; set; }

        /// <summary>
        /// Time remaining in minutes (null if expired or not started)
        /// </summary>
        public int? TimeRemainingMinutes { get; set; }

        /// <summary>
        /// Auction status
        /// </summary>
        public string? AuctionStatus { get; set; }

        /// <summary>
        /// List of all bids placed on this auction
        /// </summary>
        public List<BidDto> Bids { get; set; } = new List<BidDto>();
    }
}

