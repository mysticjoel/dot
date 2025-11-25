namespace WebApiTemplate.Models
{
    public class ProductDto
    {
        public int ProductId { get; set; }

        // Use 'required' to ensure callers set these fields
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required string Category { get; set; }

        public decimal StartingPrice { get; set; }
        public int AuctionDuration { get; set; }

        public int OwnerId { get; set; }

        public int? HighestBidId { get; set; }
        public DateTime? ExpiryTime { get; set; }
    }
}