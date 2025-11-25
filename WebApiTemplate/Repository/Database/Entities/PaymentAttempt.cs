using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApiTemplate.Repository.Database.Entities
{
    public class PaymentAttempt
    {
        [Key]
        public int PaymentId { get; set; }

        [ForeignKey(nameof(Auction))]
        public int AuctionId { get; set; }

        [ForeignKey(nameof(Bidder))]
        public int BidderId { get; set; }

        [Required]
        [MaxLength(50)] // e.g., "Pending", "Success", "Failed"
        public string Status { get; set; } = default!;

        // 1-3 (configurable in business logic)
        [Range(1, int.MaxValue)]
        public int AttemptNumber { get; set; }

        public DateTime AttemptTime { get; set; } = DateTime.UtcNow;

        public DateTime ExpiryTime { get; set; }

        // Optional: store the amount attempted for audit consistency
        [Column(TypeName = "numeric(18,2)")]
        public decimal? Amount { get; set; }

        // Amount confirmed by the bidder during payment confirmation
        [Column(TypeName = "numeric(18,2)")]
        public decimal? ConfirmedAmount { get; set; }

        // Navigation
        public Auction Auction { get; set; } = default!;
        public User Bidder { get; set; } = default!;
    }
}