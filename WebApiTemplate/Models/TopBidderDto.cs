namespace WebApiTemplate.Models
{
    /// <summary>
    /// DTO for top bidder statistics
    /// </summary>
    public class TopBidderDto
    {
        /// <summary>
        /// User ID of the bidder
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Username or email of the bidder
        /// </summary>
        public string Username { get; set; } = default!;

        /// <summary>
        /// Total amount of all bids placed by this user
        /// </summary>
        public decimal TotalBidAmount { get; set; }

        /// <summary>
        /// Total number of bids placed by this user
        /// </summary>
        public int TotalBidsCount { get; set; }

        /// <summary>
        /// Number of auctions won by this user
        /// </summary>
        public int AuctionsWon { get; set; }

        /// <summary>
        /// Win rate percentage (auctions won / unique auctions participated)
        /// </summary>
        public decimal WinRate { get; set; }
    }
}

