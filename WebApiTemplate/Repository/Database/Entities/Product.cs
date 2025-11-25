using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

namespace WebApiTemplate.Repository.Database.Entities
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = default!;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = default!;

        // Consider aligning precision with your business rules (e.g., money scale 2)
        [Column(TypeName = "numeric(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "StartingPrice must be > 0")]
        public decimal StartingPrice { get; set; }

        // Duration in minutes (2 to 1440)
        [Range(2, 24 * 60, ErrorMessage = "AuctionDuration must be between 2 minutes and 24 hours")]
        public int AuctionDuration { get; set; }

        // FK to User
        [ForeignKey(nameof(Owner))]
        public int OwnerId { get; set; }

        public DateTime? ExpiryTime { get; set; }

        // Nullable FK to Bid
        public int? HighestBidId { get; set; }

        // Navigation properties
        public User Owner { get; set; } = default!;

        public Bid? HighestBid { get; set; }

        // One-to-one with Auction (Product has one Auction)
        public Auction? Auction { get; set; }
    }
}