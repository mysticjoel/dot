namespace WebApiTemplate.Models
{
    /// <summary>
    /// DTO for creating a new product
    /// </summary>
    public class CreateProductDto
    {
        /// <summary>
        /// Product name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Product description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Product category
        /// </summary>
        public required string Category { get; set; }

        /// <summary>
        /// Starting price for the auction
        /// </summary>
        public decimal StartingPrice { get; set; }

        /// <summary>
        /// Auction duration in minutes (2-1440)
        /// </summary>
        public int AuctionDuration { get; set; }
    }
}

