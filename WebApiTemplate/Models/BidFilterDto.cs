namespace WebApiTemplate.Models
{
    /// <summary>
    /// DTO for filtering bids with query parameters
    /// </summary>
    public class BidFilterDto
    {
        /// <summary>
        /// Filter by bidder user ID
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Filter by product ID
        /// </summary>
        public int? ProductId { get; set; }

        /// <summary>
        /// Minimum bid amount
        /// </summary>
        public decimal? MinAmount { get; set; }

        /// <summary>
        /// Maximum bid amount
        /// </summary>
        public decimal? MaxAmount { get; set; }

        /// <summary>
        /// Start date for bid timestamp filter
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// End date for bid timestamp filter
        /// </summary>
        public DateTime? EndDate { get; set; }
    }
}

