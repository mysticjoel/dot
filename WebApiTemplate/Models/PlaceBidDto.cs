namespace WebApiTemplate.Models
{
    /// <summary>
    /// DTO for placing a bid on an auction
    /// </summary>
    public class PlaceBidDto
    {
        /// <summary>
        /// The auction ID to place the bid on
        /// </summary>
        public int AuctionId { get; set; }

        /// <summary>
        /// The bid amount (must be greater than current highest bid)
        /// </summary>
        public decimal Amount { get; set; }
    }
}

