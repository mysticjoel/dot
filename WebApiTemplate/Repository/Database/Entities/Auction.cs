using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

namespace WebApiTemplate.Repository.Database.Entities
{
    public class Auction
    {
        [Key]
        public int AuctionId { get; set; }

        // Unique & FK to Product.ProductId (enforce uniqueness in OnModelCreating)
        [ForeignKey(nameof(Product))]
        public int ProductId { get; set; }

        public DateTime ExpiryTime { get; set; }

        [Required]
        [MaxLength(50)] // e.g., "Active", "Completed", "Failed", "PendingPayment"
        public string Status { get; set; } = default!;

        // FK to Bid (nullable during early auction life)
        public int? HighestBidId { get; set; }

        public int ExtensionCount { get; set; }

        // Navigation properties
        public Product Product { get; set; } = default!;
        public Bid? HighestBid { get; set; }
    }
}
