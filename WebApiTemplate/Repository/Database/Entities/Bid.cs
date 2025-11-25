using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApiTemplate.Repository.Database.Entities
{
    public class Bid
    {
        [Key]
        public int BidId { get; set; }

        [ForeignKey(nameof(Auction))]
        public int AuctionId { get; set; }

        [ForeignKey(nameof(Bidder))]
        public int BidderId { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Bid Amount must be > 0")]
        public decimal Amount { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation
        public Auction Auction { get; set; } = default!;
        public User Bidder { get; set; } = default!;
    }
}